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

    public UpdatesService(ManagerContext context, DownloadQueueSingleston downloadQueue,
        IHttpClientFactory clientFactory)
    {
        _context = context;
        _downloadQueue = downloadQueue;
        _clientFactory = clientFactory;
    }

    public async Task<string> GetUpdatePreferences()
    {
        string? updatePreferences = await _context.SystemConfigurations.AsNoTracking().Where(x => x.Key == "UpdatePreferences")
            .Select(x => x.Value).FirstOrDefaultAsync();
        if (updatePreferences is null) throw new Exception("Update Preferences Not Found.");
        return updatePreferences;
    }

    public async Task SetUpdatePreferences(UpdatePreferenceModel preferenceModel)
    {
        var configuration = await _context.SystemConfigurations.FirstOrDefaultAsync(x => x.Key == "UpdatePreferences");
        if (preferenceModel.ServerAddress[^1] == '/')
            preferenceModel.ServerAddress =
                preferenceModel.ServerAddress.Remove(preferenceModel.ServerAddress.Length - 1);

        if(!Utils.CheckForInternetConnection(preferenceModel.ServerAddress)) throw new Exception("Server Address Not Reachable.");

        if (configuration is null)
        {
            configuration = new SystemConfiguration()
            {
                Key = "UpdatePreferences",
                Value = JsonSerializer.Serialize(new UpdatePreferenceValue()
                {
                    ActualBuildNumber = string.Empty,
                    ServerAddress = preferenceModel.ServerAddress,
                    PreferredBuildNumber = preferenceModel.BuildNumber,
                    AutoUpdate = preferenceModel.AutoUpdate
                })
            };
            await _context.SystemConfigurations.AddAsync(configuration);
        }
        else
        {
            var currentValue = JsonSerializer.Deserialize<UpdatePreferenceValue>(configuration.Value);
            currentValue.ServerAddress = preferenceModel.ServerAddress;
            currentValue.PreferredBuildNumber = preferenceModel.BuildNumber;
            currentValue.AutoUpdate = preferenceModel.AutoUpdate;
            configuration.Value = JsonSerializer.Serialize(currentValue);
        }

        await _context.SaveChangesAsync();
    }

    private async Task SetUpdatePreferences(UpdatePreferenceValue preferenceModel)
    {
        var configuration = await _context.SystemConfigurations.FirstOrDefaultAsync(x => x.Key == "UpdatePreferences");
        if (preferenceModel.ServerAddress[^1] == '/')
            preferenceModel.ServerAddress =
                preferenceModel.ServerAddress.Remove(preferenceModel.ServerAddress.Length - 1);

        if(!Utils.CheckForInternetConnection(preferenceModel.ServerAddress)) throw new Exception("Server Address Not Reachable.");

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
        UpdatePreferenceValue? preferenceModel =
            JsonSerializer.Deserialize<UpdatePreferenceValue>(await GetUpdatePreferences());
        if (preferenceModel is null) throw new Exception("Update Preferences Not Found.");


        return await LatestBuildNumber() != preferenceModel.ActualBuildNumber;
    }

    private async Task<string> LatestBuildNumber()
    {
        UpdatePreferenceValue? preferenceModel =
            JsonSerializer.Deserialize<UpdatePreferenceValue>(await GetUpdatePreferences());
        if (preferenceModel is null) throw new Exception("Update Preferences Not Found.");


        using var client = _clientFactory.CreateClient("UpdateServer");
        string? latestBuild =
            await client.GetStringAsync($"{preferenceModel.ServerAddress}/api/builds/getlatestbuildnumber");
        if (latestBuild is null) throw new Exception("Latest Build Not Found.");

        return latestBuild;
    }

    public Dictionary<string, object> GetStatus()
    {
        int percent = _downloadQueue.PercentComplete;
        string status = _downloadQueue.Status;



        Dictionary<string, object> statusDict = new Dictionary<string, object>();
        statusDict.Add("status", status);
        statusDict.Add("percent", percent);
        if (status == "Complete")
        {
            _downloadQueue.Status = "Idle";
            _downloadQueue.PercentComplete = 0;
        }
        return statusDict;
    }

    public async Task<Dictionary<string, string>?> GetBuilds()
    {
        UpdatePreferenceValue? preferenceModel =
            JsonSerializer.Deserialize<UpdatePreferenceValue>(await GetUpdatePreferences());
        if (preferenceModel is null) throw new Exception("Update Preferences Not Found.");


        using (var client = _clientFactory.CreateClient("UpdateServer"))
        {

            return await client.GetFromJsonAsync<Dictionary<string, string>>(
                $"{preferenceModel.ServerAddress}/api/builds/getbuilds");
        }
    }

    public async Task StartUpdate(string buildNumber)
    {
        UpdatePreferenceValue? preferenceModel =
            JsonSerializer.Deserialize<UpdatePreferenceValue>(await GetUpdatePreferences());
        if (preferenceModel is null) throw new Exception("Update Preferences Not Found.");

        preferenceModel.ActualBuildNumber = buildNumber;
        await SetUpdatePreferences(preferenceModel);

        _downloadQueue.QueueTask(async () => await InstallBuild(preferenceModel));
    }

    public async Task StartUpdate()
    {
        UpdatePreferenceValue? preferenceModel =
            JsonSerializer.Deserialize<UpdatePreferenceValue>(await GetUpdatePreferences());
        if (preferenceModel is null) throw new Exception("Update Preferences Not Found.");

        if (preferenceModel.PreferredBuildNumber == "latest")
        {
            var number = await LatestBuildNumber();
            preferenceModel.ActualBuildNumber = number;
            await SetUpdatePreferences(preferenceModel);
        }
        _downloadQueue.QueueTask(async () => await InstallBuild(preferenceModel));

    }

    public async Task<string> GetChangelog(string buildNumber)
    {
        UpdatePreferenceValue? preferenceModel =
            JsonSerializer.Deserialize<UpdatePreferenceValue>(await GetUpdatePreferences());
        if (preferenceModel is null) throw new Exception("Update Preferences Not Found.");

        using (var client = _clientFactory.CreateClient("UpdateServer"))
        {
            string changelog =
                await client.GetStringAsync(
                    $"{preferenceModel.ServerAddress}/api/builds/getchangelog?buildNumber={buildNumber}");
            return changelog;
        }
    }

    public async Task<string> GetChangelog()
    {
        UpdatePreferenceValue? preferenceModel =
            JsonSerializer.Deserialize<UpdatePreferenceValue>(await GetUpdatePreferences());
        if (preferenceModel is null) throw new Exception("Update Preferences Not Found.");

        using (var client = _clientFactory.CreateClient("UpdateServer"))
        {
            string changelog = await client.GetStringAsync(
                $"{preferenceModel.ServerAddress}/api/builds/getchangelog?buildNumber={preferenceModel.ActualBuildNumber}");
            return changelog;
        }
    }

    private async Task InstallBuild(UpdatePreferenceValue preferenceModel)
    {
        using (var client = _clientFactory.CreateClient("UpdateServer"))
        {
            var progress = new Progress<float>();
            progress.ProgressChanged += ReportProgress;


            using (var stream =
                   new FileStream(
                       Path.Join(Environment.CurrentDirectory, "Builds", $"{preferenceModel.ActualBuildNumber}.image"),
                       FileMode.Create, FileAccess.Write, FileShare.None))
                await client.DownloadDataAsync($"{preferenceModel.ServerAddress}/api/builds/getbuild?buildNumber={preferenceModel.PreferredBuildNumber}",
                    stream, progress);
        }

        if (true)
        {
            //Execute docker load command
            ProcessStartInfo psi = new ProcessStartInfo("docker",
                $"load -i {Path.Join(Environment.CurrentDirectory, "Builds", $"{preferenceModel.PreferredBuildNumber}.image")}");
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            Process process = new Process();
            process.StartInfo = psi;
            process.Start();
            await process.WaitForExitAsync();

            File.Delete(Path.Join(Environment.CurrentDirectory, "Builds", $"{preferenceModel.PreferredBuildNumber}.image"));

            //Execute docker compose down in WebApp directory
            psi = new ProcessStartInfo("docker",
                $"compose --file {Path.Join(Environment.CurrentDirectory, "WebApp", "docker-compose.yml")} down");
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            process = new Process();
            process.StartInfo = psi;
            process.Start();
            await process.WaitForExitAsync();

            //Execute docker compose up -d in WebApp directory
            psi = new ProcessStartInfo("docker",
                $"compose --file {Path.Join(Environment.CurrentDirectory, "WebApp", "docker-compose.yml")} up -d");
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            process = new Process();
            process.StartInfo = psi;
            process.Start();
            await process.WaitForExitAsync();
        }

    }

    private void ReportProgress(object? sender, float progress)
    {
        _downloadQueue.PercentComplete = (int)progress;
        if(_downloadQueue.PercentComplete >= 100) _downloadQueue.Status = "Complete";
    }
}

public static class HttpClientProgressExtensions
    {
        public static async Task DownloadDataAsync(this HttpClient client, string requestUrl, Stream destination,
            IProgress<float> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var response = await client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                var contentLength = response.Content.Headers.ContentLength;
                using (var download = await response.Content.ReadAsStreamAsync())
                {
                    // no progress... no contentLength... very sad
                    if (progress is null || !contentLength.HasValue)
                    {
                        await download.CopyToAsync(destination);
                        return;
                    }

                    // Such progress and contentLength much reporting Wow!
                    var progressWrapper = new Progress<long>(totalBytes =>
                        progress.Report(GetProgressPercentage(totalBytes, contentLength.Value)));
                    await download.CopyToAsync(destination, 81920, progressWrapper, cancellationToken);
                }
            }

            float GetProgressPercentage(float totalBytes, float currentBytes) => (totalBytes / currentBytes) * 100f;
        }

        static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize,
            IProgress<long> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (!source.CanRead)
                throw new InvalidOperationException($"'{nameof(source)}' is not readable.");
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite)
                throw new InvalidOperationException($"'{nameof(destination)}' is not writable.");

            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead =
                       await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }
    }