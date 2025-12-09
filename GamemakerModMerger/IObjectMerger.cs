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
using static GamemakerModMerger.Program;

namespace GamemakerModMerger;
public interface IObjectMerger<T> where T : UndertaleObject, new()
{
    static abstract UndertalePointerList<T> Merge();


}

public class SpriteMerger : IObjectMerger<UndertaleSprite>
{
    public static UndertalePointerList<UndertaleSprite> Merge()
    {
        //UndertalePointerList<UndertaleSprite> origSprites = [.. Datas[0].Sprites]; // used to make sure the sprites are not overwritten by vanilla sprites from another mod's data file
        bool[] spriteChanged = new bool[Datas[0].Sprites.Count];
        foreach (UndertaleData data in Datas)
        {
            if (Datas.IndexOf(data) == 0) continue; // skip vanilla
            foreach (UndertaleSprite sprite in data.Sprites)
            {
                var origSprite = Datas[0].Sprites.ByName(sprite.Name.Content);
                if (origSprite == null)
                {
                    Datas[0].AddSprite(sprite);
                    spriteChanged = [.. spriteChanged, true];
                }
                else if (!spriteChanged[Datas[0].IndexOf(origSprite)] && !origSprite.Match(sprite)) 
                { 
                    Datas[0].ReplaceSprite(sprite);
                    spriteChanged[Datas[0].IndexOf(origSprite)] = true;
                }
                if (data.Sprites.IndexOf(sprite) % 100 == 0)
                    Gaster.WriteLine($"{data.Sprites.IndexOf(sprite)}/{data.Sprites.Count} Sprites of mod {Datas.IndexOf(data)} iterated.", 
                        $"{data.Sprites.IndexOf(sprite)}/{data.Sprites.Count} IMAGES HANDLED IN DELTA {Datas.IndexOf(data)}.");
            }
            Gaster.WriteLine($"Sprite merging complete for mod {Datas.IndexOf(data)}.", $"ALL IMAGES HANDLED IN DELTA {Datas.IndexOf(data)}.");
        }
        return Datas[0].Sprites as UndertalePointerList<UndertaleSprite>;
    }
}

