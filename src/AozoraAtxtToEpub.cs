
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using AeroEpub;

class AozoraText
{

    public static EpubFile AtxtEpub2JaStyle(string epub_path)
    {
        EpubFile epub = new EpubFile(epub_path);
        epub.ReadMeta();
        epub.GetFile<TextEpubFileEntry>("META-INF/container.xml").text = container;
        var opf = epub.packageFile;
        opf.fullName = "item/standard.opf";

        epub.entries.Remove(epub.GetFile("OEBPS/toc.ncx"));
        Regex reg_html_tag = new Regex("<html[\\w\\W].*?>");
        foreach (var entry in epub.entries)
        {
            if (entry.fullName.StartsWith("OEBPS/Text/") && entry.fullName.Length > "OEBPS/Text/".Length)
            {
                //假定：均有“有意义的名称”
                entry.fullName = "item/xhtml/p-" + Path.GetFileName(entry.fullName).Substring("atxt00_".Length);
                var textEntry = entry as TextEpubFileEntry;
                textEntry.text = textEntry.text
                .Replace("../Styles/Style.css", "../style.css")
                .Replace(" class=\"atxt_drawout\"", "")
                .Replace("atxt_breakall", "line-break-loose word-break-break-all")
                ;
                textEntry.text = Regex.Replace(
                    textEntry.text,
                    "<div class=\"atxt_aligned atxt_illu.*?/Images/(.*?\\.jpg)\".*?</div>",
                    "<p><img class=\"fit\" src=\"../image/$1\" alt=\"\"/></p>"
                    );
                switch (Path.GetFileNameWithoutExtension(entry.fullName))
                {
                    case "p-colophon":
                        textEntry.text = reg_html_tag.Replace(textEntry.text, html_tag_hltr, 1);
                        break;
                    default:
                        textEntry.text = reg_html_tag.Replace(textEntry.text, html_tag_vrtl, 1);
                        break;
                }
            }
            if (entry.fullName.StartsWith("OEBPS/Images/") && entry.fullName.Length > "OEBPS/Images/".Length)
            {
                entry.fullName = "item/image/" + Path.GetFileName(entry.fullName);
            }
            if (entry.fullName.EndsWith("nav.xhtml"))
            {
                entry.fullName = "item/navigation-documents.xhtml";
                var textEntry = entry as TextEpubFileEntry;
                textEntry.text = Regex.Replace(textEntry.text, "href=\"Text/atxt[0-9]{2}_(.*?)\"", "href=\"xhtml/p-$1\"");
            }
            if (entry.fullName.EndsWith(".css"))
            {
                entry.fullName = "item/style.css";
                var textEntry = entry as TextEpubFileEntry;
                var t = "/* End of AeroNovelTool Template CSS */";
                textEntry.text = textEntry.text.Substring(textEntry.text.IndexOf(t) + t.Length + 1);
            }
        }
        epub.DeleteEmpty();//每次只查了一层所以两遍
        epub.DeleteEmpty();
        epub.manifest.Remove("ncx");
        epub.manifest["nav.xhtml"].href = "navigation-documents.xhtml";
        var cssItem = epub.manifest["Style.css"];
        cssItem.href = "style.css";
        cssItem.id = "style-ja";
        foreach (var (id, item) in epub.manifest)
        {
            item.id = Path.GetFileNameWithoutExtension(item.id);
            if (item.href.StartsWith("Text/"))
            {
                item.id = "p-" + item.id.Substring("atxt00_".Length);
                item.href = "xhtml/p-" + Path.GetFileName(item.href).Substring("atxt00_".Length);
            }
            if (item.href.StartsWith("Images/"))
            {
                item.href = "image/" + Path.GetFileName(item.href);
            }

        }
        epub.spine.pageProgressionDirection = "rtl";
        epub.spine.toc = null;
        epub.WriteMeta3();
        return epub;
    }
    public static void FormatAtxtDir(string dir)
    {
        var x = Directory.GetFiles(dir, "*.atxt");
        var iter = x.Where(f =>
        {
            var num = Path.GetFileName(f).Substring(0, 2).ToCharArray();
            return Array.TrueForAll(num, c => Char.IsDigit(c));
        });
        foreach (var f in iter)
        {
            FormatAtxt(f);
        }
    }
    public static void FormatAtxt(string txt_path)
    {
        var indentChar = new char[] { '『', '「', '（', '〝' };
        var noSpaceChar = new char[] { '！', '？', '!', '?', '」', '　', '―', '…', '）' };
        var lines = File.ReadAllLines(txt_path);
        var result = new List<string>();
        foreach (var line in lines)
        {
            if (line.StartsWith('#'))
            {
                result.Add(line);
                continue;
            }
            if (line.Length == 0)
            {
                result.Add(line);
                continue;
            }
            var sb = new StringBuilder();
            bool expectBreak = false;
            foreach (var rune in line.Trim().EnumerateRunes())
            {
                var s = rune.ToString();
                switch (s)
                {
                    case "？":
                    case "！":
                    case "!":
                    case "?":
                        expectBreak = true;
                        sb.Append(rune);
                        continue;
                }
                if (expectBreak)
                {
                    if (!Array.Exists(noSpaceChar, c => c == s[0]))
                    {
                        sb.Append('\u3000');
                    }
                }
                switch (s)
                {
                    case "\u2015"://日文
                    case "\u2014"://中文——
                        sb.Append("\u2015");
                        break;
                    default:
                        sb.Append(rune);
                        break;
                }
                expectBreak = false;

            }
            var r = sb.ToString();
            if (!Array.Exists(indentChar, c => r[0] == c))
            {
                r = "　" + r;
            }
            result.Add(r);

        }
        File.WriteAllLines(txt_path, result);
    }

    const string html_tag_vrtl = @"<html
 xmlns=""http://www.w3.org/1999/xhtml""
 xmlns:epub=""http://www.idpf.org/2007/ops""
 xml:lang=""ja""
 class=""vrtl""
>";

    const string html_tag_hltr = @"<html
 xmlns=""http://www.w3.org/1999/xhtml""
 xmlns:epub=""http://www.idpf.org/2007/ops""
 xml:lang=""ja""
 class=""hltr""
>";
    const string container =
   "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">\n    <rootfiles>\n        <rootfile full-path=\"item/standard.opf\" media-type=\"application/oebps-package+xml\"/>\n    </rootfiles>\n</container>";

    const string package_template = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<package
 xmlns=""http://www.idpf.org/2007/opf""
 version=""3.0""
 xml:lang=""ja""
 unique-identifier=""BookId""
 prefix=""rendition: http://www.idpf.org/vocab/rendition/#
         ebpaj: http://www.ebpaj.jp/
         fixed-layout-jp: http://www.digital-comic.jp/
         access: http://www.access-company.com/2012/layout#
         ibooks: http://vocabulary.itunes.apple.com/rdf/ibooks/vocabulary-extensions-1.0/""
>
{metadata}
<manifest>
{items}
</manifest>

<spine page-progression-direction=""rtl"">
{itemrefs}
</spine>

</package>
";
}