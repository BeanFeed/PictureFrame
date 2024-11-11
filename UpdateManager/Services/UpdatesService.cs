using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using UpdateManager.Database.Context;
using UpdateManager.Database.Entities;
using UpdateManager.Models;

namespace UpdateManager.Services;

public class UpdatesService
{
    private readonly ManagerContext _context;
    private readonly DownloadQueueSingleston _downloadQueue;
    private readonly IHttpClientFactory _clientFactory;

    public UpdatesService(ManagerContext context, DownloadQueueSingleston downloadQueue, IHttpClientFactory clientFactory)
    {
        _context = context;
        _downloadQueue = downloadQueue;
        _clientFactory = clientFactory;
    }

    public async Task<string> GetUpdatePreferences()
    {
        string? updatePreferences = await _context.SystemConfigurations.Where(x => x.Key == "UpdatePreferences").Select(x => x.Value).FirstOrDefaultAsync();
        if(updatePreferences is null) throw new Exception("Update Preferences Not Found.");
        return updatePreferences;
    }

    public async Task SetUpdatePreferences(UpdatePreferenceModel preferenceModel)
    {
        var configuration = await _context.SystemConfigurations.FirstOrDefaultAsync(x => x.Key == "UpdatePreferences");
        if(preferenceModel.ServerAddress[^1] == '/') preferenceModel.ServerAddress = preferenceModel.ServerAddress.Remove(preferenceModel.ServerAddress.Length - 1);
        if (configuration is null)
        {
            configuration = new SystemConfiguration()
            {
                Key = "UpdatePreferences",
                Value = JsonSerializer.Serialize(preferenceModel)
            };
            await _context.SystemConfigurations.AddAsync(configuration);
        }
        else
        {
            configuration.Value = JsonSerializer.Serialize(preferenceModel);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsUpdateAvailable()
    {
        UpdatePreferenceModel? preferenceModel = JsonSerializer.Deserialize<UpdatePreferenceModel>(await GetUpdatePreferences());
        if(preferenceModel is null) throw new Exception("Update Preferences Not Found.");


        using (var client = _clientFactory.CreateClient("UpdateServer"))
        {
            string? latestBuild = await client.GetStringAsync($"{preferenceModel.ServerAddress}/api/builds/getlatestbuildnumber");
            if(latestBuild is null) throw new Exception("Latest Build Not Found.");

            return latestBuild != preferenceModel.BuildNumber;
        }
    }

    public async Task<(string, int)> GetStatus()
    {
        int percent = _downloadQueue.PercentComplete;
        string status = _downloadQueue.Status;

        if(status == "Complete")
        {
            _downloadQueue.Status = "Idle";
            _downloadQueue.PercentComplete = 0;
        }

        return (status, percent);
    }

    public async Task<Dictionary<string,string>?> GetBuilds()
    {
        UpdatePreferenceModel? preferenceModel = JsonSerializer.Deserialize<UpdatePreferenceModel>(await GetUpdatePreferences());
        if(preferenceModel is null) throw new Exception("Update Preferences Not Found.");


        using (var client = _clientFactory.CreateClient("UpdateServer"))
        {
            
            return await client.GetFromJsonAsync<Dictionary<string,string>>($"{preferenceModel.ServerAddress}/api/builds/getbuilds");
        }
    }

    public async Task StartUpdate(string buildNumber)
    {
        UpdatePreferenceModel? preferenceModel = JsonSerializer.Deserialize<UpdatePreferenceModel>(await GetUpdatePreferences());
        if(preferenceModel is null) throw new Exception("Update Preferences Not Found.");

        preferenceModel.BuildNumber = buildNumber;
        await SetUpdatePreferences(preferenceModel);

        _downloadQueue.QueueTask(async () => await InstallBuild());
    }

    public async Task StartUpdate()
    {
        _downloadQueue.QueueTask(async () => await InstallBuild());
    }

    public async Task<string> GetChangelog(string buildNumber)
    {
        UpdatePreferenceModel? preferenceModel = JsonSerializer.Deserialize<UpdatePreferenceModel>(await GetUpdatePreferences());
        if(preferenceModel is null) throw new Exception("Update Preferences Not Found.");
        
        using (var client = _clientFactory.CreateClient("UpdateServer"))
        {
            string changelog = await client.GetStringAsync($"{preferenceModel.ServerAddress}/api/builds/getchangelog?buildNumber={buildNumber}");
            return changelog;
        }
    }

    public async Task<string> GetChangelog()
    {
        UpdatePreferenceModel? preferenceModel = JsonSerializer.Deserialize<UpdatePreferenceModel>(await GetUpdatePreferences());
        if(preferenceModel is null) throw new Exception("Update Preferences Not Found.");

        using (var client = _clientFactory.CreateClient("UpdateServer"))
        {
            string changelog = await client.GetStringAsync($"{preferenceModel.ServerAddress}/api/builds/getchangelog?buildNumber={preferenceModel.BuildNumber}");
            return changelog;
        }
    }

    private async Task InstallBuild()
    {
        UpdatePreferenceModel? preferenceModel = JsonSerializer.Deserialize<UpdatePreferenceModel>(await GetUpdatePreferences());
        if(preferenceModel is null) throw new Exception("Update Preferences Not Found.");

        using (var client = _clientFactory.CreateClient("UpdateServer"))
        {
            var response = await client.GetAsync($"{preferenceModel.ServerAddress}/api/builds/getbuild?buildNumber={preferenceModel.BuildNumber}");
            response.EnsureSuccessStatusCode();

            long totalContentLength = response.Content.Headers.ContentLength ?? 0;
            long downloadedBytes = 0;
            
            using (var fileStream =
                   new FileStream(
                       Path.Join(Environment.CurrentDirectory, "Builds", $"{preferenceModel.BuildNumber}.image"),
                       FileMode.Create, FileAccess.Write))
            {
                response.Content.CopyToAsync(fileStream);

                ReportProgressAsync(totalContentLength, downloadedBytes);
            }
        }

        //Execute docker load command
        ProcessStartInfo psi = new ProcessStartInfo("docker", $"load -i {Path.Join(Environment.CurrentDirectory, "Builds", $"{preferenceModel.BuildNumber}.image")}");
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        Process process = new Process();
        process.StartInfo = psi;
        process.Start();
        await process.WaitForExitAsync();

        File.Delete(Path.Join(Environment.CurrentDirectory, "Builds", $"{preferenceModel.BuildNumber}.image"));

        //Execute docker compose down in WebApp directory
        psi = new ProcessStartInfo("docker", $"compose --file {Path.Join(Environment.CurrentDirectory, "WebApp", "docker-compose.yml")} down");
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        process = new Process();
        process.StartInfo = psi;
        process.Start();
        await process.WaitForExitAsync();

        //Execute docker compose up -d in WebApp directory
        psi = new ProcessStartInfo("docker", $"compose --file {Path.Join(Environment.CurrentDirectory, "WebApp", "docker-compose.yml")} up -d");
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        process = new Process();
        process.StartInfo = psi;
        process.Start();
        await process.WaitForExitAsync();
    }
    
    private void ReportProgressAsync(long totalContentLength, long downloadedBytes)
    {
        double progressPercentage = (downloadedBytes * 100.0 / totalContentLength);
        _downloadQueue.PercentComplete = (int)progressPercentage;
    }
}