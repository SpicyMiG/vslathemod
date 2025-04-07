using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace lathemod.src.common {
    public class InventoryLathe : InventoryBase, ISlotProvider {
        private ItemSlot[] slots;
        public ItemSlot[] Slots { get { return slots; } }
        

        public InventoryLathe(string inventoryID, ICoreAPI api) : base(inventoryID, api) {
            //slot 0 = input
            //slot 1 = ouput

            slots = base.GenEmptySlots(2);
        }

        public InventoryLathe(string className, string instanceID, ICoreAPI api) : base(className, instanceID, api) {
            slots = base.GenEmptySlots(2);
        }

        public override int Count {
            get { return 2; }
        }

        public override ItemSlot this[int slotId] {
            get {
                if(slotId < 0 || slotId >= Count) return null;
                return slots[slotId];
            }
            set {
                if (slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException(nameof(slotId));
                if (value == null) throw new ArgumentNullException(nameof(value));
                slots[slotId] = value;
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree) {
            slots = SlotsFromTreeAttributes(tree, slots);
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {
            SlotsToTreeAttributes(slots, tree);
        }

        protected override ItemSlot NewSlot(int i) {
            return new ItemSlotSurvival(this);
        }

        public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge) {
            //if (targetSlot == slots[0] && sourceSlot.Itemstack.Collectible.GrindingProps != null) return 4f;
            return base.GetSuitability(sourceSlot, targetSlot, isMerge);
        }

        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot) {
            return null;
        }
    }
}
