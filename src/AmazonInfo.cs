using System;
using System.IO;
using System.Web;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
class AmazonInfo
{
    public static void GetAllAsinFromSearchPage(string searchPageUrl, string outputFile = "asin_list.csv", Encoding encoding = null)
    {
        Log.log("输入URL：" + searchPageUrl);
        Uri inputUri = new Uri(searchPageUrl);
        string urlWithoutQuery = searchPageUrl.Split('?')[0];
        var querys = HttpUtility.ParseQueryString(inputUri.Query);
        Log.log("关键词：" + HttpUtility.HtmlDecode(querys["k"]));
        if (string.IsNullOrEmpty(querys["page"]))
        {
            querys.Add("page", "1");
        }
        querys["ref"] = "sr_pg_1";
        string urlTemp = urlWithoutQuery + "?" + querys.ToString();
        string temp = GetWebText(urlTemp);
        int maxPage = GetMaxPageFromSearchPage("", temp);
        Log.log($"检测到共{maxPage}页");
        int page = 1;
        List<string> asinList = new List<string>();
        while (page <= maxPage)
        {
            var r = GetAsinFromSearchPageTemp(temp);
            Log.log($"第{page}页：获取到{r.Length}个ASIN。");
            Log.log("  起始：" + r[0]);
            Log.log("  结束：" + r[r.Length - 1]);
            asinList.AddRange(r);

            page++;
            querys["page"] = page + "";
            querys["ref"] = "sr_pg_" + page;
            urlTemp = urlWithoutQuery + "?" + querys.ToString();
            temp = GetWebText(urlTemp);
        }

        Log.log($"共收集到{asinList.Count}条");
        if (encoding == null) encoding = Encoding.UTF8;
        if (!File.Exists(outputFile))
        {
            Log.log("创建 " + outputFile);
            File.WriteAllLines(outputFile, asinList, encoding);
        }
        else
        {
            Log.log("将内容追加至 " + outputFile);
            File.AppendAllLines(outputFile, asinList, encoding);
        }

    }
    public static string[] GetAsinFromSearchPageTemp(string content)
    {
        Regex regex = new Regex("<a class=.a-link-normal s-underline-text s-underline-link-text s-link-style a-text-normal. target=._blank.*?ebook/dp/(.*?)/ref[\\s\\S]*?a-size-medium a-color-base a-text-normal.*?>(.*?)</span>");
        Regex regex2 = new Regex("MAIN-SEARCH_RESULTS.*?\n.*?ebook/dp/(.*?)/.*?alt=\"(.*?)\"");
        var ms = regex.Matches(content);
        if (ms.Count == 0) ms = regex2.Matches(content);
        if (ms.Count == 0) throw new Exception("匹配失败。可能日亚又刁难你了，需要更新匹配规则。");
        string[] r = new string[ms.Count];
        int i = 0;
        foreach (Match m in ms)
        {
            string asin = m.Groups[1].Value;
            string title = m.Groups[2].Value;
            r[i] = asin + '	' + title;
            i++;
        }
        return r;

    }


    public static int GetMaxPageFromSearchPage(string url, string content = null)
    {
        if (content == null)
        {
            content = GetWebText(url);
        }
        Regex reg_tag = new Regex("<.*?>");
        var m1 = Regex.Match(content, "<ul class=\"a-pagination\">[\\w\\W]*?</ul>");
        if (m1.Success)
        {
            var list = m1.Value;
            Regex reg_list = new Regex("<li.*?</li>");

            var ms1 = reg_list.Matches(list);
            int result = -1;
            for (int x = ms1.Count - 1; x > 0; x--)
            {
                var li = ms1[x].Value;
                var tryNumber = reg_tag.Replace(li, "");
                if (int.TryParse(tryNumber, out result))
                {
                    return result;
                }
            }
        }
        m1 = Regex.Match(content, "<div.*?s-pagination-container[\\s\\S].*?</div>");
        if (m1.Success)
        {
            var ms = Regex.Matches(content, "<span.*?s-pagination-item.*?</span>");
            int result = -1;
            for (int x = ms.Count - 1; x > 0; x--)
            {
                var li = ms[x].Value;
                var tryNumber = reg_tag.Replace(li, "");
                if (int.TryParse(tryNumber, out result))
                {
                    return result;
                }
            }
        }
        throw new Exception("没找到页码！");

    }



    static string GetWebText(string url)
    {
        //Log.log("[Info]Try dl " + url);
        while (true)
            try
            {
                HttpWebRequest req = HttpWebRequest.CreateHttp(url);
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
                using (var res = req.GetResponse())
                using (var s = res.GetResponseStream())
                using (var reader = new StreamReader(s))
                {
                    var r = reader.ReadToEnd();
                    Log.log("[Info]dl OK.");
                    File.WriteAllText("debug_temp.html", r);
                    return r;
                }
            }
            catch (Exception)
            {
                Log.log("[Warn]Retry");
            }
    }
}