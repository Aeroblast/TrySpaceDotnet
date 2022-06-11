
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using AeroEpub;

class AozoraText
{
    public static void FormatDir(string dir)
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
        var indentChar = new char[] { '『', '「', '（' };
        var noSpaceChar = new char[] { '！', '？', '!', '?', '」', '　' };
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

    const string xhtml_template = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE html>
<html
 xmlns=""http://www.w3.org/1999/xhtml""
 xmlns:epub=""http://www.idpf.org/2007/ops""
 xml:lang=""ja""
 class=""vrtl""
>
<head>
  <meta charset=""UTF-8""/>
  <title>{title}</title>
  <link rel=""stylesheet"" type=""text/css"" href=""../style/book-style.css""/>
</head>
<body>
{body}
</body>
</html>";
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