using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace GamemakerModMerger;
public interface IObjectMerger<T> where T : UndertaleObject, new()
{
    static abstract UndertalePointerList<T> Merge(List<UndertaleData> datas);


}

public class SpriteMerger : IObjectMerger<UndertaleSprite>
{
    public static UndertalePointerList<UndertaleSprite> Merge(List<UndertaleData> datas)
    {
        UndertalePointerList<UndertaleSprite> origSprites = [.. datas[0].Sprites]; // used to make sure the sprites are not overwritten by vanilla sprites from another mod's data file
        bool[] spriteChanged = new bool[datas[0].Sprites.Count];
        foreach (UndertaleData data in datas)
        {
            if (datas.IndexOf(data) == 0) continue; // skip vanilla
            foreach (UndertaleSprite sprite in data.Sprites)
            {
                var origSprite = datas[0].Sprites.ByName(sprite.Name.Content);
                if (origSprite == null)
                {
                    datas[0].AddSprite(sprite);
                    spriteChanged = [.. spriteChanged, true];
                }
                else if (!spriteChanged[datas[0].IndexOf(origSprite)] && !origSprite.Match(sprite)) 
                { 
                    datas[0].ReplaceSprite(sprite);
                    spriteChanged[datas[0].IndexOf(origSprite)] = true;
                }
                if (data.Sprites.IndexOf(sprite) % 100 == 0)
                    Console.WriteLine($"{data.Sprites.IndexOf(sprite)}/{data.Sprites.Count} IMAGES HANDLED IN DELTA {datas.IndexOf(data)}.");
            }
            Console.WriteLine($"ALL IMAGES HANDLED IN DELTA {datas.IndexOf(data)}.");
        }
        return datas[0].Sprites as UndertalePointerList<UndertaleSprite>;
    }
}

public class CodeMerger : IObjectMerger<UndertaleCode>
{
    public static UndertalePointerList<UndertaleCode> Merge(List<UndertaleData> datas)
    {
        CodeImportGroup importGroup = new(datas[0]);
        GlobalDecompileContext vanillaContext = new(datas[0]);
        foreach (UndertaleData data in datas)
        {
            if (datas.IndexOf(data) == 0) continue; // skip vanilla
            GlobalDecompileContext context = new(data);
            foreach (UndertaleCode code in data.Code)
            {
                if (code.ParentEntry != null) continue;
                bool check = false;
                if (datas[0].Code.ByName(code.Name.Content) == null)
                    check = true;
                else
                {
                    var newCode = new DecompileContext(context, code, data.ToolInfo.DecompilerSettings).DecompileToString();
                    var origCode = new DecompileContext(vanillaContext, datas[0].Code.ByName(code.Name.Content), datas[0].ToolInfo.DecompilerSettings).DecompileToString();
                    check = newCode != origCode;
                }

                if (check)
                    importGroup.QueueReplace(code.Name.Content, new DecompileContext(context, code, data.ToolInfo.DecompilerSettings).DecompileToString());

                if (data.Code.IndexOf(code) % 100 == 0)
                    Console.WriteLine($"{data.Code.IndexOf(code)}/{data.Code.Count} CODE ENTRIES HANDLED IN DELTA {datas.IndexOf(data)}.");
            }
            Console.WriteLine($"ALL CODE ENTRIES HANDLED IN DELTA {datas.IndexOf(data)}.");
        }
        importGroup.Import();
        return datas[0].Code as UndertalePointerList<UndertaleCode>;
    }
}
public class GameObjectMerger : IObjectMerger<UndertaleGameObject>
{
    public static UndertalePointerList<UndertaleGameObject> Merge(List<UndertaleData> datas)
    {
        CodeImportGroup importGroup = new(datas[0]);
        GlobalDecompileContext vanillaContext = new(datas[0]);
        foreach (UndertaleData data in datas)
        {
            if (datas.IndexOf(data) == 0) continue; // skip vanilla
            GlobalDecompileContext context = new(data);
            foreach (UndertaleCode code in data.Code)
            {
                if (code.ParentEntry != null) continue;
                bool check = false;
                if (datas[0].Code.ByName(code.Name.Content) == null)
                    check = true;
                else
                {
                    var newCode = new DecompileContext(context, code, data.ToolInfo.DecompilerSettings).DecompileToString();
                    var origCode = new DecompileContext(vanillaContext, datas[0].Code.ByName(code.Name.Content), datas[0].ToolInfo.DecompilerSettings).DecompileToString();
                    check = newCode != origCode;
                }

                if (check)
                    importGroup.QueueReplace(code.Name.Content, new DecompileContext(context, code, data.ToolInfo.DecompilerSettings).DecompileToString());

                if (data.Code.IndexOf(code) % 100 == 0)
                    Console.WriteLine($"{data.Code.IndexOf(code)}/{data.Code.Count} CODE ENTRIES HANDLED IN DELTA {datas.IndexOf(data)}.");
            }
            Console.WriteLine($"ALL CODE ENTRIES HANDLED IN DELTA {datas.IndexOf(data)}.");
        }
        importGroup.Import();
        return datas[0].GameObjects as UndertalePointerList<UndertaleGameObject>;
    }
}

