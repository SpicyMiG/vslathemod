using Cairo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace lathemod.src.common {
    internal class ItemWoodturningChisel : Item {

        SkillItem[] toolModes;

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);

            if (api is ICoreClientAPI capi) {
                toolModes = ObjectCacheUtil.GetOrCreate(api, "woodturningChiselModes", () => {
                    SkillItem[] modes = new SkillItem[2];

                    modes[0] = new SkillItem() { Code = new AssetLocation("single"), Name = Lang.Get("Single") }.WithIcon(capi, DrawHit);
                    modes[1] = new SkillItem() { Code = new AssetLocation("debugmode"), Name = Lang.Get("Debug Mode") }.WithIcon(capi, DrawHit);
                    //modes[2] = new SkillItem() { Code = new AssetLocation("triple"), Name = Lang.Get("Triple-wide") }.WithIcon(capi, DrawHit);

                    return modes;
                });
            }
        }

        public override void OnUnloaded(ICoreAPI api) {
            for (int i = 0; toolModes != null && i < toolModes.Length; i++) {
                toolModes[i]?.Dispose();
            }
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling) {
            if (blockSel == null) {
                base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
                return;
            }

            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (byPlayer == null) return;

            BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);

            if (!(byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is LatheBlock)) {
                base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
                api.Logger.Event("OnHeldAttackStart, BE not lathe. Returning. Block found: " + byEntity.World.BlockAccessor.GetBlock(blockSel.Position));
                //handling = EnumHandHandling.PreventDefault;
                //turnWood(byEntity, slot);
                return;
            }

            handling = EnumHandHandling.PreventDefault;
        }

        public override void OnHeldActionAnimStart(ItemSlot slot, EntityAgent byEntity, EnumHandInteract type) {
            var eplr = byEntity as EntityPlayer;
            var byPlayer = eplr.Player;
            var blockSel = eplr.BlockSelection;
            if (type != EnumHandInteract.HeldItemAttack || blockSel == null) return;

            BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            BlockEntityLathe bel = null;
            if (!(byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is LatheBlock)) {
                api.Logger.Event("OnHeldActionAnimStart, BE not lathe. Returning. Block found: " + byEntity.World.BlockAccessor.GetBlock(blockSel.Position));
                if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BELatheEntityRedirect) {
                    bel = (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BELatheEntityRedirect).Principal;
                    api.Logger.Event("Found BE");
                    bel.OnBeginUse(byPlayer, blockSel);
                    startHitAction(slot, byEntity, false);

                    return;
                }
            }

            if(bel == null) bel = be as BlockEntityLathe;
            if (bel == null) return;
            bel.OnBeginUse(byPlayer, blockSel);

            startHitAction(slot, byEntity, false);
        }

        private void startHitAction(ItemSlot slot, EntityAgent byEntity, bool merge) {
            string anim = GetHeldTpHitAnimation(slot, byEntity);

            float framesound = CollectibleBehaviorAnimationAuthoritative.getSoundAtFrame(byEntity, anim);
            float framehitaction = CollectibleBehaviorAnimationAuthoritative.getHitDamageAtFrame(byEntity, anim);

            slot.Itemstack.TempAttributes.SetBool("isLatheAction", true);
            var state = byEntity.AnimManager.GetAnimationState(anim);

            //byEntity.AnimManager.RegisterFrameCallback(new AnimFrameCallback() { Animation = anim, Frame = framesound, Callback = () => turnWoodSound(byEntity, merge) });
            byEntity.AnimManager.RegisterFrameCallback(new AnimFrameCallback() { Animation = anim, Frame = framehitaction, Callback = () => turnWood(byEntity, slot) });
        }

        protected virtual void turnWood(EntityAgent byEntity, ItemSlot slot) {
            //api.Logger.Event("turnWood");
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            if (byPlayer == null) return;

            var blockSel = byPlayer.CurrentBlockSelection;
            if (blockSel == null) {
                api.Logger.Event("blockSel == null");
                return; 
            }

            BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            BlockEntityLathe bel = null;

            if (!(byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is LatheBlock)) {
                api.Logger.Event("turnWood, BE not lathe. Returning. Block found: " + byEntity.World.BlockAccessor.GetBlock(blockSel.Position));
                if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BELatheEntityRedirect) {
                    bel = (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BELatheEntityRedirect).Principal;
                }
            }
            if(bel == null)bel = be as BlockEntityLathe;

            if (bel == null) return;

            if (api.World.Side == EnumAppSide.Client) {
                //api.Logger.Event("turning!");
                
                bel.OnUseOver(byPlayer, blockSel.SelectionBoxIndex);
            }


            slot.Itemstack?.TempAttributes.SetBool("isLatheAction", false);
        }

        public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason) {
            if (!slot.Itemstack.TempAttributes.GetBool("isLatheAction")) {
                return base.OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSelection, entitySel, cancelReason);
            }

            if (cancelReason == EnumItemUseCancelReason.Death || cancelReason == EnumItemUseCancelReason.Destroyed) {
                slot.Itemstack.TempAttributes.SetBool("isLathelAction", false);
                return true;
            }

            return false;
        }

        public override bool OnHeldAttackStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel) {
            if (!slot.Itemstack.TempAttributes.GetBool("isLatheAction")) {
                return base.OnHeldAttackStep(secondsUsed, slot, byEntity, blockSelection, entitySel);
            }

            if (blockSelection == null) return false;

            BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSelection.Position);

            string animCode = GetHeldTpHitAnimation(slot, byEntity);
            return byEntity.AnimManager.IsAnimationActive(animCode);
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) {
            if (blockSel == null) return null;
            Block block = forPlayer.Entity.World.BlockAccessor.GetBlock(blockSel.Position);
            return block is LatheBlock ? toolModes : null;
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) {
            return slot.Itemstack.Attributes.GetInt("toolMode");
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode) {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }

        private void DrawHit(Context cr, int x, int y, float width, float height, double[] colordoubles) {
            Pattern pattern = null;
            Matrix matrix = cr.Matrix;

            cr.Save();
            float w = 227;
            float h = 218;
            float scale = Math.Min(width / w, height / h);
            matrix.Translate(x + Math.Max(0, (width - w * scale) / 2), y + Math.Max(0, (height - h * scale) / 2));
            matrix.Scale(scale, scale);
            cr.Matrix = matrix;

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(159.96875, 110.125);
            cr.CurveTo(159.96875, 134.976563, 139.824219, 155.125, 114.96875, 155.125);
            cr.CurveTo(90.117188, 155.125, 69.96875, 134.976563, 69.96875, 110.125);
            cr.CurveTo(69.96875, 85.273438, 90.117188, 65.125, 114.96875, 65.125);
            cr.CurveTo(139.824219, 65.125, 159.96875, 85.273438, 159.96875, 110.125);
            cr.ClosePath();
            cr.MoveTo(159.96875, 110.125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 1;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(159.96875, 110.125);
            cr.CurveTo(159.96875, 134.976563, 139.824219, 155.125, 114.96875, 155.125);
            cr.CurveTo(90.117188, 155.125, 69.96875, 134.976563, 69.96875, 110.125);
            cr.CurveTo(69.96875, 85.273438, 90.117188, 65.125, 114.96875, 65.125);
            cr.CurveTo(139.824219, 65.125, 159.96875, 85.273438, 159.96875, 110.125);
            cr.ClosePath();
            cr.MoveTo(159.96875, 110.125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(110.71875, 0);
            cr.LineTo(119.21875, 0);
            cr.LineTo(119.21875, 52);
            cr.LineTo(110.71875, 52);
            cr.ClosePath();
            cr.MoveTo(110.71875, 0);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 1;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(110.71875, 0);
            cr.LineTo(119.21875, 0);
            cr.LineTo(119.21875, 52);
            cr.LineTo(110.71875, 52);
            cr.ClosePath();
            cr.MoveTo(110.71875, 0);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(110.71875, 164.710938);
            cr.LineTo(119.21875, 164.710938);
            cr.LineTo(119.21875, 216.710938);
            cr.LineTo(110.71875, 216.710938);
            cr.ClosePath();
            cr.MoveTo(110.71875, 164.710938);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 1;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(110.71875, 164.710938);
            cr.LineTo(119.21875, 164.710938);
            cr.LineTo(119.21875, 216.710938);
            cr.LineTo(110.71875, 216.710938);
            cr.ClosePath();
            cr.MoveTo(110.71875, 164.710938);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(173.804688, 105.875);
            cr.LineTo(225.804688, 105.875);
            cr.LineTo(225.804688, 114.375);
            cr.LineTo(173.804688, 114.375);
            cr.ClosePath();
            cr.MoveTo(173.804688, 105.875);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 1;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(173.804688, 105.875);
            cr.LineTo(225.804688, 105.875);
            cr.LineTo(225.804688, 114.375);
            cr.LineTo(173.804688, 114.375);
            cr.ClosePath();
            cr.MoveTo(173.804688, 105.875);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(0, 105.375);
            cr.LineTo(52, 105.375);
            cr.LineTo(52, 113.875);
            cr.LineTo(0, 113.875);
            cr.ClosePath();
            cr.MoveTo(0, 105.375);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 1;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(0, 105.375);
            cr.LineTo(52, 105.375);
            cr.LineTo(52, 113.875);
            cr.LineTo(0, 113.875);
            cr.ClosePath();
            cr.MoveTo(0, 105.375);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(173.757813, 68.78125);
            cr.LineTo(167.75, 62.769531);
            cr.LineTo(204.515625, 26.003906);
            cr.LineTo(210.527344, 32.011719);
            cr.ClosePath();
            cr.MoveTo(173.757813, 68.78125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 1;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(173.757813, 68.78125);
            cr.LineTo(167.75, 62.769531);
            cr.LineTo(204.515625, 26.003906);
            cr.LineTo(210.527344, 32.011719);
            cr.ClosePath();
            cr.MoveTo(173.757813, 68.78125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            matrix = new Matrix(-0.7071, -0.7071, 0.7071, -0.7071, 289.3736, 214.6403);
            pattern.Matrix = matrix;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(32.007813, 190.707031);
            cr.LineTo(25.996094, 184.699219);
            cr.LineTo(62.757813, 147.925781);
            cr.LineTo(68.769531, 153.933594);
            cr.ClosePath();
            cr.MoveTo(32.007813, 190.707031);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 1;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(32.007813, 190.707031);
            cr.LineTo(25.996094, 184.699219);
            cr.LineTo(62.757813, 147.925781);
            cr.LineTo(68.769531, 153.933594);
            cr.ClosePath();
            cr.MoveTo(32.007813, 190.707031);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            matrix = new Matrix(-0.7072, -0.707, 0.707, -0.7072, -38.8126, 322.5648);
            pattern.Matrix = matrix;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(68.78125, 62.773438);
            cr.LineTo(62.769531, 68.78125);
            cr.LineTo(26, 32.015625);
            cr.LineTo(32.011719, 26.003906);
            cr.ClosePath();
            cr.MoveTo(68.78125, 62.773438);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 1;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(68.78125, 62.773438);
            cr.LineTo(62.769531, 68.78125);
            cr.LineTo(26, 32.015625);
            cr.LineTo(32.011719, 26.003906);
            cr.ClosePath();
            cr.MoveTo(68.78125, 62.773438);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            matrix = new Matrix(-0.7071, 0.7071, -0.7071, -0.7071, 114.4105, 47.3931);
            pattern.Matrix = matrix;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(210.527344, 184.6875);
            cr.LineTo(204.515625, 190.695313);
            cr.LineTo(167.75, 153.921875);
            cr.LineTo(173.761719, 147.910156);
            cr.ClosePath();
            cr.MoveTo(210.527344, 184.6875);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 1;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(210.527344, 184.6875);
            cr.LineTo(204.515625, 190.695313);
            cr.LineTo(167.75, 153.921875);
            cr.LineTo(173.761719, 147.910156);
            cr.ClosePath();
            cr.MoveTo(210.527344, 184.6875);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            matrix = new Matrix(-0.7072, 0.707, -0.707, -0.7072, 442.6037, 155.3283);
            pattern.Matrix = matrix;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Restore();
        }
    }
}
