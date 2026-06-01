using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Scripts.Rendering
{
    public sealed class ScreenSpaceHoverOutlineFeature : ScriptableRendererFeature
    {
        private const string MaskShaderName = "Hidden/Game/HoverOutlineMask";
        private const string OutlineShaderName = "Hidden/Game/ScreenSpaceHoverOutline";
        private const string MaskTextureName = "_WOM_HoverOutlineMask";
        private const int MaxOutlineRadiusPixels = 8;

        private static readonly List<RendererEntry> ActiveRenderers = new List<RendererEntry>(64);
        private static int _activeOwnerId;
        private static Color _outlineColor = Color.red;
        private static float _outlineWidthPixels = 3f;

        public Material maskMaterial;
        public Material outlineMaterial;
        public bool gameCamerasOnly = true;

        private MaskPass _maskPass;
        private CompositePass _compositePass;
        private Material _runtimeMaskMaterial;
        private Material _runtimeOutlineMaterial;

        public static bool HasActiveRenderers
        {
            get { return ActiveRenderers.Count > 0; }
        }

        public static Color ActiveOutlineColor
        {
            get { return _outlineColor; }
        }

        public static float ActiveOutlineWidthPixels
        {
            get { return _outlineWidthPixels; }
        }

        public static void SetTarget(
            Object owner,
            List<Renderer> renderers,
            List<int> subMeshCounts,
            Color color,
            float widthPixels)
        {
            if (owner == null)
            {
                return;
            }

            ActiveRenderers.Clear();
            _activeOwnerId = owner.GetInstanceID();
            _outlineColor = color;
            _outlineWidthPixels = Mathf.Clamp(widthPixels, 1f, MaxOutlineRadiusPixels);

            int rendererCount = renderers != null ? renderers.Count : 0;
            for (int i = 0; i < rendererCount; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                int subMeshCount = 1;
                if (subMeshCounts != null && i < subMeshCounts.Count)
                {
                    subMeshCount = Mathf.Max(1, subMeshCounts[i]);
                }

                ActiveRenderers.Add(new RendererEntry(renderer, subMeshCount));
            }
        }

        public static void ClearTarget(Object owner)
        {
            if (owner == null)
            {
                return;
            }

            if (_activeOwnerId != owner.GetInstanceID())
            {
                return;
            }

            ActiveRenderers.Clear();
            _activeOwnerId = 0;
        }

        public static List<RendererEntry> GetActiveRenderers()
        {
            return ActiveRenderers;
        }

        public override void Create()
        {
            Material resolvedMaskMaterial = ResolveMaterial(maskMaterial, MaskShaderName, ref _runtimeMaskMaterial);
            Material resolvedOutlineMaterial = ResolveMaterial(outlineMaterial, OutlineShaderName, ref _runtimeOutlineMaterial);

            _maskPass = new MaskPass(resolvedMaskMaterial)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingSkybox
            };

            _compositePass = new CompositePass(resolvedOutlineMaterial, _maskPass)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (gameCamerasOnly && renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            if (!HasActiveRenderers || _maskPass == null || _compositePass == null)
            {
                return;
            }

            if (!_maskPass.HasMaterial || !_compositePass.HasMaterial)
            {
                return;
            }

            renderer.EnqueuePass(_maskPass);
            renderer.EnqueuePass(_compositePass);
        }

        protected override void Dispose(bool disposing)
        {
            _maskPass?.Dispose();
            _maskPass = null;

            _compositePass?.Dispose();
            _compositePass = null;

            CoreUtils.Destroy(_runtimeMaskMaterial);
            CoreUtils.Destroy(_runtimeOutlineMaterial);
            _runtimeMaskMaterial = null;
            _runtimeOutlineMaterial = null;
        }

        private static Material ResolveMaterial(Material assignedMaterial, string shaderName, ref Material runtimeMaterial)
        {
            if (assignedMaterial != null)
            {
                return assignedMaterial;
            }

            if (runtimeMaterial != null)
            {
                return runtimeMaterial;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                return null;
            }

            runtimeMaterial = CoreUtils.CreateEngineMaterial(shader);
            runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
            return runtimeMaterial;
        }

        public readonly struct RendererEntry
        {
            public readonly Renderer Renderer;
            public readonly int SubMeshCount;

            public RendererEntry(Renderer renderer, int subMeshCount)
            {
                Renderer = renderer;
                SubMeshCount = subMeshCount;
            }
        }

        private sealed class MaskPass : ScriptableRenderPass
        {
            private static readonly int MaskTextureId = Shader.PropertyToID(MaskTextureName);
            private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("WOM Hover Outline Mask");
            private readonly Material _material;
            private RTHandle _maskHandle;
            private int _maskWidth;
            private int _maskHeight;

            public bool HasMaterial
            {
                get { return _material != null; }
            }

            public RTHandle MaskHandle
            {
                get { return _maskHandle; }
            }

            public int MaskWidth
            {
                get { return _maskWidth; }
            }

            public int MaskHeight
            {
                get { return _maskHeight; }
            }

            public int MaskTextureWidth
            {
                get
                {
                    if (_maskHandle != null && _maskHandle.rt != null)
                    {
                        return _maskHandle.rt.width;
                    }

                    return _maskWidth;
                }
            }

            public int MaskTextureHeight
            {
                get
                {
                    if (_maskHandle != null && _maskHandle.rt != null)
                    {
                        return _maskHandle.rt.height;
                    }

                    return _maskHeight;
                }
            }

            public Vector2 MaskUvScale
            {
                get
                {
                    if (_maskHandle != null && _maskHandle.useScaling)
                    {
                        Vector4 scale = _maskHandle.rtHandleProperties.rtHandleScale;
                        return new Vector2(scale.x, scale.y);
                    }

                    return Vector2.one;
                }
            }

            public MaskPass(Material material)
            {
                _material = material;
            }

#if URP_COMPATIBILITY_MODE
#pragma warning disable 618, 672
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                RenderTextureDescriptor descriptor = cameraTextureDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                descriptor.colorFormat = RenderTextureFormat.R8;

                _maskWidth = Mathf.Max(1, descriptor.width);
                _maskHeight = Mathf.Max(1, descriptor.height);

                RenderingUtils.ReAllocateIfNeeded(
                    ref _maskHandle,
                    descriptor,
                    FilterMode.Point,
                    TextureWrapMode.Clamp,
                    name: MaskTextureName
                );
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_material == null || _maskHandle == null || !HasActiveRenderers)
                {
                    return;
                }

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, _profilingSampler))
                {
                    CoreUtils.SetRenderTarget(cmd, _maskHandle, ClearFlag.Color, Color.clear);

                    List<RendererEntry> entries = GetActiveRenderers();
                    for (int i = 0; i < entries.Count; i++)
                    {
                        RendererEntry entry = entries[i];
                        Renderer renderer = entry.Renderer;
                        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                        {
                            continue;
                        }

                        int subMeshCount = Mathf.Max(1, entry.SubMeshCount);
                        for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                        {
                            cmd.DrawRenderer(renderer, _material, subMeshIndex, 0);
                        }
                    }

                    cmd.SetGlobalTexture(MaskTextureId, _maskHandle.nameID);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
#pragma warning restore 618, 672
#endif

            public void Dispose()
            {
                _maskHandle?.Release();
                _maskHandle = null;
            }
        }

        private sealed class CompositePass : ScriptableRenderPass
        {
            private static readonly int MaskTextureId = Shader.PropertyToID(MaskTextureName);
            private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
            private static readonly int OutlineWidthPixelsId = Shader.PropertyToID("_OutlineWidthPixels");
            private static readonly int OutlineTexelSizeId = Shader.PropertyToID("_OutlineTexelSize");
            private static readonly int OutlineMaskUvScaleId = Shader.PropertyToID("_OutlineMaskUvScale");

            private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("WOM Hover Outline Composite");
            private readonly Material _material;
            private readonly MaskPass _maskPass;

            public bool HasMaterial
            {
                get { return _material != null; }
            }

            public CompositePass(Material material, MaskPass maskPass)
            {
                _material = material;
                _maskPass = maskPass;
            }

#if URP_COMPATIBILITY_MODE
#pragma warning disable 618, 672
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_material == null || _maskPass == null || _maskPass.MaskHandle == null)
                {
                    return;
                }

                if (!HasActiveRenderers)
                {
                    return;
                }

                RTHandle colorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, _profilingSampler))
                {
                    float width = Mathf.Clamp(ActiveOutlineWidthPixels, 1f, MaxOutlineRadiusPixels);
                    int maskWidth = Mathf.Max(1, _maskPass.MaskTextureWidth);
                    int maskHeight = Mathf.Max(1, _maskPass.MaskTextureHeight);
                    Vector2 maskUvScale = _maskPass.MaskUvScale;

                    _material.SetTexture(MaskTextureId, _maskPass.MaskHandle);
                    _material.SetColor(OutlineColorId, ActiveOutlineColor);
                    _material.SetFloat(OutlineWidthPixelsId, width);
                    _material.SetVector(
                        OutlineTexelSizeId,
                        new Vector4(1f / maskWidth, 1f / maskHeight, maskWidth, maskHeight)
                    );
                    _material.SetVector(OutlineMaskUvScaleId, new Vector4(maskUvScale.x, maskUvScale.y, 0f, 0f));

                    CoreUtils.SetRenderTarget(
                        cmd,
                        colorTarget,
                        RenderBufferLoadAction.Load,
                        RenderBufferStoreAction.Store,
                        ClearFlag.None,
                        Color.clear
                    );
                    Blitter.BlitTexture(cmd, new Vector4(1f, 1f, 0f, 0f), _material, 0);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
#pragma warning restore 618, 672
#endif

            public void Dispose()
            {
            }
        }
    }
}
