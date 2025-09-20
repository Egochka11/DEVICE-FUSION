using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;
using System.Threading.Tasks;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

namespace GamemakerModMerger;
public interface IObjectMerger<T> where T : UndertaleObject, new()
{
    static abstract UndertalePointerList<T> Merge(List<UndertaleData> datas);


}

public class SpriteMerger : IObjectMerger<UndertaleSprite>
{
    public static UndertalePointerList<UndertaleSprite> Merge(List<UndertaleData> datas)
    {
        //UndertalePointerList<UndertaleSprite> origSprites = [.. datas[0].Sprites]; // used to make sure the sprites are not overwritten by vanilla sprites from another mod's data file
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
        GlobalDecompileContext[] context = new GlobalDecompileContext[datas.Count];

        List<string> changedCode = [];
        for (var i = 0; i < datas.Count; i++)
        {
            var data = datas[i];
            context[i] = new(data);
            if (datas.IndexOf(data) == 0) continue; // skip vanilla
            foreach (UndertaleCode code in data.Code)
            {
                if (code.ParentEntry != null) continue;
                bool check;
                if (datas[0].Code.ByName(code.Name.Content) == null)
                    check = true;
                else
                {
                    var newCode = new DecompileContext(context[i], code, data.ToolInfo.DecompilerSettings).DecompileToString();
                    var origCode = new DecompileContext(context[0], datas[0].Code.ByName(code.Name.Content), datas[0].ToolInfo.DecompilerSettings).DecompileToString();
                    check = newCode != origCode;
                }

                if (check && !changedCode.Contains(code.Name.Content))
                    changedCode.Add(code.Name.Content);
                //importGroup.QueueReplace(code.Name.Content, new DecompileContext(context, code, data.ToolInfo.DecompilerSettings).DecompileToString());

                if (data.Code.IndexOf(code) % 100 == 0)
                    Console.WriteLine($"{data.Code.IndexOf(code)}/{data.Code.Count} CODE ENTRIES ITERATED IN DELTA {datas.IndexOf(data)}.");
            }
            Console.WriteLine($"ALL CODE ENTRIES ITERATED IN DELTA {datas.IndexOf(data)}.");
        }

        Console.WriteLine($"BEGINNING FUSION OF CODE TEXT.");

        CodeImportGroup importGroup = new(datas[0]);
        foreach (string codeName in changedCode)
        {
            if (codeName == "scr_green_draw" || codeName == "gml_GlobalScript_scr_green_draw")
                Console.WriteLine("green deawn");
            var origCodeObj = datas[0].Code.ByName(codeName);
            string origCode = origCodeObj == null ? "\n" : new DecompileContext(context[0], origCodeObj, datas[0].ToolInfo.DecompilerSettings).DecompileToString();
            string mergedCode = origCode;

            for (int data = 1; data < datas.Count; data++)
            {
                var thisCodeObj = datas[data].Code.ByName(codeName);
                string thisCode = thisCodeObj == null ? "\n" : new DecompileContext(context[data], thisCodeObj, datas[data].ToolInfo.DecompilerSettings).DecompileToString();
                var diff = ThreeWayDiffer.Instance.CreateDiffs(origCode, mergedCode, thisCode, true, false, LineChunkerThatPreservesNewlines.Instance);

                mergedCode = "";
                int baseIndex = 0;
                int oldIndex = 0;
                int newIndex = 0;
                //Shoutout to thej01 and their moonwarmer project
                foreach (var block in diff.DiffBlocks)
                {
                    while (baseIndex < block.BaseStart)
                    {
                        mergedCode = string.Concat(mergedCode, diff.PiecesBase[baseIndex]);
                        baseIndex++;
                        oldIndex++;
                        newIndex++;
                    }

                    switch (block.ChangeType)
                    {
                        case ThreeWayChangeType.Unchanged:
                            for (var i = 0; i < block.BaseCount; i++)
                                mergedCode = string.Concat(mergedCode, diff.PiecesBase[baseIndex + i]);
                            break;
                        case ThreeWayChangeType.OldOnly:
                            for (var i = 0; i < block.OldCount; i++)
                                mergedCode = string.Concat(mergedCode, diff.PiecesOld[oldIndex + i]);
                            break;
                        case ThreeWayChangeType.NewOnly:
                            for (var i = 0; i < block.NewCount; i++)
                                mergedCode = string.Concat(mergedCode, diff.PiecesNew[newIndex + i]);
                            break;
                        case ThreeWayChangeType.BothSame:
                            for (var i = 0; i < block.OldCount; i++)
                                mergedCode = string.Concat(mergedCode, diff.PiecesOld[oldIndex + i]);
                            break;
                        case ThreeWayChangeType.Conflict:
                            Console.WriteLine($"CONFLICT ENCOUNTERED DURING FUSION OF \"{codeName}\". FUSION MAY NOT WORK CORRECTLY.");
                            for (var i = 0; i < block.OldCount; i++) // do both i guess
                                mergedCode = string.Concat(mergedCode, diff.PiecesOld[oldIndex + i]);
                            for (var i = 0; i < block.NewCount; i++)
                                mergedCode = string.Concat(mergedCode, diff.PiecesNew[newIndex + i]);
                            break;
                    }

                    baseIndex += block.BaseCount;
                    newIndex += block.NewCount;
                    oldIndex += block.OldCount;
                }

                while (baseIndex < diff.PiecesBase.Count)
                {
                    mergedCode = string.Concat(mergedCode, diff.PiecesBase[baseIndex]);
                    baseIndex++;
                    oldIndex++;
                    newIndex++;
                }
            }

            importGroup.QueueReplace(codeName, mergedCode);
        }


        importGroup.Import();
        return datas[0].Code as UndertalePointerList<UndertaleCode>;
    }
    public class LineChunkerThatPreservesNewlines : DelimiterChunker
    {
        private readonly static char[] lineSeparators = ['\r', '\n'];

