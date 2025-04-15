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
            api.Logger.Event(api.GetType().Name + " found " + api.GetLatheRecipes()
                .Where(r => r.Ingredient.SatisfiesAsIngredient(stack))
                .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code)
                .ToList().Count + " matching recipes");

            return api.GetLatheRecipes()
                .Where(r => r.Ingredient.SatisfiesAsIngredient(stack))
                .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code)
                .ToList()
            ;
        }

        public ItemStack TryPlaceOn(ItemStack stack, BlockEntityLathe be) {
            Item item = api.World.GetItem(new AssetLocation("lathemod:latheworkitem-" + Variant["wood"]));
            if (item == null) {
                api.Logger.Event("item == null in ItemLatheBlank");
                return null;
            }

            ItemStack workItemStack = new ItemStack(item);

            if (stack?.ItemAttributes?["blanklength"].Exists == true) {
                length = (int)stack.ItemAttributes?["blanklength"].AsInt();
                api.Logger.Event("blanklength: " + length);
            } else {
                api.Logger.Event("no blankLength attribute found!");
                length = 10;
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

        public static void CreateVoxelsFromBlank(ICoreAPI api, ref byte[,,] voxels, int length = 10) {
            voxels = new byte[16, 6, 16];
            int startX = 9 - (length - 1);

            for (int x = 0; x < length; x++) {
                for (int y = 0; y < 4; y++) {
                    for (int z = 0; z < 4; z++) {
                        voxels[startX + x, y, 6 + z] = (byte)EnumVoxelMaterial.Metal;
                    }
                }
            }
            //TODO: wide blanks (6x6 ideally to fit flowerpot)

            voxels[10, 1, 7] = (byte)EnumVoxelMaterial.Metal;
            voxels[10, 1, 8] = (byte)EnumVoxelMaterial.Metal;
            voxels[10, 2, 7] = (byte)EnumVoxelMaterial.Metal;
            voxels[10, 2, 8] = (byte)EnumVoxelMaterial.Metal;
        }

        public static int AddVoxelsFromBlank(ref byte[,,] voxels, int length) {
            int totalAdded = 0;
            for (int x = 0; x < length; x++) {
                for (int z = 0; z < 3; z++) {
                    int y = 0;
                    int added = 0;
                    while (y < 6 && added < 2) {
                        if (voxels[4 + x, y, 6 + z] == (byte)EnumVoxelMaterial.Empty) {
                            voxels[4 + x, y, 6 + z] = (byte)EnumVoxelMaterial.Metal;
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
