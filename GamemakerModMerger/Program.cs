// See https://aka.ms/new-console-template for more information

using GamemakerModMerger;
using System.Reflection;
using Underanalyzer.Decompiler;
using UndertaleModLib;

try {

Gaster.WriteLine("Before beginning, would you like to enable Gaster mode? (y/n) WARNING: Gaster mode is kind of cryptic so it is recommended to use only if you're familiar with the program");
Gaster.GasterAlt = Console.ReadLine() == "y";
if (Gaster.GasterAlt) Console.ForegroundColor = ConsoleColor.Green;


string programLocation = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
//Console.WriteLine(programLocation);

Gaster.WriteLine("The original data.win file:", "THE STORY IN PUREST FORM.");
var datapath = Console.ReadLine();
Gaster.WriteLine("Where the file should be placed INCLUDING THE FILE NAME AND EXTENSION:", "THE DESTINATION OF FUSION.");
var savepath = Console.ReadLine();
Gaster.WriteLine("Patch amount:", "THE AMOUNT OF ALTERATIONS.");
var totalPatches = uint.Parse(Console.ReadLine());

var patches = new List<string>();
for (uint i = 0; i < totalPatches; i++)
{
    Gaster.WriteLine($"Patch {i + 1}:", $"DELTA {i+1}.");
    patches.Add(Console.ReadLine());
}

Directory.CreateDirectory("cache\\patchedData");

File.Copy(datapath, "cache\\patchedData\\0.win", true);
Gaster.WriteLine("Patching mods into separate files...", "APPLYING THE DELTAS SEPARATELY.");
await XdeltaPatching.PatchXDelta(datapath, patches, programLocation + "\\cache\\patchedData");

Gaster.WriteLine("Loading the data files into memory...", "DECONSTRUCTING THE DELTA STORIES.");
List<UndertaleData> datas = [];
for (int i = 0; i <= totalPatches; i++)
{
    using FileStream fileStream = new($"cache\\patchedData\\{i}.win", FileMode.Open, FileAccess.Read);
    datas.Add(UndertaleIO.Read(fileStream));
    datas[i].ToolInfo.DecompilerSettings = new DecompileSettings() //decompile settings for more consistent diffs
    {
        PrintWarnings = false,
        RemoveSingleLineBlockBraces = true,

        EmptyLineAfterBlockLocals = false,
        EmptyLineAfterSwitchCases = false,
        EmptyLineAroundBranchStatements = false,
        EmptyLineAroundEnums = false,
        EmptyLineAroundFunctionDeclarations = false,
        EmptyLineAroundStaticInitialization = false,
    };
}

Gaster.WriteLine("Merging Sprites...", "MIGRATING IMAGES.");
SpriteMerger.Merge(datas);
Gaster.WriteLine("Merging Shaders...", "UNIFYING SHADERS.");
ShaderMerger.Merge(datas);
Gaster.WriteLine("Merging Objects...", "COMBINING DEVICES.");
GameObjectMerger.Merge(datas);
Gaster.WriteLine("Merging Code...", "FUSING CODE.");
CodeMerger.Merge(datas);

//for (int i = 1; i <= totalPatches; i++)
 //   datas[i].Dispose();

Gaster.WriteLine("Saving merged data.win...", "CREATING THE FILE OF FUSION.");
using (FileStream fileStream = new("cache\\patchedData\\data.win", FileMode.Create, FileAccess.Write))
{
    UndertaleIO.Write(fileStream, datas[0]);
}

using (FileStream fileStream = new(savepath, FileMode.Create, FileAccess.Write))
{
    UndertaleIO.Write(fileStream, datas[0]);
}

Gaster.WriteLine("Mod merging completed successfully.", "FUSION IS COMPLETE. FAREWELL.");

}
catch (Exception ex)
{
    Gaster.WriteLine($"An error occurred. The error information was saved in {Path.GetFullPath("cache\\crashLog.txt")}", $"ERROR OCCURED. IT MAY BE REVIEWED IN {Path.GetFullPath("cache\\crashLog.txt")}.");
    File.WriteAllText("cache\\crashLog.txt", ex.ToString());
}