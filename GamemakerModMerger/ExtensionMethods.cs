using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace GamemakerModMerger
{
    public static class ExtensionMethods
    {
        #region AddObjects
        public static void AddSprite(this UndertaleData data, UndertaleSprite sprite)
        {
            UndertaleSprite newsprite = new() { Name = data.Strings.MakeString(sprite.Name.Content) };
            data.Sprites.Add(newsprite);
            data.ReplaceSprite(sprite);
        }

        public static void ReplaceSprite(this UndertaleData data, UndertaleSprite sprite)
        {
            foreach (UndertaleSprite.TextureEntry texture in sprite.Textures)
            {
                data.AddTexturePageItem(texture.Texture);
            }
            var origSprite = data.Sprites.ByName(sprite.Name.Content);
            origSprite.Textures = sprite.Textures;
            origSprite.OriginXWrapper = sprite.OriginXWrapper;
            origSprite.OriginYWrapper = sprite.OriginYWrapper;
            origSprite.Width = sprite.Width;
            origSprite.Height = sprite.Height;
            origSprite.MarginLeft = sprite.MarginLeft;
            origSprite.MarginRight = sprite.MarginRight;
            origSprite.MarginTop = sprite.MarginTop;
            origSprite.MarginBottom = sprite.MarginBottom;
            origSprite.Transparent = sprite.Transparent;
            origSprite.Smooth = sprite.Smooth;
            origSprite.Preload = sprite.Preload;
            origSprite.BBoxMode = sprite.BBoxMode;
            origSprite.SepMasks = sprite.SepMasks;
            origSprite.CollisionMasks = sprite.CollisionMasks;
            origSprite.SVersion = sprite.SVersion;
            origSprite.SSpriteType = sprite.SSpriteType;
            origSprite.GMS2PlaybackSpeed = sprite.GMS2PlaybackSpeed;
            origSprite.GMS2PlaybackSpeedType = sprite.GMS2PlaybackSpeedType;
            origSprite.IsSpecialType = sprite.IsSpecialType;
            //TODO: add support for all the other sprtie metadata
        }

        public static void AddTexturePageItem(this UndertaleData data, UndertaleTexturePageItem pageItem)
        {
            data.TexturePageItems.Add(pageItem);
            if (!data.EmbeddedTextures.Contains(pageItem.TexturePage))
            {
                data.AddEmbeddedTexture(pageItem.TexturePage);
            }
        }

        public static void AddEmbeddedTexture(this UndertaleData data, UndertaleEmbeddedTexture texture)
        {
            data.EmbeddedTextures.Add(texture);
        }

        public static void AddString(this UndertaleData data, UndertaleString @string)
        {
            data.Strings.Add(@string);
        }
        #endregion

        #region Equals

        private static readonly TextureWorker Worker = new();
        public static bool Match(this UndertaleSprite spriteA, UndertaleSprite spriteB)
        {
            //handle null cases
            ArgumentNullException.ThrowIfNull(spriteA);
            ArgumentNullException.ThrowIfNull(spriteB);

            if (spriteA.Textures.Count != spriteB.Textures.Count) return false;
            for (int i = 0; i < spriteA.Textures.Count; i++)
            {
                if (spriteA.Textures[i].Texture == null && spriteB.Textures[i].Texture == null) continue;
                if (spriteA.Textures[i].Texture == null || spriteB.Textures[i].Texture == null) return false;

                var imgA = Worker.GetTextureFor(spriteA.Textures[i].Texture, $"Texture {i} of spriteA ({spriteA.Name.Content}) in ExtensionMethods.Equals(this UndertaleSprite, UndertaleSprite)", true) as MagickImage;
                var imgB = Worker.GetTextureFor(spriteB.Textures[i].Texture, $"Texture {i} of spriteB ({spriteB.Name.Content}) in ExtensionMethods.Equals(this UndertaleSprite, UndertaleSprite)", true) as MagickImage;
                imgB.Format = imgA.Format;
                if (imgA.Compare(imgB, ErrorMetric.Absolute) != 0)
                {
                    return false;
                }
            } // Just check for the textures, the rest probably doesnt matter ¯\_(ツ)_/¯

            return true;
        }
        #endregion
    }
}
