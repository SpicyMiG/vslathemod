using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent
{
    public class BlockCandleHolder : Block
    {
        public bool Empty
        {
            get { return Variant["state"] == "empty"; }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Empty)
            {
                ItemStack heldStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
                if (heldStack != null && heldStack.Collectible.Code.Path.Equals("candle"))
                {
                    byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                    byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();

                    Block filledBlock = world.GetBlock(CodeWithVariant("state", "filled"));
                    world.BlockAccessor.ExchangeBlock(filledBlock.BlockId, blockSel.Position);

                    if (Sounds?.Place != null)
                    {
                        world.PlaySoundAt(Sounds.Place, blockSel.Position, 0.1, byPlayer);
                    }

                    return true;
                }
            } else
            {
                ItemStack stack = new ItemStack(world.GetItem(new AssetLocation("game:candle")));
                if (byPlayer.InventoryManager.TryGiveItemstack(stack, true))
                {
                    Block filledBlock = world.GetBlock(CodeWithVariant("state", "empty"));
                    world.BlockAccessor.ExchangeBlock(filledBlock.BlockId, blockSel.Position);

                    if (Sounds?.Place != null)
                    {
                        world.PlaySoundAt(Sounds.Place, blockSel.Position, 0.1, byPlayer);
                    }

                    return true;
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (Empty)
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-candlestick-addcandle",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = new ItemStack[] { new ItemStack(world.GetBlock(new AssetLocation("candle"))) }
                    }
                }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
            } else
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-candlestick-removecandle",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = null
                    }
                }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
            }
            
        }
    }
}
