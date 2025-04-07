using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace lathemod.src.common {
    internal interface ILatheWorkable {
        ItemStack TryPlaceOn(ItemStack stack, BlockEntityLathe be);
        List<LatheRecipe> GetMatchingRecipes(ItemStack stack);
        ItemStack GetBaseMaterial(ItemStack workItemStack);
    }
}
