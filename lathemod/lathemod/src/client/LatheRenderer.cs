using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using lathemod.src.common;

namespace lathemod.src.client {
    internal class LatheRenderer : IRenderer {

        MeshRef meshref;
        public Matrixf ModelMat = new Matrixf();

        private ICoreClientAPI api;
        private BlockPos pos;
        BlockEntityLathe be;

        public LatheRenderer(ICoreClientAPI api, BlockEntityLathe be, BlockPos pos, MeshData mesh) {
            this.api = api;
            this.pos = pos;
            this.be = be;

            meshref = api.Render.UploadMesh(mesh);
        }
        public double RenderOrder {
            get { return 0.5; }
        }

        public int RenderRange => 24;

        public void Dispose() {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            meshref.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage) {
            if (meshref == null) return;

            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;
            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;

            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                .Translate(0f, 0f, 0f)
                .Translate(0f, 0f, 0f)
                .Values;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(meshref);
            prog.Stop();
        }
    }
}
