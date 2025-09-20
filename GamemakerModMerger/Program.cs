// See https://aka.ms/new-console-template for more information

using GamemakerModMerger;
using System.Reflection;
using UndertaleModLib;
Console.ForegroundColor = ConsoleColor.Green;


string programLocation = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
//Console.WriteLine(programLocation);

Console.WriteLine("THE STORY IN PUREST FORM.");
var datapath = Console.In.ReadLine();
Console.WriteLine("THE AMOUNT OF ALTERATIONS.");
var totalPatches = uint.Parse(Console.In.ReadLine());

var patches = new List<string>();
for (uint i = 0; i < totalPatches; i++)
{
    Console.WriteLine($"DELTA {i+1}.");
    patches.Add(Console.ReadLine());
}

Directory.CreateDirectory("cache\\patchedData");

File.Copy(datapath, "cache\\patchedData\\0.win", true);
Console.WriteLine("APPLYING THE DELTAS SEPARATELY.");
await XdeltaPatching.PatchXDelta(datapath, patches, programLocation + "\\cache\\patchedData");

Console.WriteLine("DECONSTRUCTING THE DELTA STORIES.");
List<UndertaleData> datas = [];
for (uint i = 0; i <= totalPatches; i++)
{
    using FileStream fileStream = new($"cache\\patchedData\\{i}.win", FileMode.Open, FileAccess.Read);
    datas.Add(UndertaleIO.Read(fileStream));
}

Console.WriteLine("MIGRATING IMAGES.");
SpriteMerger.Merge(datas);
Console.WriteLine("COMBINING DEVICES.");
GameObjectMerger.Merge(datas);
Console.WriteLine("FUSING CODE.");
CodeMerger.Merge(datas);

using (FileStream fileStream = new($"cache\\patchedData\\data.win", FileMode.Create, FileAccess.Write))
{
    UndertaleIO.Write(fileStream, datas[0]);
}

Console.WriteLine("FUSION IS COMPLETE. FAREWELL.");