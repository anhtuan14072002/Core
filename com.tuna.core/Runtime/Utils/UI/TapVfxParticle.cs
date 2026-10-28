using UnityEngine;
using UnityEngine.UI;

namespace Crusher
{
    [RequireComponent(typeof(ParticleSystem), typeof(CanvasRenderer))]
    public sealed class TapVfxParticle : MaskableGraphic
    {
        private ParticleSystemRenderer _particleRenderer;
        [SerializeField] private Camera _camera;
        private Mesh _mesh;
        private Texture _texture;

        public override Texture mainTexture => _texture != null ? _texture : Texture2D.whiteTexture;

        protected override void Awake()
        {
            base.Awake();
            _particleRenderer = GetComponent<ParticleSystemRenderer>();
            _particleRenderer.enabled = false;
            var sheet = GetComponent<ParticleSystem>().textureSheetAnimation;
            _texture = sheet.enabled && sheet.mode == ParticleSystemAnimationMode.Sprites
                ? sheet.GetSprite(0).texture : material.mainTexture;
            _mesh = new Mesh();
            _mesh.MarkDynamic();
        }

        private void LateUpdate()
        {
            SetVerticesDirty();
        }

        protected override void UpdateGeometry()
        {
            if (_mesh == null || _camera == null)
                return;

            // Keep the prefab's sprite UVs and particle vertex streams.
            _particleRenderer.BakeMesh(_mesh, _camera, false);
            canvasRenderer.SetMesh(_mesh);
        }

        protected override void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
            base.OnDestroy();
        }
    }
}
