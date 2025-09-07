using lathemod.src.client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace lathemod.src.common {
    public class BlockEntityLathe : BlockEntity {

        /* Quern Type Stuff */
        internal InventoryLathe inventory;

        float timer = 0;

        public float inputTurnTime;
        public float prevInputTurnTime;

        public int degRot = 0;

        //GuiDialogBlockEntityLathe clientDialog;
        GuiDialog dlg;
        bool automated;
        BEBehaviorMPConsumerLathe mpc;
        LatheChuckRenderer renderer;
        LatheRenderer baseRenderer;

        /* Anvil Type Stuff */

        ItemStack workItemStack;
        ItemStack returnOnCancelStack;
        public int selectedRecipeId = -1;

        public BlockFacing facing;
        public byte[,,] Voxels = new byte[32, 12, 32];
        public byte[,,] OrigVoxels = null;
        float voxYOff = 19 / 32f;
        Cuboidf[] selectionBoxes = new Cuboidf[1];
        public float MeshAngle;
        MeshData currentMesh;
        public int rotation = 0;
        private int rotationSteps = 0;

        public static SimpleParticleProperties bigWoodchips;
        public static SimpleParticleProperties smallWoodchips;

        LatheWorkItemRenderer workItemRenderer;

        #region Getters

        public string Material {
            get { return Block.LastCodePart(); }
        }

        public float TurnSpeed {
            get {

                if (automated && mpc.Network != null) {
                    float speed = Math.Clamp(mpc.TrueSpeed, 0, 3.5f); //otherwise it's just silly and easy to destroy blanks
                    return speed;
                }

                return 0f;
            }
        }
        #endregion

        #region Particles
        static BlockEntityLathe() {
            smallWoodchips = new SimpleParticleProperties(
                2, 5,
                ColorUtil.ToRgba(255, 95, 75, 35), //TODO: match to wood variant colour
                new Vec3d(), new Vec3d(),
                new Vec3f(-3f, 8f, -3f),
                new Vec3f(3f, 12f, 3f),
                0.1f,
                1f,
                0.25f, 0.25f,
                EnumParticleModel.Quad
            );
            smallWoodchips.VertexFlags = 128;
            smallWoodchips.AddPos.Set(1 / 16f, 0, 1 / 16f);
            smallWoodchips.ParticleModel = EnumParticleModel.Quad;
            smallWoodchips.LifeLength = 0.03f;
            smallWoodchips.MinVelocity = new Vec3f(-0.5f, -0.5f, -0.5f);
            smallWoodchips.AddVelocity = new Vec3f(0.2f, -0.1f, 0.2f);
            smallWoodchips.MinQuantity = 6;
            smallWoodchips.AddQuantity = 12;
            smallWoodchips.MinSize = 0.1f;
            smallWoodchips.MaxSize = 0.1f;



            bigWoodchips = new SimpleParticleProperties(
                4, 8, //min, max
                ColorUtil.ToRgba(255, 95, 75, 35), //TODO: match to wood variant colour
                new Vec3d(), new Vec3d(), //minPox, maxPos
                new Vec3f(-0.5f, -0.5f, -0.5f), //minVel
                new Vec3f(0.2f, -0.1f, 0.2f),    //maxVel //TODO: if voxel covered, spawn particles to side
                0.5f, //lifeLength
                1f,   //gravityEffect
                0.25f, 0.25f //minSize, maxSize
            );
            bigWoodchips.VertexFlags = 128;
            bigWoodchips.AddPos.Set(1 / 16f, 0, 1 / 16f);
            bigWoodchips.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 0f);
            bigWoodchips.Bounciness = 0.2f;
            bigWoodchips.addLifeLength = 2f;
        }
        #endregion
        public BlockEntityLathe() {
            this.inventory = new InventoryLathe(null, null);
            this.inventory.SlotModified += OnSlotModified;
        }

        public override void Initialize(ICoreAPI api) {
            base.Initialize(api);

            facing = BlockFacing.FromCode(Block.Variant["side"]);
            if (facing == null) { Api.World.BlockAccessor.SetBlock(0, Pos); return; }
            Vec3i dir = facing.Normali;

            if (api.Side == EnumAppSide.Client) {
                renderer = new LatheChuckRenderer(api as ICoreClientAPI, Pos, GenMesh("chuck"), this);
                baseRenderer = new LatheRenderer(api as ICoreClientAPI, this, Pos, GenMesh("base"));
                
                renderer.mechPowerPart = this.mpc;

                if (automated) {
                    renderer.ShouldRender = true;
                    renderer.ShouldRotateAutomated = true;
                }

                (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "lathe");
                (api as ICoreClientAPI).Event.RegisterRenderer(baseRenderer, EnumRenderStage.Opaque, "lathebase");

                if (latheBaseMesh == null) {
                    latheBaseMesh = GenMesh("base");
                }
                if (latheChuckMesh == null) {
                    latheChuckMesh = GenMesh("chuck");
                }

                /*baseRenderer = new LatheRenderer(Api as ICoreClientAPI, this, Pos, GenMesh("base"));
                (Api as ICoreClientAPI).Event.RegisterRenderer(baseRenderer, EnumRenderStage.Opaque, "lathebase");*/
            }

            workItemStack?.ResolveBlockOrItem(api.World);

            if (api is ICoreClientAPI capi) {
                capi.Event.RegisterRenderer(workItemRenderer = new LatheWorkItemRenderer(this, Pos, capi), EnumRenderStage.Opaque);
                capi.Event.RegisterRenderer(workItemRenderer, EnumRenderStage.AfterFinalComposition);

                workItemRenderer.mechPowerPart = this.mpc;

                RegenMeshAndSelectionBoxes();
                //capi.Tesselator.TesselateBlock(Block, out currentMesh);
                capi.Event.ColorsPresetChanged += RegenMeshAndSelectionBoxes;
            }
        }

        internal MeshData GenMesh(string type = "base") {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            if (block.BlockId == 0) return null;
            string v = Api.World.BlockAccessor.GetBlock(Pos)
                .ToString()
                .Split(" ")[1]
                .Split("/")[0]
                .Split("-")[1];
            int deg;
            //Api.Logger.Event("BELathe GenMesh Variant Code: " + v);
            switch (v) {
                case "north":
                    deg = 0;
                    break;
                case "east":
                    deg = 270;
                    break;
                case "south":
                    deg = 180;
                    break;
                case "west":
                    deg = 90;
                    break;

                default:
                    deg = 0;
                    break;
            }
            //Api.Logger.Event("deg: " + deg);
            degRot = deg;

            MeshData mesh;
            ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;


            if (type == "base") {
                Shape shape = Vintagestory.API.Common.Shape.TryGet(Api, "lathemod:shapes/block/metal/lathe/" + type + ".json");
                mesher.TesselateShape(Block, shape, out mesh, new Vec3f(0, deg, 0));

            } else {
                mesher.TesselateShape(block, Shape.TryGet(Api, "lathemod:shapes/block/metal/lathe/" + type + "-" + v + ".json"), out mesh);
            }

            return mesh;
        }

        MeshData latheBaseMesh {
            get {
                object value;
                Api.ObjectCache.TryGetValue("lathebasemesh", out value);
                return (MeshData)value;
            }
            set { Api.ObjectCache["lathebasemesh"] = value; }
        }

        MeshData latheChuckMesh {
            get {
                object value = null;
                Api.ObjectCache.TryGetValue("lathechuckmesh", out value);
                return (MeshData)value;
            }
            set { Api.ObjectCache["lathechuckmesh"] = value; }
        }

        public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve) {
            base.CreateBehaviors(block, worldForResolve);

            mpc = GetBehavior<BEBehaviorMPConsumerLathe>();
            if (mpc != null) {
                mpc.OnConnected = () => {
                    //Api.Logger.Event("Automated!");
                    automated = true;

                    if (renderer != null) {
                        renderer.ShouldRender = true;
                        renderer.ShouldRotateAutomated = true;
                    }
                };

                mpc.OnDisconnected = () => {
                    automated = false;
                    if (renderer != null) {
                        renderer.ShouldRender = false;
                        renderer.ShouldRotateAutomated = false;
                    }
                };
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) {
            base.OnTesselation(mesher, tessThreadTesselator);
            if (Block == null) return false;

            return true;
        }

        private void OnRetesselated() {
            if (renderer == null) return; // Maybe already disposed
            renderer.ShouldRender = automated;

        }

        public override void OnBlockRemoved() {
            base.OnBlockRemoved();

            //clientDialog?.TryClose();

            renderer?.Dispose();
            baseRenderer?.Dispose();
            workItemRenderer?.Dispose();
            renderer = null;
            baseRenderer = null;
            workItemRenderer = null;
        }

        public override void OnBlockUnloaded() {
            base.OnBlockUnloaded();

            renderer?.Dispose();
            baseRenderer?.Dispose();
            workItemRenderer?.Dispose();
            dlg?.TryClose();
            dlg?.Dispose();

            if (Api is ICoreClientAPI capi) capi.Event.ColorsPresetChanged -= RegenMeshAndSelectionBoxes;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc) {
            dsc.AppendLine(Lang.Get("Wood Lathe"));

            if (workItemStack == null || SelectedRecipe == null) {
                return;
            }

            dsc.AppendLine(Lang.Get("Output: {0}", SelectedRecipe.Output?.ResolvedItemstack?.GetName()));
            if (!automated) {
                dsc.AppendLine(Lang.Get("No mechanical power!"));
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
            base.FromTreeAttributes(tree, worldForResolving);

            Voxels = deserializeVoxels(tree.GetBytes("voxels"));
            workItemStack = tree.GetItemstack("workItemStack");
            selectedRecipeId = tree.GetInt("selectedRecipeId", -1);
            rotation = tree.GetInt("rotation");

            if (Api != null && workItemStack != null) {
                workItemStack.ResolveBlockOrItem(Api.World);
            }

            RegenMeshAndSelectionBoxes();

            MeshAngle = tree.GetFloat("meshAngle", MeshAngle);

            //container.FromTreeAttributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {
            base.ToTreeAttributes(tree);
            tree.SetBytes("voxels", serializeVoxels(Voxels));
            tree.SetItemstack("workItemStack", workItemStack);
            tree.SetInt("selectedRecipeId", selectedRecipeId);
            tree.SetInt("rotation", rotation);
            tree.SetFloat("meshAngle", MeshAngle);
        }

        internal bool OnPlayerInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            if (byPlayer.Entity.Controls.ShiftKey) {
                return TryPut(world, byPlayer, blockSel);
            } else return TryTake(world, byPlayer, blockSel);
        }


        private bool TryPut(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            //Api.Logger.Event("Hotbar Item: " + slot.ToString());
            if (slot.Itemstack == null) return false;
            ItemStack stack = slot.Itemstack;

            ILatheWorkable workableobj = stack.Collectible as ILatheWorkable;

            if (workableobj == null) {
                Api.Logger.Event("Not workable item");
                return false;
            }

            if (mpc.Network != null) {

            }

            ItemStack newWorkItemStack = workableobj.TryPlaceOn(stack, this);
            if (newWorkItemStack != null) {
                if (workItemStack == null) {
                    workItemStack = newWorkItemStack;
                    rotation = workItemStack.Attributes.GetInt("rotation");
                } else return false;

                string facing = Block.Variant["side"];
                //Api.Logger.Event("TryPut: " + facing);
                switch (facing) {
                    case "north":
                        RotateWorkItem(false);
                        rotationSteps = 1;
                        break;
                    case "east":
                        rotationSteps = 0;
                        break;
                    case "south":
                        RotateWorkItem(false);
                        RotateWorkItem(false);
                        RotateWorkItem(false);
                        rotationSteps = 3;
                        break;
                    case "west":
                        RotateWorkItem(false);
                        RotateWorkItem(false);
                        rotationSteps = 2;
                        break;
                }
                if (selectedRecipeId < 0) {
                    var list = workableobj.GetMatchingRecipes(stack);
                    if (list.Count == 1) {
                        selectedRecipeId = list[0].RecipeId;
                    } else {
                        if (world.Side == EnumAppSide.Client) {
                            //Api.Logger.Event("selectedRecipeId: " + selectedRecipeId + ", workitem: " + stack.ToString());
                            OpenDialog(stack);
                        }
                    }
                }

                returnOnCancelStack = slot.TakeOut(1);
                slot.MarkDirty();

                Api.World.Logger.Audit("{0} Put 1x{1} in to Lathe at {2}.",
                    byPlayer?.PlayerName,
                    newWorkItemStack.Collectible.Code,
                    Pos);

                if (Api.Side == EnumAppSide.Server) {
                    // Let the server decide the shape, then send the stuff to client, and then show the correct voxels
                    // instead of the voxels flicker thing when both sides do it
                    RegenMeshAndSelectionBoxes();
                }

                CheckIfFinished(byPlayer);
                MarkDirty();
                return true;
            }

            return false;
        }

        public virtual void CheckIfFinished(IPlayer byPlayer) {
            if (SelectedRecipe == null) return;

            if (MatchesRecipe() && Api.World is IServerWorldAccessor) {
                Voxels = new Byte[32, 12, 32];
                ItemStack outstack = SelectedRecipe.Output.ResolvedItemstack.Clone();
                workItemStack = null;

                selectedRecipeId = -1;

                if (byPlayer?.InventoryManager.TryGiveItemstack(outstack) == true) {
                    Api.World.PlaySoundFor(new AssetLocation("game:sounds/player/collect"), byPlayer, 24);
                } else {
                    Api.World.SpawnItemEntity(outstack, Pos.ToVec3d().Add(0.5, 0.626, 0.5));
                }
                Api.World.Logger.Audit("{0} Took 1x{1} from Lathe at {2}.",
                    byPlayer?.PlayerName,
                    outstack.Collectible.Code,
                    Pos
                );

                RegenMeshAndSelectionBoxes();
                MarkDirty();
                Api.World.BlockAccessor.MarkBlockDirty(Pos);
            }
        }

        public void ditchWorkItemStack(IPlayer byPlayer = null) {
            if (workItemStack == null) return;

            ItemStack ditchedStack;
            if (SelectedRecipe == null) {
                ditchedStack = returnOnCancelStack ?? (workItemStack.Collectible as ILatheWorkable).GetBaseMaterial(workItemStack);
            } else {
                for (int i = 0; i < rotationSteps; i++) {
                    RotateWorkItem(true);
                    //it's a little silly but it works B)
                }
                workItemStack.Attributes.SetBytes("voxels", serializeVoxels(Voxels));
                workItemStack.Attributes.SetInt("selectedRecipeId", selectedRecipeId);

                ditchedStack = workItemStack;
            }

            if (byPlayer == null || !byPlayer.InventoryManager.TryGiveItemstack(ditchedStack)) {
                Api.World.SpawnItemEntity(ditchedStack, Pos);
            }
            Api.World.Logger.Audit("{0} Took 1x{1} from Lathe at {2}.",
                byPlayer?.PlayerName,
                ditchedStack.Collectible.Code,
                Pos
            );

            clearWorkSpace();
        }

        protected void clearWorkSpace() {
            workItemStack = null;
            Voxels = new byte[32, 12, 32];
            RegenMeshAndSelectionBoxes();
            MarkDirty();
            rotation = 0;
            selectedRecipeId = -1;
        }

        public bool[,,] recipeVoxels {
            get {
                if (SelectedRecipe == null) return null;

                bool[,,] origVoxels = SelectedRecipe.Voxels;
                bool[,,] rotVoxels = new bool[origVoxels.GetLength(0), origVoxels.GetLength(1), origVoxels.GetLength(2)];

                if (rotation == 0) return origVoxels;

                for (int i = 0; i < rotation / 90; i++) {
                    for (int x = 0; x < origVoxels.GetLength(0); x++) {
                        for (int y = 0; y < origVoxels.GetLength(1); y++) {
                            for (int z = 0; z < origVoxels.GetLength(2); z++) {
                                rotVoxels[z, y, x] = origVoxels[32 - x - 1, y, z];
                            }
                        }
                    }

                    origVoxels = (bool[,,])rotVoxels.Clone();
                }

                return rotVoxels;
            }
        }

        public static byte[,,] deserializeVoxels(byte[] data) {
            byte[,,] voxels = new byte[32, 12, 32];

            if (data == null || data.Length < 32 * 12 * 32 / partsPerByte) return voxels;

            int pos = 0;

            for (int x = 0; x < 32; x++) {
                for (int y = 0; y < 11; y++) {
                    for (int z = 0; z < 32; z++) {
                        int bitpos = bitsPerByte * (pos % partsPerByte);
                        voxels[x, y, z] = (byte)((data[pos / partsPerByte] >> bitpos) & 0x3);

                        pos++;
                    }
                }
            }

            return voxels;
        }

        public ItemStack WorkItemStack {
            get { return workItemStack; }
        }

        private bool MatchesRecipe() {
            if (SelectedRecipe == null) return false;

            int ymax = Math.Min(12, SelectedRecipe.QuantityLayers);

            bool[,,] recipeVoxels = this.recipeVoxels; // Otherwise we cause lag spikes

            for (int x = 0; x < 32; x++) {
                for (int y = 0; y < ymax; y++) {
                    for (int z = 0; z < 32; z++) {
                        byte desiredMat = (byte)(recipeVoxels[x, y, z] ? EnumVoxelMaterial.Metal : EnumVoxelMaterial.Empty);

                        if (Voxels[x, y, z] != desiredMat) {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        static int bitsPerByte = 2;
        static int partsPerByte = 8 / bitsPerByte;

        public static byte[] serializeVoxels(byte[,,] voxels) {
            byte[] data = new byte[32 * 12 * 32 / partsPerByte];
            int pos = 0;

            for (int x = 0; x < 32; x++) {
                for (int y = 0; y < 11; y++) {
                    for (int z = 0; z < 32; z++) {
                        int bitpos = bitsPerByte * (pos % partsPerByte);
                        data[pos / partsPerByte] |= (byte)((voxels[x, y, z] & 0x3) << bitpos);
                        pos++;
                    }
                }
            }

            return data;
        }

        internal Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos) {
            return selectionBoxes;
        }

        private bool TryTake(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            if (workItemStack == null) return false;

            ditchWorkItemStack(byPlayer);
            return true;
        }

        internal void OpenDialog(ItemStack ingredient) {
            List<LatheRecipe> recipes = (ingredient.Collectible as ILatheWorkable).GetMatchingRecipes(ingredient);

            List<ItemStack> stacks = recipes
                .Select(r => r.Output.ResolvedItemstack)
                .ToList();

            foreach (ItemStack stack in stacks) {
                //Api.Logger.Event("recipe: " + stack.ToString());
            }

            IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;
            ICoreClientAPI capi = Api as ICoreClientAPI;

            dlg?.Dispose();
            dlg = new GuiDialogBlockEntityRecipeSelector(
                Lang.Get("Select lathe recipe"),
                stacks.ToArray(),
                (selectedIndex) => {
                    selectedRecipeId = recipes[selectedIndex].RecipeId;
                    capi.Network.SendBlockEntityPacket(Pos, (int)EnumLathePacket.SelectRecipe, SerializerUtil.Serialize(recipes[selectedIndex].RecipeId));
                },
                () => {
                    capi.Network.SendBlockEntityPacket(Pos, (int)EnumLathePacket.CancelSelect);
                },
                Pos,
                Api as ICoreClientAPI
            );
            dlg.TryOpen();
        }

        public LatheRecipe SelectedRecipe {
            get {
                return Api.GetLatheRecipes().FirstOrDefault(r => r.RecipeId == selectedRecipeId);
            }
        }

        private void OnSlotModified(int slodid) {
            if (Api is ICoreClientAPI) {
                //clientDialog.Update(inputTurnTime, maxTurningTime());
            }

            if (slodid == 0) {
                if (InputSlot.Empty) {
                    inputTurnTime = 0.0f; //reset progress to 0 if item removed
                }
            }
        }
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data) {
            base.OnReceivedClientPacket(player, packetid, data);
            if (packetid == (int)EnumLathePacket.SelectRecipe) {
                int recipeid = SerializerUtil.Deserialize<int>(data);
                LatheRecipe recipe = Api.GetLatheRecipes().FirstOrDefault(r => r.RecipeId == recipeid);

                if (recipe == null) {
                    Api.World.Logger.Error("Client tried to selected lathe recipe with id {0}, but no such recipe exists!");
                    ditchWorkItemStack(player);
                    return;
                }

                var list = (workItemStack?.Collectible as ItemLatheWorkItem)?.GetMatchingRecipes(workItemStack);
                if (list == null || list.FirstOrDefault(r => r.RecipeId == recipeid) == null) {
                    Api.World.Logger.Error("Client tried to selected lathe recipe with id {0}, but it is not a valid one for the given work item stack! (" + workItemStack.ToString() + ")", recipe.RecipeId);
                    ditchWorkItemStack(player);
                    return;
                }


                selectedRecipeId = recipe.RecipeId;

                // Tell server to save this chunk to disk again
                MarkDirty();
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
            }

            if (packetid == (int)EnumAnvilPacket.CancelSelect) {
                ditchWorkItemStack(player);
                return;
            }

            if (packetid == (int)EnumAnvilPacket.OnUserOver) {
                Vec3i voxelPos;
                using (MemoryStream ms = new MemoryStream(data)) {
                    BinaryReader reader = new BinaryReader(ms);
                    voxelPos = new Vec3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                }

                OnUseOver(player, voxelPos, new BlockSelection() { Position = Pos });
            }
        }

        internal void OnUseOver(IPlayer byPlayer, int selectionBoxIndex) {
            // box index 0 is the lathe itself
            if (selectionBoxIndex <= 0 || selectionBoxIndex >= selectionBoxes.Length) {
                //Api.Logger.Event("selectionBoxIndex <= 0 || selectionBoxIndex >= selectionBoxes.Length");
                return;
            }

            Cuboidf box = selectionBoxes[selectionBoxIndex];
            //Api.Logger.Event("selectionBoxIndex: " + selectionBoxIndex);
            Vec3i voxelPos = new Vec3i(
                (int)(32 * box.X1),
                (int)(32 * box.Y1) - 20,
                (int)(32 * box.Z1));

            OnUseOver(byPlayer, voxelPos, new BlockSelection() { Position = Pos, SelectionBoxIndex = selectionBoxIndex });
        }

        internal void OnUseOver(IPlayer byPlayer, Vec3i voxelPos, BlockSelection blockSel) {
            timer += TurnSpeed;
            //Api.Logger.Event("timer: " + timer + ", TurnSpeed: " + TurnSpeed);
            if (voxelPos == null) {
                Api.Logger.Error("voxelPos == null");
                return;
            }

            if (SelectedRecipe == null) {
                Api.Logger.Error("SelectedRecipe == null");
                ditchWorkItemStack();
                return;
            }

            // Send a custom network packet for server side, because
            // serverside blockselection index is inaccurate
            if (Api.Side == EnumAppSide.Client) {
                SendUseOverPacket(byPlayer, voxelPos);
            }


            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack == null) { //add or no power
                Api.Logger.Error("slot.Itemstack == null");
                return;
            }
            int toolMode = slot.Itemstack.Collectible.GetToolMode(slot, byPlayer, blockSel);

            float yaw = GameMath.Mod(byPlayer.Entity.Pos.Yaw, 2 * GameMath.PI);

            //Api.Logger.Event("toolMode: " + toolMode.ToString());

            EnumVoxelMaterial voxelMat = (EnumVoxelMaterial)Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z];

            if (voxelMat != EnumVoxelMaterial.Empty && timer > 5) {
                spawnParticles(voxelPos, voxelMat, byPlayer);

                //Api.Logger.Event("voxelMat != EnumVoxelMaterial.Empty");
                switch (toolMode) {
                    case 0: OnSplit(voxelPos); break;
                    case 1: DebugPos(voxelPos); break; //replace with wider chisel tools?
                                                       //case 2: OnSplit3(voxelPos); break;
                }
                timer = 0;

                RegenMeshAndSelectionBoxes();
                Api.World.BlockAccessor.MarkBlockDirty(Pos);
                Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
                slot.Itemstack.Collectible.DamageItem(Api.World, byPlayer.Entity, slot);

                if (!HasAnyMetalVoxel()) {
                    clearWorkSpace();
                    return;
                }
            }

            CheckIfFinished(byPlayer);
            MarkDirty();
        }

        private void spawnParticles(Vec3i voxelPos, EnumVoxelMaterial voxelMat, IPlayer byPlayer) {
            if (voxelMat == EnumVoxelMaterial.Metal) {

                bigWoodchips.MinPos = Pos.ToVec3d().AddCopy(voxelPos.X / 32f, voxYOff + voxelPos.Y / 32f + 0.0625f, voxelPos.Z / 32f);
                bigWoodchips.AddPos.Set(1 / 32f, 0, 1 / 32f);
                bigWoodchips.VertexFlags = (byte)GameMath.Clamp((int)(80 - 550) / 2, 32, 128);

                Api.World.SpawnParticles(bigWoodchips, byPlayer);


                smallWoodchips.MinPos = Pos.ToVec3d().AddCopy(voxelPos.X / 32f, voxYOff + voxelPos.Y / 32f + 0.0625f, voxelPos.Z / 32f);
                smallWoodchips.VertexFlags = (byte)GameMath.Clamp((int)(80 - 550) / 3, 32, 128);
                smallWoodchips.AddPos.Set(1 / 32f, 0, 1 / 32f);

                Api.World.SpawnParticles(smallWoodchips, byPlayer);
            }
        }

        internal void OnBeginUse(IPlayer byPlayer, BlockSelection blockSel) {
        }

        public virtual void OnSplit(Vec3i voxelPos) {
            //Api.Logger.Event("OnSplit");
            if (rotation == 0 || rotation == 180) {
                //centers: X = 6.5, Y = 1.5, Z = 7.5
                int zo = 6;
                int x = voxelPos.X;
                int y = voxelPos.Y;
                int z = voxelPos.Z;

                //Api.Logger.Event("X: " + x + ", Y:" + y + "Z: " + z);
                try {
                    if (Voxels[x, y, z + 1] == (int)EnumVoxelMaterial.Metal && Voxels[x, y, z - 1] == (int)EnumVoxelMaterial.Metal ||
                    Voxels[x, y + 1, z] == (int)EnumVoxelMaterial.Metal && Voxels[x, y - 1, z] == (int)EnumVoxelMaterial.Metal) {
                        if (x != 15 && x != 0 && z != 15 && z != 0) {
                            if (Voxels[x + 1, y, z] != (int)EnumVoxelMaterial.Empty && Voxels[x - 1, y, z] != (int)EnumVoxelMaterial.Empty) {
                                return;
                            }
                        }
                    }

                    if((y == 1 || y == 2) && (z == 7 || z == 8)) {
                        if(x != 0 && x != 15) {
                            if (Voxels[x + 1, y, z] == (int)EnumVoxelMaterial.Metal && Voxels[x - 1, y, z] == (int)EnumVoxelMaterial.Metal) {
                                //Api.Logger.Event("Can't split work item");
                                return; 
                            }
                        }
                    }

                    if ((z == 6 || z == 9) && (y == 1 || y == 2)) {
                        if (CheckCornersIntactWE(x)) {
                            return;
                        }
                    } else if ((z == 7 || z == 8) && (y == 3 || y == 0)) {
                        if (CheckCornersIntactWE(x)) {
                            return;
                        }
                    }

                    Voxels[x, y, z] = 0; //hit center
                    Voxels[x, y, 15 - z] = 0; //opposite side
                    Voxels[x, 3 - y, z] = 0; //opposite vertical
                    Voxels[x, 3 - y, 15 - z] = 0; //opposite vertical and side

                    Voxels[x, z - zo, y + zo] = 0; //rotate 90 degrees clockwise
                    Voxels[x, z - zo, 15 - y - zo] = 0; //rotate 90 degrees clockwise and opposite vertical
                    Voxels[x, 3 - z + zo, y + zo] = 0; //rotate 90 degrees clockwise and opposite horizontal
                    Voxels[x, 3 - z + zo, 15 - y - zo] = 0; //rotate 90 degrees clockwise and opposite horizontal and vertical

                    //Voxels[x, 15 - z + zo, 3 - y - zo] = 0; // rotate -90 degrees clockwise //womp womp
                    //Voxels[x, 15 - z + zo, y + zo] = 0;     // rotate -90 degrees clockwise and opposite vertical
                    //Voxels[x, z - zo, 3 - y - zo] = 0;      // rotate -90 degrees clockwise and opposite horizontal
                    //Voxels[x, z - zo, y + zo] = 0;          // rotate -90 degrees clockwise and opposite horizontal and vertical

                    //Api.Logger.Event("Split at " + voxelPos.ToString());
                    //Api.Logger.Event("x-base");

                } catch (IndexOutOfRangeException ie) {
                    Api.Logger.Error("Voxel out of range!");
                }

            } else {
                int xo = 6;
                int x = voxelPos.X;
                int y = voxelPos.Y;
                int z = voxelPos.Z;

                try {
                    //Api.Logger.Event("Split at " + voxelPos.ToString());

                    if (Voxels[x + 1, y, z] == (int)EnumVoxelMaterial.Metal && Voxels[x - 1, y, z] == (int)EnumVoxelMaterial.Metal ||
                    Voxels[x, y + 1, z] == (int)EnumVoxelMaterial.Metal && Voxels[x, y - 1, z] == (int)EnumVoxelMaterial.Metal) {
                        if (x != 15 && x != 0 && z != 15 && z != 0) {
                            if (Voxels[x, y, z + 1] != (int)EnumVoxelMaterial.Empty && Voxels[x, y, z - 1] != (int)EnumVoxelMaterial.Empty) {
                                return;
                            }
                        }
                    }

                    if ((y == 1 || y == 2) && (x == 7 || x == 8)) {
                        if (z != 0 && z != 15) {
                            if (Voxels[x, y, z + 1] == (int)EnumVoxelMaterial.Metal && Voxels[x, y, z - 1] == (int)EnumVoxelMaterial.Metal) {
                                //Api.Logger.Event("Can't split work item");
                                return;
                            }
                        }

                    }

                    if((x == 6 || x == 9) && (y == 1 || y == 2)) {
                        if(CheckCornersIntactNS(z)) {
                            return;
                        }
                    }else if ((x == 7 || x == 8) && (y == 3 || y == 0)) {
                        if (CheckCornersIntactNS(z)) {
                            return;
                        }
                    }

                    Voxels[x, y, z] = 0;
                    Voxels[15 - x, y, z] = 0;
                    Voxels[x, 3 - y, z] = 0;
                    Voxels[15 - x, 3 - y, z] = 0;

                    Voxels[y + xo, x - xo, z] = 0;
                    Voxels[15 - y - xo, x - xo, z] = 0;
                    Voxels[y + xo, 3 - x + xo, z] = 0;
                    Voxels[15 - y - xo, 3 - x + xo, z] = 0;

                    //Api.Logger.Event("z-base");
                    //Voxels[15 + y - xo, x + xo, z] = 0;
                    //Voxels[y + xo, x + xo, z] = 0;
                    //Voxels[15 + y - xo, 3 - x - xo, z] = 0; failures
                    //Voxels[y + xo, 3 - x - xo, z] = 0;

                    //Voxels[15 - y + xo, x - xo, z] = 0;
                    //Voxels[y + xo, x - xo, z] = 0;
                    //Voxels[15 - y + xo, 3 - x + xo, z] = 0;
                    //Voxels[y + xo, 3 - x + xo, z] = 0;
                } catch (IndexOutOfRangeException ie) {
                    Api.Logger.Error("Voxel out of range!");
                }
            }
        }
        public virtual void DebugPos(Vec3i voxelPos) {
            Api.Logger.Event(voxelPos.ToString());
            //for debug : )

        }

        public bool CheckCornersIntactNS(int z) {
            if (Voxels[6, 0, z] == 1 || Voxels[9, 3, z] == 1) {
                //Api.Logger.Event("No floating corners");
                return true;
            }

            if (Voxels[6, 0, z] == 1 || Voxels[9, 0, z] == 1) {
                //Api.Logger.Event("No floating corners");
                return true;
            }

            return false;
        }
        public bool CheckCornersIntactWE(int x) {
            if (Voxels[x, 0, 9] == 1 || Voxels[x, 3, 6] == 1) {
                //Api.Logger.Event("No floating corners");
                return true;
            }

            if (Voxels[x, 0, 6] == 1 || Voxels[x, 3, 9] == 1) {
                //Api.Logger.Event("No floating corners");
                return true;
            }

            return false;
        }

        private bool RotateWorkItem(bool ccw) {
            byte[,,] rotVoxels = new byte[32, 12, 32];

            for (int x = 0; x < 32; x++) {
                for (int y = 0; y < 11; y++) {
                    for (int z = 0; z < 32; z++) {
                        if (ccw) {
                            rotVoxels[z, y, x] = Voxels[x, y, 32 - z - 1];
                        } else {
                            rotVoxels[z, y, x] = Voxels[32 - x - 1, y, z];
                        }

                    }
                }
            }

            rotation = (rotation + 90) % 360;

            Voxels = rotVoxels;
            RegenMeshAndSelectionBoxes();
            MarkDirty();

            return true;
        }
        bool HasAnyMetalVoxel() {
            for (int x = 0; x < 32; x++) {
                for (int y = 0; y < 11; y++) {
                    for (int z = 0; z < 32; z++) {
                        if (Voxels[x, y, z] == (byte)EnumVoxelMaterial.Metal) return true;
                    }
                }
            }

            return false;
        }
        protected void SendUseOverPacket(IPlayer byPlayer, Vec3i voxelPos) {
            byte[] data;

            using (MemoryStream ms = new MemoryStream()) {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(voxelPos.X);
                writer.Write(voxelPos.Y);
                writer.Write(voxelPos.Z);
                data = ms.ToArray();
            }

            ((ICoreClientAPI)Api).Network.SendBlockEntityPacket(
                Pos,
                (int)EnumAnvilPacket.OnUserOver,
                data
            );
        }

        protected void RegenMeshAndSelectionBoxes() {
            if (workItemRenderer != null) {
                workItemRenderer.RegenMesh(workItemStack, Voxels, recipeVoxels);
            }

            List<Cuboidf> boxes = new List<Cuboidf>();
            boxes.Add(null);

            for (int x = 0; x < 32; x++) {
                for (int y = 0; y < 11; y++) {
                    for (int z = 0; z < 32; z++) {
                        if (Voxels[x, y, z] != (byte)EnumVoxelMaterial.Empty) {
                            float py = y + 20;
                            boxes.Add(new Cuboidf(
                                x / 32f,
                                py / 32f,
                                z / 32f,
                                x / 32f + 1 / 32f,
                                py / 32f + 1 / 32f,
                                z / 32f + 1 / 32f));
                        }
                    }
                }
            }

            selectionBoxes = boxes.ToArray();
            //Api.Logger.Debug("RegenMeshAndSelectionBoxes: " + selectionBoxes.Length + " boxes of " + counter);
            //throw new NotImplementedException();
        }

        #region Helper getters


        public ItemSlot InputSlot {
            get { return inventory[0]; }
        }

        public ItemSlot OutputSlot {
            get { return inventory[1]; }
        }

        public ItemStack InputStack {
            get { return inventory[0].Itemstack; }
            set { inventory[0].Itemstack = value; inventory[0].MarkDirty(); }
        }

        public ItemStack OutputStack {
            get { return inventory[1].Itemstack; }
            set { inventory[1].Itemstack = value; inventory[1].MarkDirty(); }
        }

        public bool IsAutomated {
            get { return automated; }
        }

        #endregion

    }
}

public enum EnumLathePacket {
    OpenDialog = 1004,
    SelectRecipe = 1005,
    OnUserOver = 1006,
    CancelSelect = 1007
}
