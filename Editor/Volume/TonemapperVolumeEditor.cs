using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Gameheads.RenderPipelines.BoilerPlate.Runtime;

namespace Gameheads.RenderPipelines.BoilerPlate.Editor
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(TonemapperVolume))]
    public class TonemapperVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Saturation;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<TonemapperVolume>(serializedObject);
            m_Saturation = Unpack(o.Find(x => x.saturation));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Saturation, EditorGUIUtility.TrTextContent("Saturation", "Controls global saturation of image. 0.0 applies no saturation modification (raw image). -1 is fully desaturated. 1 is max saturation."));
        }
    }
}