        public LineChunkerThatPreservesNewlines() : base(lineSeparators) { }

        public static LineChunkerThatPreservesNewlines Instance { get; } = new LineChunkerThatPreservesNewlines();
    }
}
public class GameObjectMerger : IObjectMerger<UndertaleGameObject>
{
    public static UndertalePointerList<UndertaleGameObject> Merge(List<UndertaleData> datas)
    {
        foreach (UndertaleData data in datas)
        {
            if (datas.IndexOf(data) == 0) continue; // skip vanilla
            foreach (UndertaleGameObject gameObject in data.GameObjects)
            {
                var origObject = datas[0].GameObjects.ByName(gameObject.Name.Content);
                if (origObject == null)
                {
                    origObject = new UndertaleGameObject();
                    datas[0].GameObjects.Add(origObject);
                    origObject.Name = datas[0].Strings.MakeString(gameObject.Name.Content);
                }
                if (gameObject.Sprite is not null)
                    origObject.Sprite = datas[0].Sprites.ByName(gameObject.Sprite.Name.Content);
                origObject.Visible = gameObject.Visible;
                origObject.Managed = gameObject.Managed;
                origObject.Solid = gameObject.Solid;
                origObject.Depth = gameObject.Depth;
                origObject.Persistent = gameObject.Persistent;
                //origObject.ParentId = gameObject.ParentId; //TODO: implement good parenting
                if (gameObject.TextureMaskId is not null)
                    origObject.TextureMaskId = datas[0].Sprites.ByName(gameObject.TextureMaskId.Name.Content);

                // Physics.
                origObject.UsesPhysics = gameObject.UsesPhysics;
                origObject.IsSensor = gameObject.IsSensor;
                origObject.CollisionShape = gameObject.CollisionShape;
                origObject.Density = gameObject.Density;
                origObject.Restitution = gameObject.Restitution;
                origObject.Group = gameObject.Group;
                origObject.LinearDamping = gameObject.LinearDamping;
                origObject.AngularDamping = gameObject.AngularDamping;
                origObject.Friction = gameObject.Friction;
                origObject.Awake = gameObject.Awake;
                origObject.Kinematic = gameObject.Kinematic;
                origObject.PhysicsVertices = gameObject.PhysicsVertices;
                //Events should be handled by the code merger

                if (data.GameObjects.IndexOf(gameObject) % 100 == 0)
                    Console.WriteLine($"{data.GameObjects.IndexOf(gameObject)}/{data.GameObjects.Count} DEVICES HANDLED IN DELTA {datas.IndexOf(data)}.");
            }
            Console.WriteLine($"ALL DEVICES HANDLED IN DELTA {datas.IndexOf(data)}.");
        }
        foreach (UndertaleData data in datas)
        {
            if (datas.IndexOf(data) == 0) continue; // skip vanilla
            foreach (UndertaleGameObject gameObject in data.GameObjects)
            {
                var origObject = datas[0].GameObjects.ByName(gameObject.Name.Content);
                if (gameObject.ParentId is not null)
                    origObject.ParentId = datas[0].GameObjects.ByName(gameObject.ParentId.Name.Content);
            }
        }
        return datas[0].GameObjects as UndertalePointerList<UndertaleGameObject>;
    }


}

