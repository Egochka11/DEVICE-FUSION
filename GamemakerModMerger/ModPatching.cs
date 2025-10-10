using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using static UndertaleModLib.Models.UndertaleRoom;

namespace GamemakerModMerger;
internal static class ModPatching
{
    public static async Task PatchXDelta(string data, string patch, string output)
    {
        using (var process = new Process())
        {
            //process.StartInfo.FileName = "xdelta3-3.1.0-x86_64.exe";
            process.StartInfo.FileName = "xdelta3-3.1.0-i686.exe";
            process.StartInfo.Arguments = $"-d -f -s \"{data}\" \"{patch}\" \"{output}\"";
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            await process.WaitForExitAsync();
        }
    }
    /*public static async Task PatchCsx(string data, string patch, string output)
    {
        //copied from https://github.com/UnderminersTeam/UndertaleModTool/blob/master/UndertaleModTests/GameScriptTests.cs#L202
        await CSharpScript.EvaluateAsync(File.ReadAllText(patch), ScriptOptions.Default
            .WithImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler", "UndertaleModLib.Scripting", "System", "System.IO", "System.Collections.Generic")
            .WithReferences(typeof(UndertaleObject).GetTypeInfo().Assembly)
            .WithReferences(typeof(Underanalyzer.Decompiler.DecompileContext).GetTypeInfo().Assembly),
            typeof(IScriptInterface));
    }*/

    public static async Task Patch(string data, List<string> patches, string output)
    {
        var patchTasks = new List<Task>();
        for (int i = 0; i < patches.Count; i++)
        {
            if (patches[i].EndsWith(".xdelta"))
                patchTasks.Add(PatchXDelta(data, patches[i], output + $"\\{i + 1}.win"));
            //else if (patches[i].EndsWith(".csx"))
            //    patchTasks.Add(PatchCsx(data, patches[i], output + $"\\{i + 1}.win"));
            else if (patches[i].EndsWith(".win"))
                patchTasks.Add(new Task(() => { File.Copy(patches[i], output + $"\\{i + 1}.win"); }));
            else throw new Exception($"File of mod {i} is not a valid file type. Supported file types: .xdelta, .win");
        }
        await Task.WhenAll(patchTasks);
    }
}
