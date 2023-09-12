using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
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
        var baseAddress = new Uri(url);
        using (var handler = new HttpClientHandler() { UseCookies = false })
        using (var httpClient = new HttpClient(handler) { BaseAddress = baseAddress })
        using (var req = new HttpRequestMessage(HttpMethod.Get, url))
        {
            req.Headers.Add("UserAgent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36");
            var responseMessage = httpClient.SendAsync(req).Result;
            responseMessage.EnsureSuccessStatusCode();
            using (var webstm = responseMessage.Content.ReadAsStream())
            using (var tr = new StreamReader(webstm))
            {
                return tr.ReadToEnd();
            }
        }
    }
    public static void GetWebBinary(string img_url, string filepath, string referer = "")
    {
        var baseAddress = new Uri(img_url);
        var handler = new HttpClientHandler() { UseCookies = false };
        var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
        using (var req = new HttpRequestMessage(HttpMethod.Get, img_url))
        {
            req.Headers.Add("UserAgent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36");
            req.Headers.Add("Referer", referer);
            var responseMessage = httpClient.SendAsync(req).Result;
            responseMessage.EnsureSuccessStatusCode();
            using (var webstm = responseMessage.Content.ReadAsStream())
            using (var file = File.OpenWrite(filepath))
            {
                webstm.CopyTo(file);
            }
        }
        httpClient.Dispose();
        handler.Dispose();
    }

    public static string Trim(string str)
    {
        int s = 0, e = str.Length - 1;
        for (; s < str.Length; s++) { if (str[s] == ' ' || str[s] == '\t' || str[s] == '\n' || str[s] == '\r') { } else break; }
        for (; e >= 0; e--) { if (str[e] == ' ' || str[e] == '\t' || str[e] == '\n' || str[e] == '\r') { } else break; }
        if (s <= e) return str.Substring(s, e - s + 1);
        else return "";
    }
    public static string FilenameCheck(string s)
    {
        if (s == null) return "";
        return s
        .Replace('?', '？')
        .Replace('\\', '＼')
        .Replace('/', '／')
        .Replace(':', '：')
        .Replace('*', '＊')
        .Replace('"', '＂')
        .Replace('|', '｜')
        .Replace('<', '＜')
        .Replace('>', '＞')
        ;
    }

    ///<remarks>
    ///只实现到百
    ///</remarks>
    public static int FromChineseNumber(string s)
    {
        if (s == "零") return 0;
        int r = 0;
        string dic = "零一二三四五六七八九";
        int i = s.Length - 1;
        if (dic.Contains(s[i]))
        {
            r += dic.IndexOf(s[i]);
        }
        i = s.IndexOf('十');
        if (i > 0)//几十几
        {
            r += dic.IndexOf(s[i - 1]) * 10;
        }
        else if (i == 0)//十几
        {
            r += 10;
        }
        i = s.IndexOf('百');
        if (i > 0)
        {
            r += dic.IndexOf(s[i - 1]) * 100;
        }
        if (i == 0)
        {
            throw new Exception("第一个字不能是百。");
        }
        return r;
    }
}