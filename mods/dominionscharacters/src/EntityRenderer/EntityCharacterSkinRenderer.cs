using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;

namespace dominions.characters
{
    public class EntityCharacterSkinRenderer : EntityShapeRenderer
    {
        protected int skinTextureSubId;

        public override TextureAtlasPosition this[string textureCode]
        {
            get
            {
                CompositeTexture cpt = null;
                if (extraTexturesByTextureName != null && extraTexturesByTextureName.TryGetValue(textureCode, out cpt) == true)
                {
                    return capi.EntityTextureAtlas.Positions[cpt.Baked.TextureSubId];
                }

                return skinTexPos;
            }
        }

        public EntityCharacterSkinRenderer(Entity entity, ICoreClientAPI api) : base(entity, api)
        {
            api.Event.ReloadTextures += reloadSkin;

            this.setShape();

            // TEXTURE LISTENER
            string[] skinParts = Core.skinTypes.Keys.ToArray();
            foreach (string part in skinParts)
            {
                entity.WatchedAttributes.RegisterModifiedListener(part, () =>
                {
                    this.reloadSkin();
                });
            }
            entity.WatchedAttributes.RegisterModifiedListener("sex", () =>
                {
                    this.reloadSkin();
                });

            // SHAPE LISTENER
            entity.WatchedAttributes.RegisterModifiedListener("race", () =>
                {
                    this.setShape();
                    this.TesselateShape();
                });

        }

        bool textureSpaceAllocated = false;
        protected override ITexPositionSource GetTextureSource()
        {
            if (!textureSpaceAllocated)
            {
                TextureAtlasPosition origTexPos = capi.EntityTextureAtlas.Positions[entity.Properties.Client.FirstTexture.Baked.TextureSubId];
                int width = (int)((origTexPos.x2 - origTexPos.x1) * AtlasSize.Width);
                int height = (int)((origTexPos.y2 - origTexPos.y1) * AtlasSize.Height);

                capi.EntityTextureAtlas.AllocateTextureSpace(width, height, out skinTextureSubId, out skinTexPos);

                textureSpaceAllocated = true;
            }

            return base.GetTextureSource();
        }

        public override void TesselateShape()
        {
            base.TesselateShape();

            if (eagent.GearInventory != null)
            {
                reloadSkin();
            }
        }

        public virtual void setShape()
        {
            float x, z, scale;

            switch (entity.WatchedAttributes.GetString("race", "human"))
            {
                case "dwarf":
                    x = z = 1.5f;
                    scale = 0.65f;
                    this.WindWaveIntensity = 0;
                    break;
                case "human":
                default:
                    this.WindWaveIntensity = 1;
                    x = z = scale = 1f;
                    break;
            }

            Shape entityShape = entity.Properties.Client.LoadedShape;

            ShapeElement[] newElements = entityShape.CloneElements();

            newElements[0].ScaleX = x;
            newElements[0].ScaleZ = z;

            Shape newShape = new Shape()
            {
                Elements = newElements,
                Animations = entityShape.Animations,
                AnimationsByCrc32 = entityShape.AnimationsByCrc32,
                AttachmentPointsByCode = entityShape.AttachmentPointsByCode,
                JointsById = entityShape.JointsById,
                TextureWidth = entityShape.TextureWidth,
                TextureHeight = entityShape.TextureHeight,
                TextureSizes = entityShape.TextureSizes,
                Textures = entityShape.Textures,
            };

            newShape.ResolveAndLoadJoints("head");

            this.OverrideEntityShape = newShape;

            entity.Properties.Client.Size = scale;
        }

