using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemakerModMerger;
public static class Gaster
{
    public static bool GasterAlt = false;

    public static void WriteLine(string message)
    {
        Console.WriteLine(message);
        Program.FileStream.Write(Encoding.UTF8.GetBytes(message + Environment.NewLine));
    }
    public static void WriteLine(string message, string altMessage)
    {
        Console.WriteLine(GasterAlt ? altMessage : message);
        Program.FileStream.Write(Encoding.UTF8.GetBytes(message + Environment.NewLine)); // gaster is too cryptic to be logged
        // 🐘 ADDRESS ME.
    }

}