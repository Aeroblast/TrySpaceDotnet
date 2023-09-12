
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml;
using System.Text.RegularExpressions;
using AeroEpub;

/// <summary>
/// 处理某种格式的电子书（章节全都在一个HTML里，使用section标签分章节，使用形式为UUID的h2标签定位）。
/// 比如 G L 文 庫。
/// 小心草台出版社（空章节，只有标题的章节）需要手动处理。
/// </summary>
class EbookFix_Ichitaro
{

    public static EpubFile GetFixedFile(string src_path)
    {
        EpubFile src = new EpubFile(src_path);
        var dst = FixEpub(src);
        return src;
    }

    public static EpubFile FixEpub(EpubFile src)
    {
        src.ReadMeta();
        var needCreateV3 = false;
        Log.log("src epub version: " + src.version);
        if (src.version == "2.0")
        {
            needCreateV3 = true;
        }
        Itemref ref_cover, ref_toc, ref_content;
        if (src.spine.Count == 3)
        {
            ref_cover = src.spine[0];
            ref_toc = src.spine[1];
            ref_content = src.spine[2];
        }
        else if (src.spine.Count == 2)
        {
            ref_toc = src.spine[0];
            ref_content = src.spine[1];
            var cover_path = "OEBPS/Text/cover.xhtml";
            var fileEntry = new TextEpubFileEntry(cover_path,
                    cover_xhtml.Replace("{filename}", Path.GetFileName(src.coverImage.filePath))
            );

            src.entries.Add(fileEntry);
            src.manifest.Add("cover", new Item(src, fileEntry, "cover", "application/xhtml+xml", ""));
            ref_cover = new Itemref("cover", src.manifest);
        }
        else
        {
            throw new Exception("Spine should be 3.");
        }

        var content_file = ref_content.item.GetFile() as TextEpubFileEntry;
        var doc = new XmlDocument();
        doc.LoadXml(content_file.text);
        var body = doc.GetElementsByTagName("body")[0];
        DelUselessLink(doc);
        XmlElement sections = null;
        foreach (var _ele in body.ChildNodes)
        {
            if (sections != null)
            {
                throw new Exception("already have root <section>");
            }
            var ele = _ele as XmlElement;
            if (ele == null) { throw new Exception("ELement?"); }
            if (ele.Name == "section")
            {
                sections = ele;
                continue;
            }
            if (ele.Name != "p" && ele.Name != "div")
            {
                throw new Exception("Unknown ELement");
            }
        }
        List<string> chapters = new List<string>();

        foreach (var _ele in sections.ChildNodes)
        {
            var ele = _ele as XmlElement;
            if (ele == null) { throw new Exception("ELement?"); }
            if (ele.Name == "section")
            {
                chapters.Add(ele.InnerXml);
            }
            else
            {
                throw new Exception("non- <section> element.");
            }
        }
        Log.log("Got chapter content.");

        sections.ParentNode.RemoveChild(sections);
        var pre = doc2string(doc);


        // proc epub
        ref_cover.item.id = "cover";
        ref_toc.item.id = "toc";
        var entry_toc = ref_toc.item.GetFile();
        entry_toc.fullName = "OEBPS/Text/toc.xhtml";
        ref_toc.item.href = "Text/toc.xhtml";

        bool r;

        src.spine.items.Clear();
        r = src.manifest.Remove(ref_content.item.id);
        if (!r) { throw new Exception("Delete failed"); }
        r = src.entries.Remove(src.GetFile(ref_content.filePath));
        if (!r) { throw new Exception("Delete failed"); }

        src.spine.items.Add(ref_cover);
        src.spine.items.Add(ref_toc);
        var hrefTranform = new Dictionary<string, string>();
        var contentEntries = new List<TextEpubFileEntry>();
        contentEntries.Add(ref_toc.item.GetFile() as TextEpubFileEntry);
        hrefTranform.Add("toc", Path.GetFileName(ref_toc.filePath));
        // create new files
        {
            var filename = "titlepage";
            var path = "OEBPS/Text/" + filename + ".xhtml";
            var fileEntry = new TextEpubFileEntry(path, pre);
            var id = filename;
            src.entries.Add(fileEntry);
            src.manifest.Add(filename, new Item(src, fileEntry, id, "application/xhtml+xml", ""));
            src.spine.items.Add(new Itemref(id, src.manifest));
            contentEntries.Add(fileEntry);
            Log.log("Created titlepage.xhtml");
        }

        Log.log("Processing chapters...");
        var tocTitles = new List<string>();
        var tocHrefs = new List<string>();

        foreach (var str in chapters)
        {
            body.RemoveAll();
            body.InnerXml = str;
            var data = doc2string(doc);
            var h2s = doc.GetElementsByTagName("h2");
            var h2 = h2s[0];

            var title = h2.InnerText;
            var id = h2.Attributes["id"].Value;
            var filename = MapFileName(title);
            if (string.IsNullOrEmpty(filename)) { throw new NotImplementedException("Map name failure: " + title); }
            Log.log($"{title} => {filename}");
            var path = "OEBPS/Text/" + filename + ".xhtml";
            var fileEntry = new TextEpubFileEntry(path, data);
            src.entries.Add(fileEntry);
            var item = new Item(src, fileEntry, filename, "application/xhtml+xml", "");
            src.manifest.Add(filename, item);
            src.spine.items.Add(new Itemref(filename, src.manifest));
            tocTitles.Add(title.Trim());
            tocHrefs.Add(item.href);
            hrefTranform.Add(id, filename + ".xhtml");
            contentEntries.Add(fileEntry);
            if (h2s.Count > 1)
            {
                if (h2s.Count > 2) { throw new NotImplementedException("3 or more <h2> in <section>"); }
                h2 = h2s[1];
                id = h2.Attributes["id"].Value;
                hrefTranform.Add(id, filename + ".xhtml#" + id);
            }
        }
        var reg_href = new Regex("href=\"(.*?)\"");
        foreach (var entry in contentEntries)
        {
            entry.text = reg_href.Replace(entry.text, m =>
            {
                var href = m.Groups[1].Value;
                if (href.EndsWith(".css"))
                {
                    return m.Value;
                }
                if (href == "part0001.xhtml")
                {
                    return "href=\"" + Path.GetFileName(tocHrefs[0]) + "\"";
                }
                if (href == "part0000.xhtml")
                {
                    return "href=\"" + Path.GetFileName(ref_toc.filePath) + "\"";
                }
                foreach (var kp in hrefTranform)
                {
                    if (Regex.Match(href, "part000[01].xhtml#" + kp.Key).Success)
                    {
                        return "href=\"" + kp.Value + "\"";
                    }
                }
                throw new Exception("href match fail: " + href);
            });
        }

        {
            var x = src.GetFile<TextEpubFileEntry>("OEBPS/Text/toc.xhtml");
            x.text = Regex.Replace(x.text, "<link .*?stylenavhorizontal.css.*?/>", "");
        }

        {
            var x = src.GetFile<TextEpubFileEntry>("OEBPS/Text/cover.xhtml");
            x.text = Regex.Replace(x.text, "<!DOCTYPE html.*?>", "<!DOCTYPE html>");
        }


        Log.log("Processing toc...");
        // toc
        {
            var src_ncx = src.spine.toc.GetFile() as TextEpubFileEntry;
            TextEpubFileEntry src_nav;
            if (needCreateV3)
            {
                var path = "OEBPS/nav.xhtml";
                var fileEntry = new TextEpubFileEntry(path, pre);
                src.entries.Add(fileEntry);
                src.manifest.Add("epubnav", new Item(src, fileEntry, "epubnav", "application/xhtml+xml", "nav"));
                src_nav = fileEntry;
                Log.log("Created nav.xhtml");
            }
            else
            {
                src_nav = src.toc.GetFile() as TextEpubFileEntry;
            }
            XmlDocument ncxDoc = new XmlDocument();
            ncxDoc.LoadXml(src_ncx.text);
            var srcTitles = new List<string>();
            foreach (XmlElement ele in ncxDoc.GetElementsByTagName("navLabel"))
            {
                string title = ele.InnerText.Trim();
                if (!string.IsNullOrEmpty(title))
                {
                    srcTitles.Add(title);
                }
            }
            if (srcTitles.Count != tocTitles.Count)
            {
                //throw new Exception("src_toc.Count != tocTitles.Count");
                Log.Warn("src_toc.Count != tocTitles.Count");
            }
            var v2_root = ncxDoc.GetElementsByTagName("navMap")[0];

            var sb_v2 = new StringBuilder();
            var sb_v3 = new StringBuilder();
            for (int i = 0; i < tocTitles.Count; i++)
            {
                var number = i + 1;
                var entry_v2 = $"<navPoint id=\"navPoint-{number}\" playOrder=\"{number}\"><navLabel><text>{tocTitles[i]}</text></navLabel><content src=\"{tocHrefs[i]}\" /></navPoint>";
                var entry_v3 = $"<li><a href=\"{tocHrefs[i]}\">{tocTitles[i]}</a></li>";
                sb_v2.Append(entry_v2);
                sb_v3.Append(entry_v3);
                string temp_srcTitle = "null";
                if (i < srcTitles.Count) { temp_srcTitle = srcTitles[i]; }
                Log.log($"{temp_srcTitle}|{tocTitles[i]}");
            }
            v2_root.InnerXml = sb_v2.ToString();
            src_ncx.text = doc2string(ncxDoc);
            var navDoc = new XmlDocument();
            navDoc.LoadXml(nav_template.Replace("{content}", sb_v3.ToString()));
            src_nav.text = doc2string(navDoc);
        }// end of toc

        src.WriteMeta3();

        return src;
    }
    static void DelUselessLink(XmlDocument doc)
    {
        var links = doc.GetElementsByTagName("link");
        var link_tobedelete = new List<XmlElement>();
        foreach (XmlElement link in links)
        {
            if (link.Attributes["class"].Value == "horizontal")
            {
                Log.log("Del " + link.OuterXml);
                link_tobedelete.Add(link);
            }
        }
        foreach (XmlElement link in link_tobedelete)
        {
            link.ParentNode.RemoveChild(link);
        }
    }
    static string doc2string(XmlDocument doc)
    {

        StringBuilder sb = new StringBuilder();
        XmlWriterSettings settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\r\n",
            NewLineHandling = NewLineHandling.Replace,
            Encoding = Encoding.UTF8
        };
        using (XmlWriter writer = XmlWriter.Create(sb, settings))
        {
            doc.Save(writer);
        }
        var r = Regex.Replace(sb.ToString(), "<\\?xml version=\"1.0\".*?>", "<?xml version=\"1.0\"?>");
        return r.Replace("<!DOCTYPE html []", "<!DOCTYPE html");


    }

    static int interlude_count = 0;
    static string MapFileName(string readableName)
    {
        string lowered = readableName;
        string numberMap = "１①Ⅰ";
        foreach (char c in numberMap)
        {
            int block = (int)c - 1;
            for (int i = 0; i <= 9; i++)
            {
                char numberChar = Convert.ToChar(block + i);
                lowered = lowered.Replace(numberChar, (char)('0' + i));
            }
        }
        string trimmed = lowered.Replace("　", "");
        //Name dic start
        Dictionary<string, string> name_dic = new Dictionary<string, string>
                    {
                        {"目次","toc"},
                        {"プロローグ","prologue"},
                        {"エピローグ","epilogue"},
                        {"あとがき","postscript"},
                        {"インタールード","interlude"},
                        {"幕間","interlude"},
                        {"書き下ろし","additional"},
                        {"特典","special"},
                        {"最終章","chapter_final"}
                    };

        foreach (var k in name_dic)
        {
            if (trimmed.Contains(k.Key))
            {
                if (k.Value == "interlude")
                {
                    interlude_count++;
                    return k.Value + interlude_count;
                }
                return k.Value;
            }
        }
        //name dic end

        //chapter number
        {
            string t = trimmed;
            string[] chapterNumberPatterns = new string[]{
                        "^第([一二三四五六七八九十百零\\d]{1,10})",
                        "([一二三四五六七八九十百零\\d]{1,10})\\s",
                        "([一二三四五六七八九十百零\\d]{1,10})章"
                        };
            foreach (string pattern in chapterNumberPatterns)
            {
                var m_num = Regex.Match(t, pattern);
                if (m_num.Success)
                {
                    string chapterNumber = m_num.Groups[1].Value;
                    if (!char.IsDigit(chapterNumber[0])) chapterNumber = "" + Util.FromChineseNumber(chapterNumber);

                    return
                        "chapter"
                        + (chapterNumber.Length == 1 ? "0" : "") + chapterNumber;

                }
            }
        }
        //chapter numder end

        // extra
        trimmed = trimmed.Replace("番外編", "extra");

        //just keep ascii
        {
            string t = trimmed;
            string name = "";
            for (int i = 0; i < t.Length; i++)
            {
                if (t[i] < 128)
                {
                    if (t[i] == ' ')
                    {
                        if (i == t.Length - 1) continue;
                        if (name.EndsWith('_')) continue;
                        name += '_'; continue;
                    }
                    if (t[i] == '_' && name.EndsWith('_')) continue;
                    name += t[i];
                }
            }
            if (name.EndsWith('_')) name = name.Substring(0, name.Length - 1);
            return name;
        }
    }


    const string nav_template = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:epub=""http://www.idpf.org/2007/ops"">
<head>
<meta charset=""UTF-8""/>
<title>Navigation</title>
</head>
<body>

<nav epub:type=""toc"" id=""toc"">
<h1>Navigation</h1>

<ol>
{content}
</ol>

</nav>
</body>
</html>";
    const string cover_xhtml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:epub=""http://www.idpf.org/2007/ops"">
<head><title>Cover</title></head>
<body epub:type=""cover""><div style=""text-align: center; padding: 0; margin: 0;"">
<img style=""max-height: 100%; max-width: 100%; width: auto; height: auto; margin: 0; padding: 0;"" src=""../Images/{filename}"" />
</div></body></html>";
}