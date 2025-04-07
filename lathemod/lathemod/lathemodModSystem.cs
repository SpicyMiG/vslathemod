using lathemod.src.common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace lathemod
{
    public class lathemodModSystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass(Mod.Info.ModID + ".lathe", typeof(LatheBlock));

            api.RegisterBlockEntityClass("TEST", typeof(BlockEntityLathe));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".belatheentityredirect", typeof(BELatheEntityRedirect));

            api.RegisterItemClass(Mod.Info.ModID + ".latheblank", typeof(ItemLatheBlank));
            api.RegisterItemClass(Mod.Info.ModID + ".itemlatheworkitem", typeof(ItemLatheWorkItem));
            api.RegisterItemClass(Mod.Info.ModID + ".woodturningchisel", typeof(ItemWoodturningChisel));

            api.RegisterBlockEntityBehaviorClass(Mod.Info.ModID + ".BEBHlathe", typeof(BEBehaviorMPConsumerLathe));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from lathe mod server side: " + Lang.Get("lathemod:hello"));

        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from lathe mod client side: " + Lang.Get("lathemod:hello"));
        }
    }

}