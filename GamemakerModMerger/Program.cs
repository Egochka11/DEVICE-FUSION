// See https://aka.ms/new-console-template for more information

using GamemakerModMerger;
using System.IO;
using System.Reflection;
using UndertaleModLib;
string programLocation = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
//Console.WriteLine(programLocation);

Console.WriteLine("give me a path to a data.win file");
var datapath = Console.In.ReadLine();
Console.WriteLine("give me a path to a xdelta file");
var xdeltapath = Console.In.ReadLine();
UndertaleData data;
try
{
    using var stream = new FileStream(datapath, FileMode.Open, FileAccess.Read);
    data = UndertaleIO.Read(stream, (warning, _) => { Console.WriteLine("A warning occured while trying to load " + datapath + ": " + warning); Console.Error.Close(); });
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
    return;
}
Directory.CreateDirectory("cache");
Console.WriteLine("string index 100 is " + data.Strings[100].ToString());
File.WriteAllText("cache\\testfile.txt", "lol");
Console.ForegroundColor = ConsoleColor.Cyan;

await xdelta_patches.PatchXdelta(datapath, xdeltapath, programLocation + "\\cache\\data.win");