        public override void reloadSkin()
        {
            if (skinTexPos == null)
            {
                GetTextureSource();
            }

            TextureAtlasPosition origTexPos = capi.EntityTextureAtlas.Positions[entity.Properties.Client.FirstTexture.Baked.TextureSubId];

            LoadedTexture entityAtlas = new LoadedTexture(null)
            {
                TextureId = origTexPos.atlasTextureId,
                Width = capi.EntityTextureAtlas.Size.Width,
                Height = capi.EntityTextureAtlas.Size.Height
            };

            capi.Render.GlToggleBlend(true, EnumBlendMode.Overlay);

            string componentPath = "characters:textures/entity/skin/";

            string[] skinParts = Enum.GetNames(typeof(EnumSkinPart));
            for (int x = 0; x < skinParts.Length; x++)
            {
                AssetLocation componentLoc;
                string skinProperty = skinParts[x];
                float alphaTest = 0.005f;

                if (skinProperty == "hairtype" || skinProperty == "facialhair")
                {
                    if (entity.WatchedAttributes.GetString(skinProperty, "none") == "none")
                    {
                        continue;
                    }
                }

                switch (skinProperty)
                {
                    case "skincolor":
                        componentLoc = new AssetLocation(
                            componentPath +
                            "skins/" +
                            entity.WatchedAttributes.GetString("sex", "male") +
                            "/" +
                            entity.WatchedAttributes.GetString("skincolor", "tan") +
                            ".png"
                            );
                        alphaTest = -1f;
                        break;
                    case "eyecolor":
                        componentLoc = new AssetLocation(
                            componentPath +
                            "eyes/" +
                            entity.WatchedAttributes.GetString("eyecolor", "brown") +
                            ".png"
                            );
                        break;
                    case "facialhair":
                        componentLoc = new AssetLocation(
                            componentPath +
                            "facialhairs/" +
                            entity.WatchedAttributes.GetString("facialhair", "full") +
                            "-" +
                            entity.WatchedAttributes.GetString("haircolor", "brown") +
                            ".png"
                            );
                        break;
                    case "hairtype":
                        componentLoc = new AssetLocation(
                            componentPath +
                            "hairs/" +
                            entity.WatchedAttributes.GetString("hairtype", "m") +
                            "-" +
                            entity.WatchedAttributes.GetString("haircolor", "brown") +
                            ".png"
                            );
                        break;
                    default:
                        componentLoc = new AssetLocation(
                            componentPath +
                            "hairs/" +
                            entity.WatchedAttributes.GetString("hairtype", "m") +
                            "-" +
                            entity.WatchedAttributes.GetString("haircolor", "brown") +
                            ".png"
                            );
                        break;
                }

                LoadedTexture componentTexture = new LoadedTexture(capi);
                BitmapRef bitMap = capi.Assets.Get(componentLoc).ToBitmap(capi);
                capi.Render.GetOrLoadTexture(componentLoc, bitMap, ref componentTexture);

                capi.Render.GlToggleBlend(false);

                capi.EntityTextureAtlas.RenderTextureIntoAtlas(
                    componentTexture,
                    0,
                    0,
                    128,
                    128,
                    skinTexPos.x1 * capi.EntityTextureAtlas.Size.Width,
                    skinTexPos.y1 * capi.EntityTextureAtlas.Size.Height, alphaTest
                );

                capi.Render.GlToggleBlend(true, EnumBlendMode.Overlay);
            }

            // Standard

            int[] renderOrder = new int[]
            {
                (int)EnumCharacterDressType.LowerBody,
                (int)EnumCharacterDressType.Foot,
                (int)EnumCharacterDressType.UpperBody,
                (int)EnumCharacterDressType.UpperBodyOver,
                (int)EnumCharacterDressType.Waist,
                (int)EnumCharacterDressType.Shoulder,
                (int)EnumCharacterDressType.Emblem,
                (int)EnumCharacterDressType.Neck,
                (int)EnumCharacterDressType.Head,
                (int)EnumCharacterDressType.Hand,
                (int)EnumCharacterDressType.Arm,
                (int)EnumCharacterDressType.Face
            };

            if (gearInv == null && (eagent != null && eagent.GearInventory != null))
            {
                eagent.GearInventory.SlotModified += gearSlotModified;
                gearInv = eagent.GearInventory;
            }

            if (gearInv == null)
            {
                return;
            }

            for (int i = 0; i < renderOrder.Length; i++)
            {
                int slotid = renderOrder[i];

                if (gearInv[slotid] == null)
                {
                    continue;
                }
                ItemStack stack = gearInv[slotid].Itemstack;
                if (stack == null) continue;

                int itemTextureSubId = stack.Item.FirstTexture.Baked.TextureSubId;

                TextureAtlasPosition itemTexPos = capi.ItemTextureAtlas.Positions[itemTextureSubId];

                LoadedTexture itemAtlas = new LoadedTexture(null)
                {
                    TextureId = itemTexPos.atlasTextureId,
                    Width = capi.ItemTextureAtlas.Size.Width,
                    Height = capi.ItemTextureAtlas.Size.Height
                };

                capi.EntityTextureAtlas.RenderTextureIntoAtlas(
                    itemAtlas,
                    itemTexPos.x1 * capi.ItemTextureAtlas.Size.Width,
                    itemTexPos.y1 * capi.ItemTextureAtlas.Size.Height,
                    (itemTexPos.x2 - itemTexPos.x1) * capi.ItemTextureAtlas.Size.Width,
                    (itemTexPos.y2 - itemTexPos.y1) * capi.ItemTextureAtlas.Size.Height,
                    skinTexPos.x1 * capi.EntityTextureAtlas.Size.Width,
                    skinTexPos.y1 * capi.EntityTextureAtlas.Size.Height
                );
            }

            capi.Render.GlToggleBlend(true);
            capi.Render.BindTexture2d(skinTexPos.atlasTextureId);
            capi.Render.GlGenerateTex2DMipmaps();

        }


        public override void Dispose()
        {
            base.Dispose();

            capi.Event.ReloadTextures -= reloadSkin;
            if (eagent != null && eagent.GearInventory != null)
            {
                eagent.GearInventory.SlotModified -= gearSlotModified;
            }

            capi.EntityTextureAtlas.FreeTextureSpace(skinTextureSubId);
        }
    }
}