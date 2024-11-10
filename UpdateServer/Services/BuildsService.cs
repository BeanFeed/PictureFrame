using System.Text.Json;

namespace UpdateServer.Services;

public class BuildsService
{
    public BuildsService()
    {

    }

    public async Task<string> GetChangelog(string buildNumber)
    {
        if (!Utils.IsValidBuildNumber(buildNumber)) throw new Exception("Invalid Build Number.");

        string[] directories = Directory.GetDirectories(Path.Join(Environment.CurrentDirectory, "Builds"));
        if(!directories.Contains(buildNumber)) throw new Exception("Build not found.");

        string changelogPath = Path.Join(Environment.CurrentDirectory, "Builds", buildNumber, "changelog.md");
        if(!File.Exists(changelogPath)) throw new Exception("Build Has No Changelog.");

        return await File.ReadAllTextAsync(changelogPath);
    }

    public async Task<string> GetBuilds()
    {
        Dictionary<string, string> ret = new Dictionary<string, string>();
        string[] directories = Directory.GetDirectories(Path.Join(Environment.CurrentDirectory, "Builds"));
        foreach (var directory in directories)
        {
            ret.Add(directory, await GetChangelog(directory));
        }
        return JsonSerializer.Serialize(ret);
    }

    public async Task<byte[]> GetBuild(string buildNumber)
    {
        if (!Utils.IsValidBuildNumber(buildNumber)) throw new Exception("Invalid Build Number.");

        string[] directories = Directory.GetDirectories(Path.Join(Environment.CurrentDirectory, "Builds"));
        if(!directories.Contains(buildNumber)) throw new Exception("Build not found.");

        if(buildNumber == "latest") buildNumber = GetLatestBuildNumber();

        string buildPath = Path.Join(Environment.CurrentDirectory, "Builds", buildNumber, $"{buildNumber}.image");
        if(!File.Exists(buildPath)) throw new Exception("Build Not Found.");

        return await File.ReadAllBytesAsync(buildPath);
    }

    public string GetLatestBuildNumber()
    {
        string[] directories = Directory.GetDirectories(Path.Join(Environment.CurrentDirectory, "Builds"));
        if(directories.Length == 0) throw new Exception("No Builds Found.");

        string newestBuild = directories[0];

        for(int i = 1; i < directories.Length; i++)
        {
            if(Utils.IsValidBuildNumber(directories[i]) && Utils.IsValidBuildNumber(newestBuild))
            {
                string[] currentBuildParts = newestBuild.Split('.');
                string[] newBuildParts = directories[i].Split('.');
                if(int.Parse(currentBuildParts[0]) >= int.Parse(newBuildParts[0]) && int.Parse(currentBuildParts[1]) >= int.Parse(newBuildParts[1]) && int.Parse(currentBuildParts[2]) >= int.Parse(newBuildParts[2]) && int.Parse(currentBuildParts[3]) > int.Parse(newBuildParts[3]))
                {
                    newestBuild = directories[i];
                }
            }
        }

        return newestBuild;
    }
}