public class CodeMerger : IObjectMerger<UndertaleCode>
{
    public static UndertalePointerList<UndertaleCode> Merge()
    {
        GlobalDecompileContext[] context = new GlobalDecompileContext[Datas.Count];

        List<string> changedCode = [];
        for (var i = 0; i < Datas.Count; i++)
        {
            var data = Datas[i];
            context[i] = new(data);
            if (Datas.IndexOf(data) == 0) continue; // skip vanilla
            foreach (UndertaleCode code in data.Code)
            {
                if (code.ParentEntry != null) continue;
                bool check;
                if (Datas[0].Code.ByName(code.Name.Content) == null)
                    check = true;
                else
                {
                    var newCode = new DecompileContext(context[i], code, data.ToolInfo.DecompilerSettings).DecompileToString();
                    var origCode = new DecompileContext(context[0], Datas[0].Code.ByName(code.Name.Content), Datas[0].ToolInfo.DecompilerSettings).DecompileToString();
                    check = newCode != origCode;
                }

                if (check && !changedCode.Contains(code.Name.Content))
                    changedCode.Add(code.Name.Content);


                if (Datas[0].Code.ByName(code.Name.Content) == null && code.Name.Content.Contains("_Collision_"))
                {
                    string codeEntryName = code.Name.Content;
                    int lastUnderscore = codeEntryName.LastIndexOf('_');
                    int secondLastUnderscore = codeEntryName.LastIndexOf('_', lastUnderscore - 1);

                    ReadOnlySpan<char> objectName = codeEntryName.AsSpan(new Range("gml_Object_".Length, secondLastUnderscore));

                    UndertaleCode newCode = new() { Name = Datas[0].Strings.MakeString(codeEntryName) };
                    Datas[0].Code.Add(newCode);

                    CodeImportGroup.LinkEvent(Datas[0].GameObjects.ByName(objectName), newCode, EventType.Collision, (uint)Datas[0].GameObjects.IndexOfName(data.GameObjects[int.Parse(codeEntryName.AsSpan(lastUnderscore + 1))].Name.Content));
                }

                if (data.Code.IndexOf(code) % 100 == 0)
                    Gaster.WriteLine($"{data.Code.IndexOf(code)}/{data.Code.Count} Code entries of mod {Datas.IndexOf(data)} iterated.",
                        $"{data.Code.IndexOf(code)}/{data.Code.Count} CODE ENTRIES ITERATED IN DELTA {Datas.IndexOf(data)}.");
            }
            Gaster.WriteLine($"All code entries iterated for mod {Datas.IndexOf(data)}.", $"ALL CODE ENTRIES ITERATED IN DELTA {Datas.IndexOf(data)}.");
        }

        Gaster.WriteLine("Beginning code merging...", "BEGINNING FUSION OF CODE TEXT.");

        foreach (string codeName in changedCode)
        {
            CodeImportGroup importGroup = new(Datas[0]);
            var origCodeObj = Datas[0].Code.ByName(codeName);
            string origCode = origCodeObj == null ? "\n" : new DecompileContext(context[0], origCodeObj, Datas[0].ToolInfo.DecompilerSettings).DecompileToString();
            string mergedCode = origCode;

            for (int data = 1; data < Datas.Count; data++)
            {
                var thisCodeObj = Datas[data].Code.ByName(codeName);
                string thisCode = thisCodeObj == null ? "\n" : new DecompileContext(context[data], thisCodeObj, Datas[data].ToolInfo.DecompilerSettings).DecompileToString();
                var diff = ThreeWayDiffer.Instance.CreateDiffs(origCode, mergedCode, thisCode, true, false, LineEndingsPreservingChunker.Instance);

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
                            Gaster.WriteLine($"There was a conflict while fusing \"{codeName}\". This may lead to unexpected behavior.", 
                                $"CONFLICT ENCOUNTERED DURING FUSION OF \"{codeName}\". FUSION MAY NOT WORK CORRECTLY.");
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
            try
            {
                importGroup.Import();
            }
            catch (Exception e)
            {
                throw new Exception($"An error occurred while importing merged code for \"{codeName}\": {e.Message}", e);
                //Gaster.WriteLine($"An error occurred while importing merged code for \"{codeName}\": {e.Message}", 
                //    $"ERROR DURING IMPORT OF FUSED CODE FOR \"{codeName}\": {e.Message}");
            }
        }

        return Datas[0].Code as UndertalePointerList<UndertaleCode>;
    }
}
public class GameObjectMerger : IObjectMerger<UndertaleGameObject>
{
    public static UndertalePointerList<UndertaleGameObject> Merge()
    {
        foreach (UndertaleData data in Datas)
        {
            if (Datas.IndexOf(data) == 0) continue; // skip vanilla
            foreach (UndertaleGameObject gameObject in data.GameObjects)
            {
                var origObject = Datas[0].GameObjects.ByName(gameObject.Name.Content);
                if (origObject == null)
                {
                    origObject = new UndertaleGameObject();
                    Datas[0].GameObjects.Add(origObject);
                    origObject.Name = Datas[0].Strings.MakeString(gameObject.Name.Content);
                }
                if (gameObject.Sprite is not null)
                    origObject.Sprite = Datas[0].Sprites.ByName(gameObject.Sprite.Name.Content);
                origObject.Visible = gameObject.Visible;
                origObject.Managed = gameObject.Managed;
                origObject.Solid = gameObject.Solid;
                origObject.Depth = gameObject.Depth;
                origObject.Persistent = gameObject.Persistent;
                if (gameObject.TextureMaskId is not null)
                    origObject.TextureMaskId = Datas[0].Sprites.ByName(gameObject.TextureMaskId.Name.Content);

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
                    Gaster.WriteLine($"{data.GameObjects.IndexOf(gameObject)}/{data.GameObjects.Count} Objects of mod {Datas.IndexOf(data)} iterated.",
                        $"{data.GameObjects.IndexOf(gameObject)}/{data.GameObjects.Count} DEVICES HANDLED IN DELTA {Datas.IndexOf(data)}.");
            }
            Gaster.WriteLine($"Object merging complete for mod {Datas.IndexOf(data)}.", $"ALL DEVICES HANDLED IN DELTA {Datas.IndexOf(data)}.");
        }
        foreach (UndertaleData data in Datas)
        {
            if (Datas.IndexOf(data) == 0) continue; // skip vanilla
            foreach (UndertaleGameObject gameObject in data.GameObjects)
            {
                var origObject = Datas[0].GameObjects.ByName(gameObject.Name.Content);
                if (gameObject.ParentId is not null)
                    origObject.ParentId = Datas[0].GameObjects.ByName(gameObject.ParentId.Name.Content);
            }
        }
        return Datas[0].GameObjects as UndertalePointerList<UndertaleGameObject>;
    }


}

public class ShaderMerger : IObjectMerger<UndertaleShader>
{
    public static UndertalePointerList<UndertaleShader> Merge()
    {
        foreach (UndertaleData data in Datas)
        {
            if (Datas.IndexOf(data) == 0) continue; // skip vanilla
            foreach (var donorShader in data.Shaders)
            {
                var targetShader = Datas[0].Shaders.ByName(donorShader.Name.Content);
                if (targetShader == null)
                {
                    targetShader = new UndertaleShader();
                    Datas[0].Shaders.Add(targetShader);
                    targetShader.Name = Datas[0].Strings.MakeString(donorShader.Name.Content);
                }
                targetShader.Type = donorShader.Type;

                targetShader.GLSL_ES_Vertex = Datas[0].Strings.MakeString(donorShader.GLSL_ES_Vertex.Content);
                targetShader.GLSL_ES_Fragment = Datas[0].Strings.MakeString(donorShader.GLSL_ES_Fragment.Content);
                targetShader.GLSL_Vertex = Datas[0].Strings.MakeString(donorShader.GLSL_Vertex.Content);
                targetShader.GLSL_Fragment = Datas[0].Strings.MakeString(donorShader.GLSL_Fragment.Content);
                targetShader.HLSL9_Vertex = Datas[0].Strings.MakeString(donorShader.HLSL9_Vertex.Content);
                targetShader.HLSL9_Fragment = Datas[0].Strings.MakeString(donorShader.HLSL9_Fragment.Content);

                targetShader.Version = donorShader.Version;

                targetShader.HLSL11_VertexData = donorShader.HLSL11_VertexData;
                targetShader.HLSL11_PixelData = donorShader.HLSL11_PixelData;
                /* no play station
                origShader.PSSL_VertexData = shader.PSSL_VertexData;
                origShader.PSSL_PixelData = shader.PSSL_PixelData;
                origShader.Cg_PSVita_VertexData = shader.Cg_PSVita_VertexData;
                origShader.Cg_PSVita_PixelData = shader.Cg_PSVita_PixelData;
                origShader.Cg_PS3_VertexData = shader.Cg_PS3_VertexData;
                origShader.Cg_PS3_PixelData = shader.Cg_PS3_PixelData;
                */
                foreach (var attribute in donorShader.VertexShaderAttributes)
                {
                    targetShader.VertexShaderAttributes.Add(new UndertaleShader.VertexShaderAttribute() { Name = Datas[0].Strings.MakeString(donorShader.Name.Content)});
                }

                //VertexShaderAttribute and yeah

                if (data.Shaders.IndexOf(donorShader) % 100 == 0)
                    Gaster.WriteLine($"{data.Shaders.IndexOf(donorShader)}/{data.Shaders.Count} Shaders of mod {Datas.IndexOf(data)} iterated.", 
                        $"{data.Shaders.IndexOf(donorShader)}/{data.Shaders.Count} SHADERS HANDLED IN DELTA {Datas.IndexOf(data)}.");
            }
            Gaster.WriteLine($"Shader merging complete for mod {Datas.IndexOf(data)}.", $"ALL SHADERS HANDLED IN DELTA {Datas.IndexOf(data)}.");
        }
        return Datas[0].Shaders as UndertalePointerList<UndertaleShader>;
    }


}

public class UndertaleGeneralInfoMerger
{
    public void Merge()
    {
        foreach (var data in Datas)
        {
            if (Datas.IndexOf(data) == 0) return;

            var origInfo = Datas[0].GeneralInfo;
            var info = data.GeneralInfo;

            if (origInfo.Info != info.Info)
            {

            }
                
        }
    }
}