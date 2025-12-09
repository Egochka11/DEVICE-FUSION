// See https://aka.ms/new-console-template for more information

using GamemakerModMerger;
using System.Reflection;
using System.Text;
using Underanalyzer.Decompiler;
using UndertaleModLib;

namespace GamemakerModMerger;

internal class Program
{
    static string programLocation = "";

    public static List<UndertaleData> Datas = [];
    public static FileStream FileStream;
    static async Task Main(string[] args)
    {
        programLocation = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
        if (args == null || args.Length == 0)
            await RunProgram();
        else
            await RunCli(args);
    }

    static async Task RunCli(string[] args)
    {
        //vanilla file, mods, output
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: [vanilla file] [mods] [output file]");
            Console.WriteLine("Mod paths must be separated by ::");
            return;
        }
        string datapath = args[0];
        string savepath = args[2];
        List<string> patches = args[1].Split("::").ToList();
        await DoMerging(datapath, savepath, patches);
    }

    static async Task RunProgram()
    {
        using (FileStream = new FileStream(programLocation + "\\cache\\crashLog.txt", FileMode.Create, FileAccess.Write)) 
        {
            try
            {
                Gaster.WriteLine("Before beginning, would you like to enable Gaster mode? (y/n) WARNING: Gaster mode is kind of cryptic so it is recommended to use only if you're familiar with the program");
                Gaster.GasterAlt = Console.ReadLine() == "y";
                if (Gaster.GasterAlt) Console.ForegroundColor = ConsoleColor.Green;
                //Console.WriteLine(programLocation);

                Gaster.WriteLine("The original data.win file:", "THE STORY IN PUREST FORM.");
                var datapath = Console.ReadLine();
                Gaster.WriteLine("Where the file should be placed INCLUDING THE FILE NAME AND EXTENSION:", "THE DESTINATION OF FUSION.");
                var savepath = Console.ReadLine();
                Gaster.WriteLine("Mod amount:", "THE AMOUNT OF ALTERATIONS.");
                var totalPatches = uint.Parse(Console.ReadLine());

                var patches = new List<string>();
                for (uint i = 0; i < totalPatches; i++)
                {
                    Gaster.WriteLine($"Mod {i + 1}:", $"DELTA {i + 1}.");
                    patches.Add(Console.ReadLine());
                }
                await DoMerging(datapath, savepath, patches);
            }
            catch (Exception ex)
            {
                Gaster.WriteLine($"An error occurred. The error information was saved in {programLocation + "\\cache\\crashLog.txt"}", $"ERROR OCCURED. IT MAY BE REVIEWED IN {programLocation + "\\cache\\crashLog.txt"}.");
                Gaster.WriteLine(ex.ToString());
            }
        }
    }

    public static async Task DoMerging(string datapath, string savepath, List<string> patches)
    {
        datapath = datapath.Trim('/', '"');
        savepath = savepath.Trim('/', '"');
        for (int i = 0; i < patches.Count; i++)
            patches[i] = patches[i].Trim('/', '"');

        var totalPatches = patches.Count;

        Directory.CreateDirectory(programLocation + "\\cache\\patchedData");

        File.Copy(datapath, programLocation + "\\cache\\patchedData\\0.win", true);
        Gaster.WriteLine("Patching mods into separate files...", "APPLYING THE DELTAS SEPARATELY.");
        await ModPatching.Patch(datapath, patches, programLocation + "\\cache\\patchedData");

        Gaster.WriteLine("Loading the data files into memory...", "DECONSTRUCTING THE DELTA STORIES.");
        for (int i = 0; i <= totalPatches; i++)
        {
            using FileStream fileStream = new($"{programLocation}\\cache\\patchedData\\{i}.win", FileMode.Open, FileAccess.Read);
            Datas.Add(UndertaleIO.Read(fileStream));
            Datas[i].ToolInfo.DecompilerSettings = new DecompileSettings() //decompile settings for more consistent diffs
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
        SpriteMerger.Merge();
        Gaster.WriteLine("Merging Shaders...", "UNIFYING SHADERS.");
        ShaderMerger.Merge();
        Gaster.WriteLine("Merging Objects...", "COMBINING DEVICES.");
        GameObjectMerger.Merge();
        Gaster.WriteLine("Merging Code...", "FUSING CODE.");
        CodeMerger.Merge();

        Gaster.WriteLine("Saving merged data.win...", "CREATING THE FILE OF FUSION.");
        using (FileStream fileStream = new(programLocation + "\\cache\\patchedData\\data.win", FileMode.Create, FileAccess.Write))
        {
            UndertaleIO.Write(fileStream, Datas[0]);
        }

        using (FileStream fileStream = new(savepath, FileMode.Create, FileAccess.Write))
        {
            UndertaleIO.Write(fileStream, Datas[0]);
        }

        Gaster.WriteLine("Mod merging completed successfully.", "FUSION IS COMPLETE. FAREWELL.");
    }
}