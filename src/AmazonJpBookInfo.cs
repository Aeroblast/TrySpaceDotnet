using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;

class AmazonJpBookInfo
{
    static void FromBookPage(string asin)
    {
        string page_main = Util.GetWebText("https://www.amazon.co.jp/dp/" + asin);
    }
    public static void FromSearchPage(string url, string csv_save_path = "SearchPage.csv")
    {

        using (var fs = File.OpenWrite(csv_save_path))
        using (StreamWriter sr = new StreamWriter(fs))
            FromSearchPage(url, sr);
    }
    public static void FromSearchPage(string url, StreamWriter sr)
    {
        string page_main = Util.GetWebText(url);
        FromSearchPage(page_main, sr, 0);
    }
    public static void FromSearchPage(string local_cache, StreamWriter sr, int sign)
    {
        Regex regex = new Regex("<a class=.a-link-normal a-text-normal.*?ebook/dp/(.*?)/ref[\\s\\S]*?a-size-medium a-color-base a-text-normal\" dir=\"auto\">(.*?)</span>[\\s\\S]*?<span class=\"a-size-base\" dir=\"auto\">(.*?)\\|");
        Regex reg_tag = new Regex("<.*?>");
        var ms = regex.Matches(local_cache);

        foreach (Match m in ms)
        {
            string asin = m.Groups[1].Value;
            string title = m.Groups[2].Value;
            string author = reg_tag.Replace(m.Groups[3].Value, "");
            sr.WriteLine($"{asin},{title},{author}");
        }
    }
}