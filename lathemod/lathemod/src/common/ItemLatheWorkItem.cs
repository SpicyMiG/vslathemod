using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace lathemod.src.common {
    internal class ItemLatheWorkItem : Item, ILatheWorkable {

        static int nextMeshRefId = 0;

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
            if (!itemstack.Attributes.HasAttribute("voxels")) {
                CachedMeshRef ccmr = ObjectCacheUtil.GetOrCreate(capi, "clearLatheWorkItem" + Variant["wood"], () => {
                    int textureid;
                    byte[,,] voxels = new byte[16, 6, 16];
                    int length = 10;

                    if (itemstack.Attributes.HasAttribute("blanklength")) length = itemstack.Attributes.GetAsInt("blankLength");
                    api.Logger.Event("LatheWorkItem: " + length);
                    ItemLatheBlank.CreateVoxelsFromBlank(capi, ref voxels, length);
                    MeshData mesh = GenMesh(capi, itemstack, voxels, out textureid);

                    return new CachedMeshRef() {
                        meshref = capi.Render.UploadMultiTextureMesh(mesh),
                        TextureId = textureid
                    };
                });
                renderinfo.ModelRef = ccmr.meshref;
                renderinfo.TextureId = ccmr.TextureId;

                base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
                return;
            }

            int meshrefId = itemstack.Attributes.GetInt("meshRefId", -1);  // NextMeshRefId commenced at 0, so -1 is an impossible actual meshrefId

            if (meshrefId == -1) {
                meshrefId = ++nextMeshRefId;
            }

            CachedMeshRef cmr = ObjectCacheUtil.GetOrCreate(capi, "" + meshrefId, () => {
                int textureid;
                byte[,,] voxels = GetVoxels(itemstack);
                MeshData mesh = GenMesh(capi, itemstack, voxels, out textureid);

                return new CachedMeshRef() {
                    meshref = capi.Render.UploadMultiTextureMesh(mesh),
                    TextureId = textureid
                };
            });

            renderinfo.ModelRef = cmr.meshref;
            renderinfo.TextureId = cmr.TextureId;

            itemstack.Attributes.SetInt("meshRefId", meshrefId);

            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }

        public static MeshData GenMesh(ICoreClientAPI capi, ItemStack workitemStack, byte[,,] voxels, out int textureId) {
            textureId = 0;
            if (workitemStack == null) return null;

            MeshData workItemMesh = new MeshData(24, 36, false, true);
            workItemMesh.CustomBytes = new CustomMeshDataPartByte() {
                Conversion = DataConversion.NormalizedFloat,
                Count = workItemMesh.VerticesCount,
                InterleaveSizes = new int[] { 1 },
                Instanced = false,
                InterleaveOffsets = new int[] { 0 },
                InterleaveStride = 1,
                Values = new byte[workItemMesh.VerticesCount]
            };

            TextureAtlasPosition tposWood, tposSlag;

            tposWood = capi.BlockTextureAtlas.GetPosition(capi.World.GetBlock(new AssetLocation("lathemod:woodtex")), workitemStack.Collectible.Variant["wood"]);
            tposSlag = tposWood;

            MeshData woodVoxelMesh = CubeMeshUtil.GetCubeOnlyScaleXyz(1 / 32f, 1 / 32f, new Vec3f(1 / 32f, 1 / 32f, 1 / 32f));
            CubeMeshUtil.SetXyzFacesAndPacketNormals(woodVoxelMesh);
            woodVoxelMesh.CustomBytes = new CustomMeshDataPartByte() {
                Conversion = DataConversion.NormalizedFloat,
                Count = woodVoxelMesh.VerticesCount,
                Values = new byte[woodVoxelMesh.VerticesCount]
            };

            textureId = tposWood.atlasTextureId;
            for (int i = 0; i < 6; i++) woodVoxelMesh.AddTextureId(textureId);

            woodVoxelMesh.XyzFaces = (byte[])CubeMeshUtil.CubeFaceIndices.Clone();
            woodVoxelMesh.XyzFacesCount = 6;
            woodVoxelMesh.Rgba.Fill((byte)255);

            MeshData slagVoxelMesh = woodVoxelMesh.Clone();

            for (int i = 0; i < woodVoxelMesh.Uv.Length; i++) {
                if (i % 2 > 0) {
                    woodVoxelMesh.Uv[i] = tposWood.y1 + woodVoxelMesh.Uv[i] * 2f / capi.BlockTextureAtlas.Size.Height;

                    slagVoxelMesh.Uv[i] = tposSlag.y1 + slagVoxelMesh.Uv[i] * 2f / capi.BlockTextureAtlas.Size.Height;
                } else {
                    woodVoxelMesh.Uv[i] = tposWood.x1 + woodVoxelMesh.Uv[i] * 2f / capi.BlockTextureAtlas.Size.Width;

                    slagVoxelMesh.Uv[i] = tposSlag.x1 + slagVoxelMesh.Uv[i] * 2f / capi.BlockTextureAtlas.Size.Width;
                }
            }

            MeshData metVoxOffset = woodVoxelMesh.Clone();
            MeshData slagVoxOffset = slagVoxelMesh.Clone();

            for (int x = 0; x < 16; x++) {
                for (int y = 0; y < 6; y++) {
                    for (int z = 0; z < 16; z++) {
                        EnumVoxelMaterial mat = (EnumVoxelMaterial)voxels[x, y, z];
                        if (mat == EnumVoxelMaterial.Empty) continue;

                        float px = x / 16f;
                        float py = 10 / 16f + y / 16f;
                        float pz = z / 16f;

                        MeshData mesh = mat == EnumVoxelMaterial.Metal ? woodVoxelMesh : slagVoxelMesh;
                        MeshData meshVoxOffset = mat == EnumVoxelMaterial.Metal ? metVoxOffset : slagVoxOffset;

                        for (int i = 0; i < mesh.xyz.Length; i += 3) {
                            meshVoxOffset.xyz[i] = px + mesh.xyz[i];
                            meshVoxOffset.xyz[i + 1] = py + mesh.xyz[i + 1];
                            meshVoxOffset.xyz[i + 2] = pz + mesh.xyz[i + 2];
                        }

                        float textureSize = 32f / capi.BlockTextureAtlas.Size.Width;

                        float offsetX = px * textureSize;
                        float offsetY = (py * 32f) / capi.BlockTextureAtlas.Size.Width;
                        float offsetZ = pz * textureSize;

                        for (int i = 0; i < mesh.Uv.Length; i += 2) {
                            meshVoxOffset.Uv[i] = mesh.Uv[i] + GameMath.Mod(offsetX + offsetY, textureSize);
                            meshVoxOffset.Uv[i + 1] = mesh.Uv[i + 1] + GameMath.Mod(offsetZ + offsetY, textureSize);
                        }

                        for (int i = 0; i < meshVoxOffset.CustomBytes.Values.Length; i++) {
                            byte glowSub = (byte)GameMath.Clamp(10 * (Math.Abs(x - 8) + Math.Abs(z - 8) + Math.Abs(y - 2)), 100, 250);
                            meshVoxOffset.CustomBytes.Values[i] = (mat == EnumVoxelMaterial.Metal) ? (byte)0 : glowSub;
                        }

                        workItemMesh.AddMeshData(meshVoxOffset);
                    }
                }
            }

            return workItemMesh;
        }

        public static byte[,,] GetVoxels(ItemStack workitemStack) {
            return BlockEntityLathe.deserializeVoxels(workitemStack.Attributes.GetBytes("voxels"));
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            int recipeId = inSlot.Itemstack.Attributes.GetInt("selectedRecipeId");
            LatheRecipe recipe = api.GetLatheRecipes().FirstOrDefault(r => r.RecipeId == recipeId);

            if (recipe == null) {
                dsc.AppendLine("Unknown work item");
                return;
            }

            dsc.AppendLine(Lang.Get("Unfinished {0}", recipe.Output.ResolvedItemstack.GetName()));
        }

        //OLD STUFF
        /*public ItemStack GetBaseMaterial(ItemStack workItemStack) {
            return workItemStack;
        }*/

        public List<LatheRecipe> GetMatchingRecipes(ItemStack stack) {
            stack = GetBaseMaterial(stack);

            return api.GetLatheRecipes()
                .Where(r => r.Ingredient.SatisfiesAsIngredient(stack))
                .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code) //NullReferenceException at r.Output.ResolvedItemStack
                .ToList()
            ;
        }

        public ItemStack TryPlaceOn(ItemStack stack, BlockEntityLathe be) {
            if (be.WorkItemStack != null) return null;

            try {
                be.Voxels = BlockEntityLathe.deserializeVoxels(stack.Attributes.GetBytes("voxels"));
                be.selectedRecipeId = stack.Attributes.GetInt("selectedRecipeId");
            } catch (Exception) {

            }

            return stack.Clone();
        }
        public ItemStack GetBaseMaterial(ItemStack stack) {
            Item item = api.World.GetItem(AssetLocation.Create("lathemod:latheblank-" + Variant["wood"], Attributes?["baseMaterialDomain"].AsString("game")));
            if (item == null) {
                throw new Exception(string.Format("Base material for {0} not found, there is no item with code 'latheblank-{1}'", stack.Collectible.Code, Variant["wood"]));
            }
            return new ItemStack(item);
        }
    }
}
