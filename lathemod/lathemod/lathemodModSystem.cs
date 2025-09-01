using HarmonyLib;
using lathemod.src.common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using static lathemod.src.Patches.LatheModPatches;

namespace lathemod
{
    public class lathemodModSystem : ModSystem
    {
        private Harmony patcher;

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass(Mod.Info.ModID + ".lathe", typeof(LatheBlock));
            api.RegisterBlockClass(Mod.Info.ModID + ".candleholder", typeof(BlockCandleHolder));

            api.RegisterEntity(Mod.Info.ModID + ".entityprojectilelathe", typeof(EntityProjectileLathe));

            api.RegisterBlockEntityClass(Mod.Info.ModID + ".belathe", typeof(BlockEntityLathe));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".belatheentityredirect", typeof(BELatheEntityRedirect));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".candleholder", typeof(BlockEntityCandleHolder));

            api.RegisterItemClass(Mod.Info.ModID + ".latheblank", typeof(ItemLatheBlank));
            api.RegisterItemClass(Mod.Info.ModID + ".itemlatheworkitem", typeof(ItemLatheWorkItem));
            api.RegisterItemClass(Mod.Info.ModID + ".woodturningchisel", typeof(ItemWoodturningChisel));

            api.RegisterBlockEntityBehaviorClass(Mod.Info.ModID + ".BEBHlathe", typeof(BEBehaviorMPConsumerLathe));

            if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
                patcher = new Harmony(Mod.Info.ModID);
                patcher.PatchCategory(Mod.Info.ModID);
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from lathe mod server side: " + Lang.Get("lathemod:hello"));

        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from lathe mod client side: " + Lang.Get("lathemod:hello"));
        }

        public override void Dispose() {
            
        }
    }

}