using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AeroEpub;
public class UnpackEpubManga
{
    public string outputRoot = "";
    EpubFile epub;
    static public void Unpack(string epubPath)
    {
        var a = new UnpackEpubManga(epubPath);
        a.Proc();
    }
    public UnpackEpubManga(string epubPath)
    {
        epub = new EpubFile(epubPath);
    }
    public void Proc()
    {
        if (string.IsNullOrEmpty(outputRoot))
        {
            outputRoot = FilenameCheck(epub.title);
            if (!Directory.Exists(outputRoot)) Directory.CreateDirectory(outputRoot);
        }
        try
        {
            if (epub.toc.mediaType == "application/x-dtbncx+xml")
            {
                Parse2();
            }
            else
            {
                Parse3();
            }
        }
        catch (Exception)
        {
            Log.Warn("尝试序列化失败。中止执行。");
            return;
            // tocTree = null;
        }

        var plain = GetPlainStruct();
        int pageCount = 1;
        Regex imageTag = new Regex("<image .*? xlink:href=\"(.*?)\" />");
        string chapter = "";
        for (int i = 0; i < plain.Length; i++)
        {
            if (plain[i] != null)
            {
                chapter = FilenameCheck(plain[i]);
                string outputDir = Path.Combine(outputRoot, chapter);
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
                pageCount = 1;
            }
            var t = epub.spine[i].item.GetFile() as TextEpubFileEntry;
            var m = imageTag.Match(t.text);
            var imageHref = m.Groups[1].Value;
            string imagePath = ReferPath(epub.spine[i].item.filePath, imageHref);
            var imageEntry = epub.GetFile(imagePath);
            string outputPath = Path.Combine(outputRoot, chapter, pageCount.ToString("D3") + "_" + Path.GetFileName(imageHref));
            Log.Info(outputPath);
            File.WriteAllBytes(outputPath, imageEntry.GetBytes());

            pageCount++;
        }
    }
    TocItem tocTree;
    string tocPath;
    void Parse2()
    {
        var f = epub.toc.GetFile() as TextEpubFileEntry;
        tocPath = f.fullName;
        XmlDocument xml = new XmlDocument();
        xml.LoadXml(f.text);
        var root = xml.GetElementsByTagName("navMap")[0];
        tocTree = new TocItem(epub);
        tocTree.children = new List<TocItem>();
        Parse2Helper(root, tocTree);

    }
    void Parse2Helper(XmlNode px, TocItem pt)
    {
        foreach (XmlNode e in px.ChildNodes)
        {
            switch (e.Name)
            {
                case "navLabel":
                    {
                        pt.name = e.InnerText;
                    }
                    break;
                case "content":
                    {
                        pt.url = ReferPath(tocPath, e.Attributes["src"].Value);
                    }
                    break;
                case "navPoint":
                    {
                        var n = pt.AddChild();
                        Parse2Helper(e, n);
                    }
                    break;
            }
        }
    }
    //http://idpf.org/epub/30/spec/epub30-contentdocs.html#sec-xhtml-nav-def-model
    public void Parse3()
    {
        var f = epub.toc.GetFile() as TextEpubFileEntry;

        tocPath = f.fullName;
        XmlDocument xml = new XmlDocument();
        xml.LoadXml(f.text);
        var navs = xml.GetElementsByTagName("nav");
        foreach (XmlElement nav in navs)
        {
            if (nav.GetAttribute("epub:type") == "toc")
            {
                tocTree = new TocItem(epub);
                tocTree.children = new List<TocItem>();
                var root = nav.GetElementsByTagName("ol")[0];
                Parse3Helper(root, tocTree);
                return;
            }
        }
        //We have <nav>, but no epub:type is toc, so last try:
        if (navs.Count > 0)
        {
            var nav = navs[0] as XmlElement;
            tocTree = new TocItem(epub);
            tocTree.children = new List<TocItem>();
            var root = nav.GetElementsByTagName("ol")[0];
            Parse3Helper(root, tocTree);
        }
    }
    void Parse3Helper(XmlNode px, TocItem pt)
    {
        foreach (XmlNode e in px.ChildNodes)
            if (e.Name == "li")
            {
                var node = pt.AddChild();
                foreach (XmlNode a in e.ChildNodes)
                {
                    if (a.Name == "a" && node.name == "")
                    {
                        node.name = a.InnerText;
                        node.url = ReferPath(tocPath, ((XmlElement)a).GetAttribute("href"));
                        continue;
                    }
                    if (a.Name == "span" && node.name == "")
                    {
                        node.name = a.InnerText;
                        continue;
                    }
                    if (a.Name == "ol")
                    {
                        Parse3Helper(a, node);
                    }
                }
            }
    }
    public string[] GetPlainStruct()
    {
        List<string> urls = new List<string>();
        foreach (Itemref i in epub.spine)
        {
            if (!i.linear) continue;
            urls.Add(i.filePath);
        }
        string[] plain = new string[urls.Count];
        if (tocTree == null)
        {
            for (int i = 0; i < plain.Length; i++) plain[i] = "";
            return plain;
        }
        GetPlainStructHelper(urls, tocTree, ref plain);
        return plain;
    }
    static void GetPlainStructHelper(List<string> urls, TocItem p, ref string[] plain, string intro = "")
    {
        foreach (TocItem i in p.children)
        {
            if (i.url != null)
            {
                string u = i.url.Split('#')[0];
                int index = urls.IndexOf(u);
                if (index >= 0)
                {
                    if (plain[index] == null)
                        plain[index] = intro + i.name;
                }
            }
            if (i.children != null)
                GetPlainStructHelper(urls, i, ref plain, intro + i.name + " > ");
        }
    }

    public static string ReferPath(string filename, string refPath)
    {
        string r = Path.GetDirectoryName(filename);
        string[] parts = refPath.Replace('/', '\\').Split('\\');
        foreach (string p in parts)
        {
            if (p == "") continue;

            if (p == "..") { r = Path.GetDirectoryName(r); continue; }
            r = Path.Combine(r + "/", p);
        }
        return r;
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

    class TocItem
    {
        EpubFile belongTo;
        public TocItem(EpubFile epub)
        {
            belongTo = epub;
        }
        public List<TocItem> children;
        public TocItem parent;
        string _name = "";
        public string name
        {
            get { return _name; }
            set { _name = Util.Trim(value); }
        }
        string _url;
        public string url
        {
            set
            {
                _url = value;
                int i = 0;
                var spl = _url.Split('#');
                var path = spl[0];
                foreach (Itemref itemref in belongTo.spine)
                {
                    if (itemref.filePath == path)
                    {
                        docIndex = i;
                        return;
                    }
                    i++;
                }
                throw new EpubErrorException("Error at parse toc");
            }
            get { return _url; }
        }
        public int docIndex;
        public TocItem AddChild()
        {
            if (children == null) children = new List<TocItem>();
            TocItem n = new TocItem(belongTo);
            n.parent = this;
            children.Add(n);
            return n;
        }
        public override string ToString()
        {
            string s = name;
            if (parent != null)
            {
                var t = parent.ToString();
                if (t.Length > 0)
                    s = parent.ToString() + " > " + s;
            }
            return s;
        }
    }

}