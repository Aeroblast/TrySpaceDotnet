using System;
using System.IO;
class DumpFromBinary
{

    static void ScanImage(byte[] dump)
    {
        long jpg_start = 0;
        bool jpg_foundstart = false;
        long png_start = 0;
        bool png_foundstart = false;
        int count = 0;
        for (long i = 0; i < dump.Length - 3; i++)
        {
            if (dump[i] == 0xff)
            {
                if (dump[i + 1] == 0xd8)
                {
                    jpg_foundstart = true;
                    jpg_start = i;
                }
                if (dump[i + 1] == 0xd9 && jpg_foundstart)
                {
                    count++;
                    jpg_foundstart = false;
                    long l = i + 1 - jpg_start;

                    string out_path = count + ".jpg";
                    using (var fs = File.OpenWrite(out_path))
                        fs.Write(dump, (int)jpg_start, (int)l);
                    Console.WriteLine($"Start:{jpg_start:x8} End:{jpg_start + l:x8} Size-{l}B");

                }
            }

            if (dump[i] == 0x89 && dump[i + 1] == 0x50 && dump[i + 2] == 0x4E && dump[i + 3] == 0x47)
            {
                png_foundstart = true;
                png_start = i;
            }



            if (png_foundstart && dump[i] == 0xae && dump[i + 1] == 0x42 && dump[i + 2] == 0x60 && dump[i + 3] == 0x82)
            {
                count++;
                png_foundstart = false;
                long l = i + 1 - png_start;
                //if (sizeDic.ContainsKey(l + 34))
                if (l > 500000)
                {
                    //var name = sizeDic[l + 34];
                    string out_path = count + ".jpg";
                    using (var fs = File.OpenWrite(out_path))
                        fs.Write(dump, (int)png_start, (int)l);
                    Log.Info($"Start:{png_start:x8} End:{png_start + l:x8} Size-{l}B");
                }
            }
        }
    }
}