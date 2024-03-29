using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using AeroEpub;

/// <summary>
///  电子书文档相关
/// </summary>
class EbookTools
{
    /// <summary>
    //将纯txt稿与生肉生成的注释文档合并。
    /// </summary>
    public static void MergeAtxt(string raw_path, string text_path, string output_path = "temp.atxt")
    {
        string[] raw = File.ReadAllLines(raw_path);
        string[] text = File.ReadAllLines(text_path);
        int i = 0;
        string error_temp = "";
        try
        {

            foreach (var t in text)
            {
                error_temp = t;
                if (!t.StartsWith("#"))
                {
                    while ((raw[i].Length > 0) ? (raw[i][0] == '#') : false) i++;
                    raw[i] = t;
                    i++;
                }
            }

        }
        catch (IndexOutOfRangeException)
        { Log.Error($"Index out of range: {error_temp} "); }

        File.WriteAllLines(output_path, raw);
        Log.Info("Saved " + output_path);
    }

    public static void RenameEpub(string path)
    {
        EpubFile epub = new EpubFile(path);
        string[] creators = epub.creators;
        string creatorStr = "";
        if (creators.Length > 0)
        {
            creatorStr = $"[{creators[0]}] ";
            //creatorStr = $"[{String.Join(',',creators)}] ";
        }
        string dstFilename = creatorStr + epub.title + ".epub";
        dstFilename = Util.FilenameCheck(dstFilename);
        File.Move(path, Path.Combine(Path.GetDirectoryName(path), dstFilename));
        Log.Info(path + " -> " + dstFilename);
    }

    public static void ConcatenateText(string dir_path, string[] names)
    {
        List<string> txts = new List<string>();
        txts.AddRange(Directory.GetFiles(dir_path, "*.txt"));
        txts.AddRange(Directory.GetFiles(dir_path, "*.atxt"));
        txts.Sort();
        string output_path = Path.Combine(dir_path, "c");
        Directory.CreateDirectory(output_path);
        StringBuilder temp = new StringBuilder();
        string temp_filename = Path.GetFileNameWithoutExtension(txts[0]);
        int i = 0;
        foreach (string txt in txts)
        {
            string n = Path.GetFileNameWithoutExtension(txt);
            if (i < names.Length)
                if (n.StartsWith(names[i]))
                {
                    File.WriteAllText(
                        Path.Combine(output_path, temp_filename + ".txt"),
                        temp.ToString()
                        );
                    temp.Clear();
                    temp_filename = n;
                    i++;
                }
            string s = File.ReadAllText(txt);
            temp.Append(s);
        }
        File.WriteAllText(
            Path.Combine(output_path, temp_filename + ".txt"),
            temp.ToString()
            );

    }

    /// <summary>
    ///  将KindleUnpack的epub和DumpAZW6.py的高清图合并，必须存在封面图用于计算offset。在高清图文件夹中创建文件夹"rename"存放输出。
    /// </summary>
    public static void RenameHDImageToKindleUnpackEpub(string imgDir, string hdimgDir)
    {
        Directory.CreateDirectory(Path.Combine(hdimgDir, "rename"));
        string[] imgs = Directory.GetFiles(imgDir);
        int offset = 0;
        string covername = "";
        for (int i = 0; i < imgs.Length; i++)
        {
            imgs[i] = Path.GetFileName(imgs[i]);
            if (imgs[i].Contains("cover"))
            {
                Match m = Regex.Match(imgs[i], "cover([0-9]{5})");
                offset = int.Parse(m.Groups[1].Value);
                covername = imgs[i];
                break;
            }
        }
        string[] hdimgs = Directory.GetFiles(hdimgDir);
        List<string> hdimgs_ = new List<string>(hdimgs);
        hdimgs_.Sort();
        string hdcover = hdimgs_[hdimgs_.Count - 1];
        Regex hdnum = new Regex("HDimage([0-9]{5})");
        hdimgs_.RemoveAt(hdimgs_.Count - 1);
        {
            Match m = hdnum.Match(hdcover);
            offset = offset - int.Parse(m.Groups[1].Value);
        }
        File.Copy(hdcover, Path.Combine(hdimgDir, "rename", covername));
        //offset=107;//没找到封面需要手动指定offset时注释掉上面那句
        foreach (string n in hdimgs_)
        {
            Match m = hdnum.Match(Path.GetFileName(n));
            if (!m.Success) continue;
            string name = "image" + Util.Number(int.Parse(m.Groups[1].Value) + offset, 5) + Path.GetExtension(n);
            File.Copy(n, Path.Combine(hdimgDir, "rename", name));
        }
    }

    /// <summary>
    //输出ID
    /// </summary>
    public static void ExportEpubIdentifier(string rootPath)
    {
        foreach (string file in Directory.EnumerateFiles(rootPath, "*.epub", SearchOption.AllDirectories))
        {
            EpubFile epub = new EpubFile(file);
            epub.ReadMeta();
            string id = epub.uniqueIdentifier.value;
            string relativePath = Path.GetRelativePath(rootPath, file);
            Log.log(id + " " + relativePath);
        }
        Log.Save("log.txt");
    }


    public static int CharacterCount(EpubFile epub)
    {
        epub.ReadMeta();
        int count = 0;
        foreach (var itemref in epub.spine)
        {
            var file = (TextEpubFileEntry)itemref.item.GetFile();
            count += Regex.Replace(file.text, "<[\\s\\S]*?>", "").Length;
        }
        return count;
    }


    public static void ChangeWritingDirection(EpubFile epub)
    {
        epub.ReadMeta();
        epub.spine.pageProgressionDirection = "ltr";
        foreach (var pair in epub.manifest)
        {
            if (pair.Value.mediaType == "text/css" || pair.Value.mediaType == "application/xhtml+xml")
            {
                var entry = pair.Value.GetFile() as TextEpubFileEntry;
                entry.text = entry.text.Replace("vertical-rl", "horizontal-tb");
            }
        }
        epub.WriteMeta3();
    }
}