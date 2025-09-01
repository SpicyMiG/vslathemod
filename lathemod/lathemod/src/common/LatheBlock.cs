using Microsoft.VisualBasic;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace lathemod.src.common {
    internal class LatheBlock : BlockMPBase {

        WorldInteraction[] interactions;

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode) {

            //no mech power cons
            if (base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode)) {
                WasPlaced(world, blockSel.Position, null);

                string facing = this.GetBlockEntity<BlockEntityLathe>(blockSel.Position).facing.ToString();
                //api.Logger.Event(facing);
                //string facing = b.Variant["side"];

                switch (facing) {
                    case "north":
                        tryConnect(world, byPlayer, blockSel.Position, BlockFacing.NORTH);
                        break;
                    case "east":
                        tryConnect(world, byPlayer, blockSel.Position, BlockFacing.EAST);
                        break;
                    case "south":
                        tryConnect(world, byPlayer, blockSel.Position, BlockFacing.SOUTH);
                        break;
                    case "west":
                        tryConnect(world, byPlayer, blockSel.Position, BlockFacing.WEST);
                        break;
                }

                //PlaceFakeBlocks(world, blockSel.Position, facing);

                return true;
            }

            return false;
        }

        //deprecated
        /*private void PlaceFakeBlocks(IWorldAccessor world, BlockPos pos, string facing) {
            Block toPlaceBlock = world.GetBlock(new AssetLocation("lathemod:fakeblock"));
            BlockPos newPos = new BlockPos();
            newPos.Z = pos.Z;
            newPos.X = pos.X;
            newPos.Y = pos.Y;

            switch (facing) {
                case "north":
                    newPos.Z = pos.Z + 1;
                    break;
                case "east":
                    newPos.X = pos.X - 1;
                    break;
                case "south":
                    newPos.Z = pos.Z - 1;
                    break;
                case "west":
                    newPos.X = pos.X + 1;
                    break;
            }

            world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, newPos);
            BELatheEntityRedirect be = world.BlockAccessor.GetBlockEntity(newPos) as BELatheEntityRedirect;
            if (be != null) {
                be.Principal = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityLathe;
            } else api.Logger.Event("be null :(");
        }*/

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "latheBlockInteractions", () => {
                List<ItemStack> workableStacklist = new List<ItemStack>();
                List<ItemStack> wtcStacklist = new List<ItemStack>();

                foreach (Item item in api.World.Items) {
                    if (item.Code == null) continue;

                    if (item is ItemLatheBlank) {
                        workableStacklist.Add(new ItemStack(item));
                    }

                    if (item is ItemWoodturningChisel) {
                        wtcStacklist.Add(new ItemStack(item));
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-lathe-takeworkable",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        ShouldApply = (wi, bs, es) => {
                            BlockEntityLathe bel = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityLathe;
                            return bel?.WorkItemStack != null;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-lathe-placeworkable",
                        HotKeyCode = "shift",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = workableStacklist.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                            BlockEntityLathe bel = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityLathe;
                            return bel?.WorkItemStack == null ? wi.Itemstacks : null;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-lathe-turn",
                        MouseButton = EnumMouseButton.Left,
                        Itemstacks = wtcStacklist.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                            BlockEntityLathe bel = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityLathe;
                            return bel?.WorkItemStack == null ? null : wi.Itemstacks;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-lathe-selecttoolmode",
                        HotKeyCode = "toolmodeselect",
                        MouseButton = EnumMouseButton.None,
                        Itemstacks = wtcStacklist.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                            BlockEntityLathe bel = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityLathe;
                            return bel?.WorkItemStack == null ? null : wi.Itemstacks;
                        }
                    }
                };
            });
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            BlockEntityLathe bea = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityLathe;
            if (bea != null) {
                if (bea.OnPlayerInteract(world, byPlayer, blockSel)) {
                    return true;
                }

                return false;
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos) {
            base.OnDecalTesselation(world, decalMesh, pos);
            BlockEntityLathe bect = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityLathe;
            if (bect != null) {
                decalMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, bect.MeshAngle, 0);
            }
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
            BlockEntityLathe bel = blockAccessor.GetBlockEntity(pos) as BlockEntityLathe;
            if (bel != null) {
                Cuboidf[] selectionBoxes = bel.GetSelectionBoxes(blockAccessor, pos);
                float angledeg = Math.Abs(bel.MeshAngle * GameMath.RAD2DEG);
                //api.Logger.Event("SelectionBoxes: " + SelectionBoxes.Length);
                selectionBoxes[0] = angledeg == 0 || angledeg == 180 ? SelectionBoxes[0] : SelectionBoxes[0];//should be 1. rotation?
                return selectionBoxes;
            }

            return base.GetSelectionBoxes(blockAccessor, pos);
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
            return GetSelectionBoxes(blockAccessor, pos);
        }
        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) {
            return true;
        }

        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face) {

        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            //BlockEntityQuern beLathe = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityQuern;

            return false;
        }

        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face) {

            return true;
        }

        public override bool tryConnect(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, BlockFacing face) {
            return base.tryConnect(world, byPlayer, pos, face);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null) {
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack) {
            bool val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);

            if(val) {
                BlockEntityLathe bect = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityLathe;
                if(bect != null) {
                    BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                    double dz = byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                    float angleHor = (float)Math.Atan2(dx, dz);

                    float deg22dot5rad = GameMath.PIHALF / 4;
                    float roundRad = ((int)Math.Round(angleHor / deg22dot5rad)) * deg22dot5rad;
                    bect.MeshAngle = roundRad;
                }
            }
            return val;
        }
    }
}
