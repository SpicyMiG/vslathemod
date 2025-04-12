using lathemod.src.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace lathemod.src.client {
    internal class LatheWorkItemRenderer : IRenderer {
        private ICoreClientAPI api;
        private BlockPos pos;

        MeshRef workItemMeshRef;
        MeshRef recipeOutlineMeshRef;

        ItemStack blank;
        int texId;

        Vec4f outLineColorMul = new Vec4f(1, 1, 1, 1);
        protected Matrixf ModelMat = new Matrixf();

        SurvivalCoreSystem coreMod;

        BlockEntityLathe beLathe;
        public BEBehaviorMPConsumerLathe mechPowerPart;
        Vec4f glowRgb = new Vec4f();
        protected Vec3f origin = new Vec3f(0, 0, 0);
        public float AngleRad;

        public LatheWorkItemRenderer(BlockEntityLathe beLathe, BlockPos pos, ICoreClientAPI capi) {
            this.pos = pos;
            this.api = capi;
            this.beLathe = beLathe;

            coreMod = capi.ModLoader.GetModSystem<SurvivalCoreSystem>();
        }

        public double RenderOrder {
            get { return 0.5; }
        }

        public int RenderRange {
            get { return 24; }
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage) {
            if (workItemMeshRef == null) return;
            if (stage == EnumRenderStage.AfterFinalComposition) {
                if (api.World.Player?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible is ItemWoodturningChisel) {
                    RenderRecipeOutLine();
                }
                return;
            }

            IRenderAPI rpi = api.Render;
            IClientWorldAccessor worldAccess = api.World;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;

            Vec4f lightrgbs = worldAccess.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
            int extraGlow = GameMath.Clamp((80 - 550) / 2, 0, 255);
            float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(80);
            glowRgb.R = glowColor[0];
            glowRgb.G = glowColor[1];
            glowRgb.B = glowColor[2];
            glowRgb.A = extraGlow / 255f;

            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true);

            IShaderProgram prog = coreMod.anvilShaderProg;
            prog.Use();
            rpi.BindTexture2d(texId);
            prog.Uniform("rgbaAmbientIn", rpi.AmbientColor);

            prog.Uniform("rgbaFogIn", rpi.FogColor);
            prog.Uniform("fogMinIn", rpi.FogMin);
            prog.Uniform("dontWarpVertices", (int)0);
            prog.Uniform("addRenderFlags", (int)0);
            prog.Uniform("fogDensityIn", rpi.FogDensity);
            prog.Uniform("rgbaTint", ColorUtil.WhiteArgbVec);
            prog.Uniform("rgbaLightIn", lightrgbs);
            //prog.Uniform("rgbaGlowIn", glowRgb);
            //prog.Uniform("extraGlow", extraGlow);

            prog.UniformMatrix("modelMatrix", ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                .Values
            );
            prog.UniformMatrix("viewMatrix", rpi.CameraMatrixOriginf);
            prog.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
            

            rpi.RenderMesh(workItemMeshRef);

            prog.UniformMatrix("modelMatrix", rpi.CurrentModelviewMatrix);
            prog.Stop();
            

            /*IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;
            switch (beLathe.degRot) {
                case 0: //north
                    prog.ModelMatrix = ModelMat
                        .Identity()
                        .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                        .Translate(0.5f, 12f / 16f, 12 / 16f)
                        .RotateZ(-AngleRad)
                        .Translate(-2 / 16f, -2 / 16f, -0.5f)
                        .Values;
                    break;
                case 270: //east
                    prog.ModelMatrix = ModelMat
                        .Identity()
                        .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                        .Translate(0.5f, 12f / 16f, 8 / 16f)
                        .RotateX(-AngleRad)
                        .RotateY((float)Math.PI * 2)
                        .Translate(-12 / 16f, -2 / 16f, -2 / 16f)
                        .Values;
                    break;

                case 180: //south
                    prog.ModelMatrix = ModelMat
                        .Identity()
                        .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                        .Translate(8 / 16f, 12f / 16f, 4 / 16f)
                        .RotateZ(-AngleRad)
                        .Translate(-14 / 16f, -2 / 16f, -0.5f)
                        .Values;
                    break;

                case 90: //west
                    prog.ModelMatrix = ModelMat
                        .Identity()
                        .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                        .Translate(12 / 16f, 12f / 16f, 8 / 16f)
                        .RotateY(-(float)Math.PI / 2)
                        .RotateZ(AngleRad)
                        .Translate(-14 / 16f, -2 / 16f, -0.5f)
                        .Values;
                    break;

                default:
                    prog.ModelMatrix = ModelMat
                        .Identity()
                        .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                        .Translate(0.5f, 12f / 16f, 12 / 16f)
                        .RotateZ(AngleRad)
                        .Translate(-2 / 16f, -2 / 16f, -0.5f)
                        .Values;
                    break;
            }

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(workItemMeshRef);
            prog.Stop();

            if (ShouldRotateAutomated) {
                AngleRad = mechPowerPart.AngleRad;
            }*/
        }

        private void RenderRecipeOutLine() {
            if (recipeOutlineMeshRef == null || api.HideGuis) return;
            IRenderAPI rpi = api.Render;
            IClientWorldAccessor worldAccess = api.World;
            EntityPos plrPos = worldAccess.Player.Entity.Pos;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;
            ModelMat.Set(rpi.CameraMatrixOriginf).Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z);
            outLineColorMul.A = 1 - GameMath.Clamp((float)Math.Sqrt(plrPos.SquareDistanceTo(pos.X, pos.Y, pos.Z)) / 5 - 1f, 0, 1);

            float linewidth = 2 * api.Settings.Float["wireframethickness"];
            rpi.LineWidth = linewidth;
            rpi.GLEnableDepthTest();
            rpi.GlToggleBlend(true);

            IShaderProgram prog = rpi.GetEngineShader(EnumShaderProgram.Wireframe);
            prog.Use();
            prog.Uniform("origin", origin);
            prog.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
            prog.UniformMatrix("modelViewMatrix", ModelMat.Values);
            prog.Uniform("colorIn", outLineColorMul);
            rpi.RenderMesh(recipeOutlineMeshRef);
            prog.Stop();

            if (linewidth != 1.6f) rpi.LineWidth = 1.6f;

            rpi.GLDepthMask(false);   // Helps prevent HUD failing to draw at the start of the next frame, on macOS.  This may be the last GL settings call before the frame is finalised.  The block outline renderer sets this to false prior to rendering its mesh.
        }

        public void RegenMesh(ItemStack workitemStack, byte[,,] voxels, bool[,,] recipeToOutlineVoxels) {
            workItemMeshRef?.Dispose();
            workItemMeshRef = null;
            this.blank = workitemStack;

            if (workitemStack == null) return;

            ObjectCacheUtil.Delete(api, "" + workitemStack.Attributes.GetInt("meshRefId"));
            workitemStack.Attributes.RemoveAttribute("meshRefId");

            if (recipeToOutlineVoxels != null) {
                RegenOutlineMesh(recipeToOutlineVoxels, voxels);
            }

            MeshData workItemMesh = ItemLatheWorkItem.GenMesh(api, workitemStack, voxels, out texId);

            workItemMeshRef = api.Render.UploadMesh(workItemMesh);
        }


        private void RegenOutlineMesh(bool[,,] recipeToOutlineVoxels, byte[,,] voxels) {
            MeshData recipeOutlineMesh = new MeshData(24, 36, false, false, true, false);
            recipeOutlineMesh.SetMode(EnumDrawMode.Lines);

            int greenCol = api.ColorPreset.GetColor("anvilColorGreen");
            int orangeCol = api.ColorPreset.GetColor("anvilColorRed");
            MeshData greenVoxelMesh = LineMeshUtil.GetCube(greenCol);
            MeshData orangeVoxelMesh = LineMeshUtil.GetCube(orangeCol);
            for (int i = 0; i < greenVoxelMesh.xyz.Length; i++) {
                greenVoxelMesh.xyz[i] = greenVoxelMesh.xyz[i] / 32f + 1 / 32f;
                orangeVoxelMesh.xyz[i] = orangeVoxelMesh.xyz[i] / 32f + 1 / 32f;
            }
            MeshData voxelMeshOffset = greenVoxelMesh.Clone();


            int yMax = recipeToOutlineVoxels.GetLength(1);
            //api.Logger.Event("\nX: " + recipeToOutlineVoxels.GetLength(0) + "\nY: " + recipeToOutlineVoxels.GetLength(1) + "\nZ: " + recipeToOutlineVoxels.GetLength(2));

            for (int x = 0; x < 16; x++) {
                for (int y = 0; y < 6; y++) {
                    for (int z = 0; z < 16; z++) {
                        bool requireMetalHere = y >= yMax ? false : recipeToOutlineVoxels[x, y, z];

                        EnumVoxelMaterial mat = (EnumVoxelMaterial)voxels[x, y, z];

                        if (requireMetalHere && mat == EnumVoxelMaterial.Metal) continue;
                        if (!requireMetalHere && mat == EnumVoxelMaterial.Empty) continue;

                        float px = x / 16f;
                        float py = 10 / 16f + y / 16f;
                        float pz = z / 16f;

                        for (int i = 0; i < greenVoxelMesh.xyz.Length; i += 3) {
                            voxelMeshOffset.xyz[i] = px + greenVoxelMesh.xyz[i];
                            voxelMeshOffset.xyz[i + 1] = py + greenVoxelMesh.xyz[i + 1];
                            voxelMeshOffset.xyz[i + 2] = pz + greenVoxelMesh.xyz[i + 2];
                        }

                        voxelMeshOffset.Rgba = (requireMetalHere && mat == EnumVoxelMaterial.Empty) ? greenVoxelMesh.Rgba : orangeVoxelMesh.Rgba;

                        recipeOutlineMesh.AddMeshData(voxelMeshOffset);
                    }
                }
            }

            recipeOutlineMeshRef?.Dispose();
            recipeOutlineMeshRef = null;
            if (recipeOutlineMesh.VerticesCount > 0) {
                recipeOutlineMeshRef = api.Render.UploadMesh(recipeOutlineMesh);
            }
        }


        public void Dispose() {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            api.Event.UnregisterRenderer(this, EnumRenderStage.AfterFinalComposition);

            recipeOutlineMeshRef?.Dispose();
            workItemMeshRef?.Dispose();
        }
    }
}
