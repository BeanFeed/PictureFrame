using System.Net;

namespace UpdateManager;

public class Utils
{
    public static bool CheckForInternetConnection()
    {
        try
        {
            using (var client = new WebClient())
            using (var stream = client.OpenRead("http://www.google.com"))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public static bool CheckForInternetConnection(string url)
    {
        try
        {
            using (var client = new WebClient())
            using (var stream = client.OpenRead(url))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}