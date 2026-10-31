#if UNITY_EDITOR
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    public sealed class ImpactDecalShaderVariantStripper : IShaderVariantStripper
    {
        private const string DecalShaderName = "Shader Graphs/Decal";
        private const string ImpactGlowDecalShaderName = "Shader Graphs/ImpactGlowDecal";
        private const string DBufferProjectorPass = "DBufferProjector";
        private const string ForwardEmissiveProjectorPass = "DecalProjectorForwardEmissive";

        public bool active => true;

        public bool CanRemoveVariant(Shader shader, ShaderSnippetData shaderVariant, ShaderCompilerData shaderCompilerData)
        {
            if (shader == null || (shader.name != DecalShaderName && shader.name != ImpactGlowDecalShaderName))
                return true;

            return shaderVariant.passName != DBufferProjectorPass && shaderVariant.passName != ForwardEmissiveProjectorPass;
        }
    }
}
#endif
