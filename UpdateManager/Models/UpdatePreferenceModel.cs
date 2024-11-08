namespace UpdateManager.Models;

public class UpdatePreferenceModel
{
    public string ServerAddress { get; set; } = null!;
    public string BuildNumber { get; set; } = null!;
    public bool AutoUpdate { get; set; }
}