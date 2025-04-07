using lathemod.src.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace lathemod {
    public static class ModApiAdditions {
        public static List<LatheRecipe> GetLatheRecipes(this ICoreAPI api) {
            return api.ModLoader.GetModSystem<LatheRegistrySystem>().TurningRecipes;
        }

        public static void RegisterTurningRecipe(this ICoreServerAPI api, LatheRecipe r) {
            api.ModLoader.GetModSystem<LatheRegistrySystem>().RegisterTurningRecipe(r);
        }
    }

    public class LatheRegistrySystem : ModSystem {
        public List<LatheRecipe> TurningRecipes = new List<LatheRecipe>();
        public static bool canRegister = true;

        public override double ExecuteOrder() {
            return 1.1;
        }

        public override void StartPre(ICoreAPI api) {
            canRegister = true;
        }

        public override void Start(ICoreAPI api) {
            TurningRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<LatheRecipe>>("turningrecipes").Recipes;
        }

        public void RegisterTurningRecipe(LatheRecipe recipe) {
            if (!canRegister) throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
            recipe.RecipeId = TurningRecipes.Count + 1;

            TurningRecipes.Add(recipe);
        }

        public override void AssetsLoaded(ICoreAPI api) {
            if (!(api is ICoreServerAPI sapi)) return;

            api.ModLoader.GetModSystem<RecipeLoader>().LoadRecipes<LatheRecipe>("turning recipe", "recipes/turning", r => RegisterTurningRecipe(r));
        }

    }
}
