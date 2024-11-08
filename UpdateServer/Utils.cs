using System.Text.RegularExpressions;

namespace UpdateServer;

public static class Utils
{
    public static bool IsValidBuildNumber(string buildNumber)
    {
        Regex regex = new Regex("\\d\\.\\d\\.\\d\\.\\d", RegexOptions.IgnoreCase);
        return regex.IsMatch(buildNumber);
    }
}