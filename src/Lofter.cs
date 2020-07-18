using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;

/// <summary>
///  Lofter导出的博客内容XML文档并不完整，长文和图片需要另外抓取。
/// </summary>
class WebSource_Lofter
{
    public static void GetImageFromExported(string xml_path)
    {
        string d = File.ReadAllText(xml_path);
        Regex regex = new Regex("<img.*?src=\"(.*?)[\\?\"]");
        var rs = regex.Matches(d);
        foreach (Match m in rs)
        {
            string url = m.Groups[1].Value;
            Util.GetWebBinary(url, "Images/" + Path.GetFileName(url), "https://lofter.com");
        };
    }
    public static void GetLongPostFromExported(string xml_path)
    {
        XmlDocument xml = new XmlDocument();
        xml.Load(xml_path);
        Regex regex_banner = new Regex("");
        Regex regex_post = new Regex("(<img class=\"banner\" id=\"banner\"[\\s\\S]*?)<div class=\"post\">");
        foreach (XmlElement e in xml.GetElementsByTagName("PostItem"))
        {
            string t = e.GetElementsByTagName("type")[0].InnerXml;
            if (t == "Long")
            {
                string id = e.GetElementsByTagName("permalink")[0].InnerXml;
                string text = Util.GetWebText("http://www.lofter.com/lpost/" + id);
                string post = regex_post.Match(text).Groups[1].Value;
                post += "</div>";
                File.WriteAllText(id, post);
                GetImageFromExported(id);
            }

        }
    }
}