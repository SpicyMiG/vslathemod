using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace lathemod.src.common {
    internal class BELatheEntityRedirect : BlockEntity {
        public BlockEntityLathe Principal { get; set; }
        public override void Initialize(ICoreAPI api) {
            base.Initialize(api);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null) {
            base.OnBlockPlaced(byItemStack);
        }
    }
}
