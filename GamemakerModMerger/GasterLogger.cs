using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemakerModMerger;
public static class GasterLogger
{
    public static void Log(string message)
    {
        Console.WriteLine(message);
    }
    public static void Log(string message, string gasterAlt)
    {
        Console.WriteLine(message);
        Console.WriteLine(gasterAlt);
    }

}