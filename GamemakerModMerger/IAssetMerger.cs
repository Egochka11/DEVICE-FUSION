using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;
using static GamemakerModMerger.Program;

namespace GamemakerModMerger;
public interface IAssetMerger<T> where T : UndertaleObject
{
    T MergeAssets(T original, T mod1, T? mod2);
}

public class ThatGameObjectMerger : IAssetMerger<UndertaleGameObject>
{
    public UndertaleGameObject MergeAssets(UndertaleGameObject original, UndertaleGameObject mod1, UndertaleGameObject? mod2)
    {
        if (mod2 is null) return mod1;
        UndertaleGameObject newObject = new();
        if (original.Sprite != null && mod1.Sprite != null && mod2.Sprite != null)
            newObject.Sprite = mod1.Sprite.Name != original.Sprite.Name ? Datas[0].Sprites.ByName(mod1.Sprite.Name.Content) : Datas[0].Sprites.ByName(mod2.Sprite.Name.Content);

        newObject.Visible = mod1.Visible != original.Visible ? mod1.Visible : mod2.Visible;
        newObject.Managed = mod1.Managed != original.Managed ? mod1.Managed : mod2.Managed;
        newObject.Solid = mod1.Solid != original.Solid ? mod1.Solid : mod2.Solid;
        newObject.Depth = mod1.Depth != original.Depth ? mod1.Depth : mod2.Depth;
        newObject.Persistent = mod1.Persistent != original.Persistent ? mod1.Persistent : mod2.Persistent;

        if (original.TextureMaskId != null && mod1.TextureMaskId != null && mod2.TextureMaskId != null)
            newObject.TextureMaskId = mod1.TextureMaskId.Name != original.TextureMaskId.Name ? Datas[0].Sprites.ByName(mod1.TextureMaskId.Name.Content) : Datas[0].Sprites.ByName(mod2.TextureMaskId.Name.Content);

        // Physics.
        newObject.UsesPhysics = mod1.UsesPhysics != original.UsesPhysics ? mod1.UsesPhysics : mod2.UsesPhysics;
        newObject.IsSensor = mod1.IsSensor != original.IsSensor ? mod1.IsSensor : mod2.IsSensor;
        newObject.CollisionShape = mod1.CollisionShape != original.CollisionShape ? mod1.CollisionShape : mod2.CollisionShape;
        newObject.Density = mod1.Density != original.Density ? mod1.Density : mod2.Density;
        newObject.Restitution = mod1.Restitution != original.Restitution ? mod1.Restitution : mod2.Restitution;
        newObject.Group = mod1.Group != original.Group ? mod1.Group : mod2.Group;
        newObject.LinearDamping = mod1.LinearDamping != original.LinearDamping ? mod1.LinearDamping : mod2.LinearDamping;
        newObject.AngularDamping = mod1.AngularDamping != original.AngularDamping ? mod1.AngularDamping : mod2.AngularDamping;
        newObject.Friction = mod1.Friction != original.Friction ? mod1.Friction : mod2.Friction;
        newObject.Awake = mod1.Awake != original.Awake ? mod1.Awake : mod2.Awake;
        newObject.Kinematic = mod1.Kinematic != original.Kinematic ? mod1.Kinematic : mod2.Kinematic;
        newObject.PhysicsVertices = mod1.PhysicsVertices != original.PhysicsVertices ? mod1.PhysicsVertices : mod2.PhysicsVertices;

        throw new NotImplementedException();
    }
}
