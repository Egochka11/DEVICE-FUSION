using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemakerModMerger;
internal static class XdeltaPatching
{
    public static async Task PatchXDelta(string data, string patch, string output)
    {
        using (var process = new Process())
        {
            if (OperatingSystem.IsWindows())
            {
                //process.StartInfo.FileName = "xdelta3-3.1.0-x86_64.exe";
                process.StartInfo.FileName = "xdelta3-3.1.0-i686.exe";
                process.StartInfo.Arguments = $"-d -f -s \"{data}\" \"{patch}\" \"{output}\"";
            }
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            await process.WaitForExitAsync();
        }
    }

    public static async Task PatchXDelta(string data, List<string> patches, string output)
    {
        var patchTasks = new List<Task>();
        for (int i = 0; i < patches.Count; i++)
        {
            patchTasks.Add(PatchXDelta(data, patches[i], output + $"\\{i + 1}.win"));
        }
        await Task.WhenAll(patchTasks);
    }
}
