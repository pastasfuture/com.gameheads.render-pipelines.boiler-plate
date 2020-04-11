using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gameheads.RenderPipelines.BoilerPlate.Runtime
{
    public class GameheadsRenderPipelineResources : ScriptableObject
    {
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Runtime/PostProcessing/Shaders/PostProcessing.shader")]
            public Shader postProcessingPS;
        }

        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
            [Reload("Runtime/RenderPipelineResources/Texture/BayerL4x4.png")]
            public Texture2D framebufferDitherTex;
        }

        [Serializable, ReloadGroup]
        public sealed class MaterialResources
        {
            [Reload("Runtime/RenderPipelineResources/Material/DefaultOpaqueMat.mat")]
            public Material defaultOpaqueMat;
        }

        public ShaderResources shaders;
        public TextureResources textures;
        public MaterialResources materials;
    }

#if UNITY_EDITOR
    [ExecuteInEditMode]
    static class GameheadsRenderPipelineResourcesFactory
    {
        static readonly string s_DefaultPath = "Assets/GameheadsRenderPipelineResources.asset";

        [UnityEditor.MenuItem("Gameheads/Create Gameheads Render Pipeline Resources")]
        static void CreateGameheadsRenderPipelineAsset()
        {
            var newAsset = ScriptableObject.CreateInstance<GameheadsRenderPipelineResources>();
            ResourceReloader.ReloadAllNullIn(newAsset, GameheadsStringConstants.s_PackagePath);
            UnityEditor.AssetDatabase.CreateAsset(newAsset, s_DefaultPath);
        }
    }
#endif
}
