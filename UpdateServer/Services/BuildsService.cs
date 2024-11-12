using System.Text.Json;

namespace UpdateServer.Services;

public class BuildsService
{
    public BuildsService()
    {

    }

    public async Task<string> GetChangelog(string buildNumber)
    {
        if (buildNumber == "latest") buildNumber = GetLatestBuildNumber();
        
        if (!Utils.IsValidBuildNumber(buildNumber)) throw new Exception("Invalid Build Number.");

        string[] directories = Directory.GetDirectories(Path.Join(Environment.CurrentDirectory, "Builds"));
        
        for (int i = 0; i < directories.Length; i++)
        {
            directories[i] = directories[i].Split('/')[directories[i].Split('/').Length - 1];
        }
        
        if(!directories.Contains(buildNumber)) throw new Exception("Build not found.");
        
        string changelogPath = Path.Join(Environment.CurrentDirectory, "Builds", buildNumber, "changelog.md");
        if(!File.Exists(changelogPath)) throw new Exception("Build Has No Changelog.");

        return await File.ReadAllTextAsync(changelogPath);
    }

    public async Task<Dictionary<string,string>> GetBuilds()
    {
        Dictionary<string, string> ret = new Dictionary<string, string>();
        string[] directories = Directory.GetDirectories(Path.Join(Environment.CurrentDirectory, "Builds"));
        for (int i = 0; i < directories.Length; i++)
        {
            directories[i] = directories[i].Split('/')[directories[i].Split('/').Length - 1];
        }
        foreach (var directory in directories)
        {
            ret.Add(directory, await GetChangelog(directory));
        }
        return ret;
    }

    public async Task<byte[]> GetBuild(string buildNumber)
    {
        if(buildNumber == "latest") buildNumber = GetLatestBuildNumber();

        if (!Utils.IsValidBuildNumber(buildNumber)) throw new Exception("Invalid Build Number.");

        string[] directories = Directory.GetDirectories(Path.Join(Environment.CurrentDirectory, "Builds"));
        
        for (int i = 0; i < directories.Length; i++)
        {
            directories[i] = directories[i].Split('/')[directories[i].Split('/').Length - 1];
        }
        
        if(!directories.Contains(buildNumber)) throw new Exception("Build not found.");



        string buildPath = Path.Join(Environment.CurrentDirectory, "Builds", buildNumber, $"{buildNumber}.image");
        if(!File.Exists(buildPath)) throw new Exception("Build Not Found.");

        return await File.ReadAllBytesAsync(buildPath);
    }

    public string GetLatestBuildNumber()
    {
        string[] directories = Directory.GetDirectories(Path.Join(Environment.CurrentDirectory, "Builds"));
        
        for (int i = 0; i < directories.Length; i++)
        {
            directories[i] = directories[i].Split('/')[directories[i].Split('/').Length - 1];
        }
        
        if(directories.Length == 0) throw new Exception("No Builds Found.");

        if(!Utils.IsValidBuildNumber(directories[0])) throw new Exception("Invalid Build Number.");

        string[] split = directories[0].Split(".");

        DateTime newestBuild = new DateTime(
            int.Parse(split[2]),
            int.Parse(split[1]),
            int.Parse(split[0]),
            int.Parse(split[3].Substring(0,2)),
            int.Parse(split[3].Substring(2,2)),
            0
            );



        for(int i = 1; i < directories.Length; i++)
        {
            if(Utils.IsValidBuildNumber(directories[i]) )
            {
                split = directories[i].Split(".");
                DateTime check = new DateTime(
                    int.Parse(split[2]),
                    int.Parse(split[1]),
                    int.Parse(split[0]),
                    int.Parse(split[3].Substring(0,2)),
                    int.Parse(split[3].Substring(2,2)),
                    0
                );
                if(check > newestBuild) newestBuild = check;

            }
        }
        return newestBuild.ToString("dd.MM.yyyy.HHmm");
    }
}