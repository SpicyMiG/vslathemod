using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace lathemod.src.common {
    internal class ItemLatheBlank : Item, ILatheWorkable {
        public ItemStack GetBaseMaterial(ItemStack workItemStack) {
            return workItemStack;
        }
        public int length;

        public List<LatheRecipe> GetMatchingRecipes(ItemStack stack) {
            /*api.Logger.Event(api.GetType().Name + " found " + api.GetLatheRecipes()
                .Where(r => r.Ingredient.SatisfiesAsIngredient(stack))
                .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code)
                .ToList().Count + " matching recipes");*/

            return api.GetLatheRecipes()
                .Where(r => r.Ingredient.SatisfiesAsIngredient(stack))
                .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code)
                .ToList()
            ;
        }

        public ItemStack TryPlaceOn(ItemStack stack, BlockEntityLathe be) {
            Item item = api.World.GetItem(new AssetLocation("lathemod:latheworkitem-" + Variant["wood"]));
            if (item == null) {
                api.Logger.Error("item == null in ItemLatheBlank");
                return null;
            }

            ItemStack workItemStack = new ItemStack(item);

            if (stack?.ItemAttributes?["blanklength"].Exists == true) {
                length = (int)stack.ItemAttributes?["blanklength"].AsInt();
                //api.Logger.Event("blanklength: " + length);
            } else {
                api.Logger.Error("no blankLength attribute found!");
                length = 20;
            }

            if (be.WorkItemStack == null) {
                CreateVoxelsFromBlank(api, ref be.Voxels, length);
            } else {
                if (!string.Equals(be.WorkItemStack.Collectible.Variant["wood"], stack.Collectible.Variant["wood"])) {
                    if (api.Side == EnumAppSide.Client) (api as ICoreClientAPI).TriggerIngameError(this, "notequal", Lang.Get("Must be the same wood to add voxels"));
                    return null;
                }

                if (AddVoxelsFromBlank(ref be.Voxels, length) == 0) {
                    if (api.Side == EnumAppSide.Client) (api as ICoreClientAPI).TriggerIngameError(this, "requiresturning", Lang.Get("Try turning down before adding additional voxels"));
                    return null;
                }
            }

            return workItemStack;
        }

        public static void CreateVoxelsFromBlank(ICoreAPI api, ref byte[,,] voxels, int length = 20) {
            voxels = new byte[32, 12, 32];
            int startX = 20 - (length - 1);

            for (int x = 0; x < length - 1; x++) {
                for (int y = 0; y < 10; y++) {
                    for (int z = 0; z < 10; z++) {
                        voxels[startX + x, y, 11 + z] = (byte)EnumVoxelMaterial.Metal;
                    }
                }
            }

            //voxels[20, 1, 15] = (byte)EnumVoxelMaterial.Metal;
            //voxels[20, 1, 16] = (byte)EnumVoxelMaterial.Metal;
            //voxels[20, 2, 15] = (byte)EnumVoxelMaterial.Metal;
            //voxels[20, 2, 16] = (byte)EnumVoxelMaterial.Metal;
        }

        public static int AddVoxelsFromBlank(ref byte[,,] voxels, int length) {
            int totalAdded = 0;
            for (int x = 0; x < length - 1; x++) {
                for (int z = 0; z < 12; z++) {
                    int y = 0;
                    int added = 0;
                    while (y < 6 && added < 2) {
                        if (voxels[9 + x, y, 12 + z] == (byte)EnumVoxelMaterial.Empty) {
                            voxels[9 + x, y, 12 + z] = (byte)EnumVoxelMaterial.Metal;
                            added++;
                            totalAdded++;
                        }

                        y++;
                    }
                }
            }
            return totalAdded;
        }
    }
}
