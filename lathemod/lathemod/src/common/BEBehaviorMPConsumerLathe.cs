using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent.Mechanics;

namespace lathemod.src.common {
    internal class BEBehaviorMPConsumerLathe : BEBehaviorMPBase {

        protected float resistance = 0.05f;

        public Action OnConnected;
        public Action OnDisconnected;

        public float TrueSpeed { get { return System.Math.Abs(Network?.Speed * GearedRatio ?? 0f); } }

        public BEBehaviorMPConsumerLathe(BlockEntity blockentity) : base(blockentity) {

        }

        public override float GetResistance() {
            return resistance;
        }

        public override void Initialize(ICoreAPI api, JsonObject properties) {
            base.Initialize(api, properties);
            resistance = properties["resistance"].AsFloat(0.05f);

            Shape = properties["mechPartShape"].AsObject<CompositeShape>(null);
            Shape?.Base.WithPathPrefixOnce("lathemod:shapes/").WithPathAppendixOnce(".json");
        }

        protected override MechPowerPath[] GetMechPowerExits(MechPowerPath fromExitTurnDir) {
            // This but' a dead end, baby!
            return new MechPowerPath[0];
        }

        public override void JoinNetwork(MechanicalNetwork network) {
            base.JoinNetwork(network);
            OnConnected?.Invoke();
        }

        public override void LeaveNetwork() {
            base.LeaveNetwork();
            OnDisconnected?.Invoke();
        }
    }
}
