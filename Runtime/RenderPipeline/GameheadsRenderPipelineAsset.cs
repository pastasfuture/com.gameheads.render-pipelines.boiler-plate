using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Gameheads.RenderPipelines.BoilerPlate.Runtime
{
    public partial class GameheadsRenderPipelineAsset : RenderPipelineAsset
    {
        GameheadsRenderPipelineAsset()
        {
        }

        protected override UnityEngine.Rendering.RenderPipeline CreatePipeline()
        {
            GameheadsRenderPipeline pipeline = null;

            try
            {
                pipeline = new GameheadsRenderPipeline(this);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }

            return pipeline;
        }

        protected override void OnValidate()
        {
            //Do not reconstruct the pipeline if we modify other assets.
            //OnValidate is called once at first selection of the asset.
            if (GraphicsSettings.renderPipelineAsset == this)
                base.OnValidate();
        }

    #if UNITY_EDITOR
        private Shader _defaultShader = null;
        public override Shader defaultShader
        {
            get
            {
                if (_defaultShader != null) { return _defaultShader; }
                _defaultShader = Shader.Find("Gameheads/Opaque");
                Debug.Assert(_defaultShader, "Error: GameheadsRenderPipelineAsset: Failed to find default shader at path: Gameheads/Opaque");
                return _defaultShader;
            }
        }

        public override Material defaultMaterial
        {
            get { return renderPipelineResources?.materials.defaultOpaqueMat; }
        }
    #endif

        [SerializeField]
        public GameheadsRenderPipelineResources renderPipelineResources;

        [SerializeField]
        public int targetRasterizationResolutionWidth = 320;
        public int targetRasterizationResolutionHeight = 240;

        public bool isSRPBatcherEnabled = false;
    }

#if UNITY_EDITOR
    [ExecuteInEditMode]
    public static class GameheadsRenderPipelineAssetFactory
    {
        static readonly string s_DefaultPath = "Assets/GameheadsRenderPipelineAsset.asset";

        [UnityEditor.MenuItem("Gameheads/Create Gameheads Render Pipeline Asset")]
        public static void CreateGameheadsRenderPipelineAsset()
        {
            var newAsset = ScriptableObject.CreateInstance<GameheadsRenderPipelineAsset>();
            ResourceReloader.ReloadAllNullIn(newAsset, GameheadsStringConstants.s_PackagePath);
            UnityEditor.AssetDatabase.CreateAsset(newAsset, s_DefaultPath);
        }
    }
#endif
}
