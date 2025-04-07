using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace lathemod.src.common {
    public class LatheRecipe : LayeredVoxelRecipe<LatheRecipe>, IByteSerializable {
        /// <summary>
        /// Defines a turning recipe, to be created on a lathe. Uses a total of 6 layers, and gets its properties from <see cref="LayeredVoxelRecipe{T}"/>.
        /// </summary>
        /// <example><code language="json">
        ///{
        ///	"ingredient": {
        ///		"type": "item",
        ///		"code": "latheblank-*",
        ///		"name": "wood",
        ///		"allowedVariants": [ "oak", "maple", "larch" ]
        ///	},
        ///	"name": "shaft",
        ///	"pattern": [
        ///		[
        ///			"____",
        ///			"_##_",
        ///			"_##_",
        ///			"____"
        ///		],
        ///		[
        ///			"####",
        ///			"#__#",
        ///			"#__#",
        ///			"####"
        ///		]
        ///	],
        ///	"output": {
        ///		"type": "item",
        ///		"code": "shaft-{wood}"
        ///	}
        ///}
        /// </code></example>
        [DocumentAsJson]
        public override int QuantityLayers => 6;
        protected override bool RotateRecipe => false;
        //public bool[,,] LatheVoxels = new bool[16, 6, 16];

        public override string RecipeCategoryCode => "turning";

        public override LatheRecipe Clone() {
            LatheRecipe recipe = new LatheRecipe();

            //Voxels = LatheVoxels;

            recipe.Pattern = (string[][])Pattern.Clone();
            recipe.Ingredient = Ingredient.Clone();
            recipe.Output = Output.Clone();
            recipe.Name = Name;
            recipe.RecipeId = RecipeId;

            return recipe;
        }

        public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging) {
            if (Pattern == null || base.Ingredient == null || Output == null) {
                world.Logger.Error("{1} Recipe with output {0} has no ingredient pattern or missing ingredient/output. Ignoring recipe.", Output, RecipeCategoryCode);
                return false;
            }

            if (!base.Ingredient.Resolve(world, RecipeCategoryCode + " recipe")) {
                world.Logger.Error("{1} Recipe with output {0}: Cannot resolve ingredient in {1}.", Output, sourceForErrorLogging, RecipeCategoryCode);
                return false;
            }

            if (!Output.Resolve(world, sourceForErrorLogging, base.Ingredient.Code)) {
                return false;
            }

            GenVoxels();
            return true;
        }

        public new void GenVoxels() {
            int length = Pattern[0][0].Length;
            int num = Pattern[0].Length;
            int num2 = Pattern.Length;
            if (num > 16 || num2 > QuantityLayers || length > 16) {
                throw new Exception(string.Format("Invalid {1} recipe {0}! Either Width or length is beyond 16 voxels or height is beyond {2} voxels", base.Name, RecipeCategoryCode, QuantityLayers));
            }

            for (int i = 0; i < Pattern.Length; i++) {
                if (Pattern[i].Length != num) {
                    throw new Exception(string.Format("Invalid {4} recipe {3}! Layer {0} has a width of {1}, which is not the same as the first layer width of {2}. All layers need to be sized equally.", i, Pattern[i].Length, num, base.Name, RecipeCategoryCode));
                }

                for (int j = 0; j < Pattern[i].Length; j++) {
                    if (Pattern[i][j].Length != length) {
                        throw new Exception(string.Format("Invalid {5} recipe {3}! Layer {0}, line {4} has a length of {1}, which is not the same as the first layer length of {2}. All layers need to be sized equally.", i, Pattern[i][j].Length, length, base.Name, j, RecipeCategoryCode));
                    }
                }
            }

            int num3 = (16 - num) / 2;
            int num4 = (16 - length) / 2;
            for (int k = 0; k < Math.Min(num, 16); k++) {
                for (int l = 0; l < Math.Min(num2, QuantityLayers); l++) {
                    for (int m = 0; m < Math.Min(length, 16); m++) {
                        if (RotateRecipe) {
                            Voxels[m + num4, l, k + num3] = Pattern[l][k][m] != '_' && Pattern[l][k][m] != ' ';
                        } else {
                            try {
                                Voxels[k + num3, l, m + num4] = Pattern[l][k][m] != '_' && Pattern[l][k][m] != ' ';
                            } catch (Exception e) { throw new Exception(string.Format("{0}", Voxels.Length)); }
                        }
                    }
                }
            }
        }

    }
}
