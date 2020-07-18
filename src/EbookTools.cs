using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

/// <summary>
///  电子书文档相关
/// </summary>
class EbookTools
{
    //将纯txt稿与生肉生成的注释文档合并。
    public static void MergeAtxt(string raw_path, string text_path, string output_path = "temp.atxt")
    {
        string[] raw = File.ReadAllLines(raw_path);
        string[] text = File.ReadAllLines(text_path);
        int i = 0;
        foreach (var t in text)
        {
            if (!t.StartsWith("#"))
            {
                while ((raw[i].Length > 0) ? (raw[i][0] == '#') : false) i++;
                raw[i] = t;
                i++;
            }
        }
        File.WriteAllLines(output_path, raw);
        Log.Info("Saved" + output_path);
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
}