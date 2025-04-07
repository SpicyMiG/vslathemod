using lathemod.src.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace lathemod.src.client {
    internal class LatheChuckRenderer : IRenderer {
        internal bool ShouldRender;
        internal bool ShouldRotateAutomated;

        public BEBehaviorMPConsumerLathe mechPowerPart;
        BlockEntityLathe attachedLathe;

        private ICoreClientAPI api;
        private BlockPos pos;

        MeshRef meshref;
        public Matrixf ModelMat = new Matrixf();

        public float AngleRad;
        public int degRot = 0;

        public LatheChuckRenderer(ICoreClientAPI coreClientAPI, BlockPos pos, MeshData mesh, BlockEntityLathe attachedLathe) {
            this.api = coreClientAPI;
            this.pos = pos;
            meshref = coreClientAPI.Render.UploadMesh(mesh);
            this.attachedLathe = attachedLathe;
        }

        public LatheChuckRenderer(ICoreClientAPI coreClientAPI, BlockPos pos, MeshData mesh) {
            this.api = coreClientAPI;
            this.pos = pos;
            meshref = coreClientAPI.Render.UploadMesh(mesh);
        }

        public double RenderOrder {
            get { return 0.5; }
        }

        public int RenderRange => 24;

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage) {
            if(meshref == null) return;

            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;
            switch (attachedLathe.degRot) {
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
                        .RotateY((float) Math.PI * 2)
                        .Translate(-12/16f, -2 / 16f, -2/16f)
                        .Values;
                    break;

                case 180: //south
                    prog.ModelMatrix = ModelMat
                        .Identity()
                        .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                        .Translate(8/16f, 12f / 16f, 4 / 16f)
                        .RotateZ(-AngleRad)
                        .Translate(-14 / 16f, -2 / 16f, -0.5f)
                        .Values;
                    break;

                case 90: //west
                    prog.ModelMatrix = ModelMat
                        .Identity()
                        .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                        .Translate(12/16f, 12f / 16f, 8 / 16f)
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
            rpi.RenderMesh(meshref);
            prog.Stop();

            if (ShouldRotateAutomated) {
                AngleRad = mechPowerPart.AngleRad;
            }
        }
        public void Dispose() {
            api.Logger.Event("Disposing chuck renderer");

            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);

            meshref.Dispose();
        }

        
    }
}
