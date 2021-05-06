using System;
using System.IO;

public class Log
{
    static string t = "";
    public static string level = "";
    public static void log(string s)
    {
        t += level+s + "\r\n";
        if(s.StartsWith("[Warn")){Console.ForegroundColor=ConsoleColor.Yellow;}
        if(s.StartsWith("[Error")){Console.ForegroundColor=ConsoleColor.Red;}
        if(s.StartsWith("[Info")){Console.ForegroundColor=ConsoleColor.Green;}
        Console.WriteLine(level+s);
        Console.ForegroundColor=ConsoleColor.White;
    }
    public static void Info(string s){
        log("[Info]"+s);
    }
    public static void Warn(string s)
    {
        log("[Warn]"+s);
    }
    public static void Error(string s){
        log("[Error]"+s);
    }
    public static void Save(string path)
    {
        File.WriteAllText(path, t);
    }

}




