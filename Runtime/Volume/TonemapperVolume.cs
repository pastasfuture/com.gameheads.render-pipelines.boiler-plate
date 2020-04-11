using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gameheads.RenderPipelines.BoilerPlate.Runtime
{
    [Serializable, VolumeComponentMenu("Gameheads/TonemapperVolume")]
    public class TonemapperVolume : VolumeComponent
    {
        public ClampedFloatParameter saturation = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        static TonemapperVolume s_Default = null;
        public static TonemapperVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<TonemapperVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}