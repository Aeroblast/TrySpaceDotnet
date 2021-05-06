using System;
using System.IO;
using System.IO.Compression;
using System.Net;
class Util
{

    public static string Number(int number, int length = 4)
    {
        string r = number.ToString();
        for (int j = length - r.Length; j > 0; j--) r = "0" + r;
        return r;
    }
    public static void Unzip(string archive_path, string output_dir)
    {
        if (Directory.Exists(output_dir))
        {
            Directory.CreateDirectory(output_dir);
        }
        ZipArchive archive = ZipFile.OpenRead(archive_path);
        archive.ExtractToDirectory(output_dir);
    }
    public static string GetWebText(string url)
    {
        Log.log("[Info]Try dl " + url);
        while (true)
            try
            {
                HttpWebRequest req = HttpWebRequest.CreateHttp(url);
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36";
                using (var res = req.GetResponse())
                using (var s = res.GetResponseStream())
                using (var reader = new StreamReader(s))
                {
                    var r = reader.ReadToEnd();
                    Log.log("[Info]dl OK.");
                    return r;
                }
            }
            catch (Exception)
            {
                Log.log("[Warn]Retry");
            }
    }
    public static void GetWebBinary(string img_url, string filepath, string referer = "")
    {
        HttpWebRequest req = HttpWebRequest.CreateHttp(img_url);
        req.Referer = referer;
        req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36";
        req.CookieContainer = new CookieContainer();
        req.Timeout = 50000;
        //req.CookieContainer.SetCookies(S
        //  new Uri(""),
        // "");
        while (true)
            try
            {
                using (var res = req.GetResponse())
                using (var s = res.GetResponseStream())
                using (var file = File.OpenWrite(filepath))
                {
                    s.CopyTo(file);
                    break;
                }
            }
            catch (Exception)
            {
                Log.log("[Warn]Retry");
            }
    }

    public static string Trim(string str)
    {
        int s = 0, e = str.Length - 1;
        for (; s < str.Length; s++) { if (str[s] == ' ' || str[s] == '\t' || str[s] == '\n' || str[s] == '\r') { } else break; }
        for (; e >= 0; e--) { if (str[e] == ' ' || str[e] == '\t' || str[e] == '\n' || str[e] == '\r') { } else break; }
        if (s <= e) return str.Substring(s, e - s + 1);
        else return "";
    }
}