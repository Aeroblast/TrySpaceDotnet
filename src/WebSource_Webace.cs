using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;

/// <summary>
///  从web-ace.jp抓漫画。
/// </summary>
class WebSource_Webace
{
    static string cookies = null;
    static void LoadCookies()
    {
        if (cookies == null)
        {
            if (File.Exists("cookies_webace.txt"))
                cookies = File.ReadAllText("cookies_webace.txt");
            else throw new Exception("Need Cookies string to download webace.");
        }
    }

    public static void WebSource_WebaceContent(string content_url)
    {
        string content = Util.GetWebText(content_url);
        Regex reg_ep = new Regex("<a href=\"(.*?)\" class=\"navigate-right\">");
        Regex reg_title = new Regex("media-body[\\s\\S]*?</div>");
        Regex reg_title1 = new Regex("<p class=\"text-bold\">(.*?)</p>");
        Regex reg_title2 = new Regex("<h3>(.*?)</h3>");
        List<string> ep_urls = new List<string>();
        List<string> ep_titles = new List<string>();
        var matchs = reg_ep.Matches(content);
        foreach (Match m in matchs)
        {
            ep_urls.Add("https://web-ace.jp" + m.Groups[1].Value);
        }
        matchs = reg_title.Matches(content);
        foreach (Match m in matchs)
        {
            string s = m.Value;
            string title = reg_title1.Match(s).Groups[1].Value;
            var m2 = reg_title2.Match(s);
            if (m2.Success) { title += " " + m2.Groups[1].Value; }
            ep_titles.Add(title);
        }
        if (ep_urls.Count != ep_titles.Count) { Log.log("[Error]"); return; }
        for (int i = ep_urls.Count - 1; i >= 0; i--)
        {
            string dir = $"[{Util.Number(ep_titles.Count - i)}] " + ep_titles[i];
            dir = dir.Replace("!", "！").Replace("?", "？");
            Directory.CreateDirectory(dir);
            Log.level = "";
            Log.log(dir);
            Log.level = " ";
            WebSource_WebaceEpisode(ep_urls[i], dir);
        }
    }
    public static void WebSource_WebaceEpisode(string url, string savePath = "")
    {
        LoadCookies();
        string json = Util.GetWebText(url + "json/");
        Regex reg_img = new Regex("/img.*?jpg");
        var ms = reg_img.Matches(json);
        foreach (Match m in ms)
        {
            string img_url = "https://web-ace.jp" + m.Value;
            string filename = Path.GetFileName(img_url);
            string filepath = Path.Combine(savePath, filename);
            if (File.Exists(filepath)) continue;
            HttpWebRequest req = HttpWebRequest.CreateHttp(img_url);
            req.Referer = url;
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36";
            req.CookieContainer = new CookieContainer();
            req.Timeout = 50000;
            req.CookieContainer.SetCookies(
                new Uri("https://web-ace.jp"),
                cookies);
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
                    Log.log("[Warn]Retry "+img_url);
                }

            Log.log("[Info]" + filename);
        }
    }
}