using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

/// <summary>
///  用谷歌文档进行OCR后，下载docx处理。
/// </summary>
class FormatOCRText
{
    public static string[] ProcessFile(string path)
    {
        var r = GetOutput("pandoc.exe", $"\"{path}\" -t plain");
        r = Regex.Replace(r, "\r\n\r\n", "\r\n");
        r = Regex.Replace(r, " ", "");
        r = Regex.Replace(r, "[.•・·]{2,99}", m => new string('…', m.Value.Length / 3));
        r = r.Replace("[]", "");//pandoc 处理图片剩下的东西
        var lines = r.Split("\r\n");
        var processedLines = new List<string>();
        var knownAllow = new string[] {
            "「", "」", "『", "』", "、", "。","々" ,
            "！", "？", "《", "》", "…", "?", "!" ,"*","〝","〟",
            "←", "→", "=", "☆", "♪"
            };
        var knownForbi = new string[] { "[", "]" };
        var indentChar = new char[] { '『', '「', '（' };
        bool inText = false;
        foreach (var line_ in lines)
        {
            var line = line_;
            if (line.StartsWith("#"))
            {
                processedLines.Add(line);
                continue;
            }
            line = Regex.Replace(line, "[0-9a-zA-Z]+", m =>
            {
                if (m.Length == 2)
                {
                    return m.ToString();

                }
                else
                {
                    var hw = m.ToString();
                    return HW2FW(hw);
                }

            });
            line = Regex.Replace(line, "[！？!?]+", m =>
            {
                var all = m.ToString().Replace("?", "？").Replace("!", "！");
                if (all.Length > 2) Log.Warn("too many !? ：" + line);
                if (all.Length == 2) return all.Replace("？", "?").Replace("！", "!");
                return all;

            });
            var sb = new StringBuilder();
            foreach (var rune in line.EnumerateRunes())
            {
                var s = rune.ToString();
                switch (s)
                {
                    case "\u2015"://日文
                    case "\u2014"://中文——
                    case "─":
                    case "|":
                    case "-":
                        sb.Append("─");
                        continue;
                    case "~":
                    case "〜":
                        sb.Append("～");
                        continue;
                    case "(":
                        sb.Append("（");
                        continue;
                    case ")":
                        sb.Append('）');
                        continue;
                    case "○":
                    case "〇":
                        sb.Append('〇');
                        continue;
                }
                if (Array.Exists(knownAllow, x => s == x))
                {
                    sb.Append(s);
                    continue;
                }
                if (Array.Exists(knownForbi, x => s == x))
                {
                    continue;
                }

                var current = CodeHandling.GetCodeType(rune);
                switch (current)
                {
                    case CodeHandling.CodeType.CJKChar:
                    case CodeHandling.CodeType.HWLetterNumber:
                    case CodeHandling.CodeType.FWLetterNumber:
                    case CodeHandling.CodeType.FWSpace:
                        sb.Append(rune);
                        break;

                    default:
                        Console.WriteLine(rune);
                        throw new Exception($"File: {Path.GetFileName(path)}\nTarget:{line}");
                        //sb.Append(rune);break;// for all through
                }

            }
            var processedLine = sb.ToString();

            if (processedLine.Length == 0)
            {
                if (inText)
                {
                    processedLines.Add("");
                }
                continue;
            }
            inText = true;

            processedLine = Regex.Replace(processedLine, "[─]+",
             m => m.ToString().Length <= 1 ? "─" : m.ToString()
            );
            if (!Array.Exists(indentChar, x => processedLine[0] == x))
            {
                processedLine = "　" + processedLine;
            }
            processedLines.Add(processedLine);
        }
        while (processedLines[^1] == "")
        {
            processedLines.RemoveAt(processedLines.Count - 1);
        }
        return processedLines.ToArray();

    }
    public static string ProcessDir(string path)
    {
        var list = new List<string>(Directory.GetFiles(path, "*.docx"));
        list.Sort();
        var result = new List<string>();
        int lastNoramlLine = 1;
        foreach (var f in list)
        {
            result.Add($"## {Path.GetFileNameWithoutExtension(f)}");
            lastNoramlLine++;
            var lines = ProcessFile(f);
            if (lines[0].StartsWith("#illu"))
            {
                result.Add(lines[0]);
                lastNoramlLine++;
                continue;
            }
            int i = 0;
            for (; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("#"))
                {
                    lastNoramlLine++;
                    result.Add(lines[i]);
                    continue;
                }

                if (lastNoramlLine <= result.Count && Regex.IsMatch(result[^lastNoramlLine].Trim(), "[^。」』、？！*]$"))
                    result[^lastNoramlLine] += lines[i].Trim();
                else
                {
                    result.Add(lines[i]);
                    lastNoramlLine = 1;
                }

            }

        }
        var concated = string.Join("\r\n", result);
        var sb = new StringBuilder();
        int lastPos = 0;
        char[] brankets = { '「', '」' };
        int pos = concated.IndexOfAny(brankets);//策略可能看情况定。此处假定对话必然单独段落
        while (pos > 0)
        {
            var c = concated[pos];
            if (c == '「')
            {
                var c_1 = concated[pos - 1];
                if (c_1 != '\r' && c_1 != '\n' && c_1 != '」')
                {
                    sb.Append(concated.Substring(lastPos, pos - lastPos));
                    sb.Append(Environment.NewLine);
                }
                else
                {
                    sb.Append(concated.Substring(lastPos, pos - lastPos));
                }
            }
            else
            {
                // '」'
                if (pos + 1 < concated.Length)
                {
                    var c_1 = concated[pos + 1];
                    if (c_1 != '\r' && c_1 != '\n')
                    {
                        sb.Append(concated.Substring(lastPos, pos - lastPos + 1));
                        sb.Append(Environment.NewLine);
                        if (concated[pos + 2] != '「')
                        {
                            sb.Append('　');// Full width 
                        }
                        pos++;
                    }
                    else
                    {
                        sb.Append(concated.Substring(lastPos, pos - lastPos));
                    }
                }
            }
            lastPos = pos;

            pos = concated.IndexOfAny(brankets, pos + 1);
        }
        sb.Append(concated.Substring(lastPos, concated.Length - lastPos));
        return sb.ToString();
    }
    static string HW2FW(string s)
    {
        string r = "";
        foreach (char c in s)
        {
            if (Char.IsDigit(c))
            {
                r += Char.ConvertFromUtf32((int)c + '０' - '0');
            }
            else if (Char.IsUpper(c))
            {
                r += Char.ConvertFromUtf32((int)c + 'Ａ' - 'A');
            }
            else
            {
                r += Char.ConvertFromUtf32((int)c + 'ａ' - 'a');
            }
        }
        return r;
    }


    public static string GetOutput(string exe, string args)
    {
        Process process = new Process();
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.StartInfo.FileName = exe;
        process.StartInfo.Arguments = args;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        string r = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return r;
    }

}