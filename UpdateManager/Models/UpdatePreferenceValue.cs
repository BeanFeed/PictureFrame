namespace UpdateManager.Models;

public class UpdatePreferenceValue
{
    public string ServerAddress { get; set; } = null!;
    public string PreferredBuildNumber { get; set; } = null!;
    public bool AutoUpdate { get; set; }
    public string ActualBuildNumber { get; set; } = null!;
}