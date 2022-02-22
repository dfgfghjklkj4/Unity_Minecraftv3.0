#if GAIA_PRESENT && UNITY_EDITOR

using HorizonBasedAmbientOcclusion;
using UnityEditor;
using UnityEngine;

namespace Gaia.GX.MichaelJimenez
{
    public class HBAOGaiaExtension : MonoBehaviour
    {
#region Generic informational methods

        /// <summary>
        /// Returns the publisher name if provided. 
        /// This will override the publisher name in the namespace ie Gaia.GX.PublisherName
        /// </summary>
        /// <returns>Publisher name</returns>
        public static string GetPublisherName()
        {
            return "Michael Jimenez";
        }

        /// <summary>
        /// Returns the package name if provided
        /// This will override the package name in the class name ie public class PackageName.
        /// </summary>
        /// <returns>Package name</returns>
        public static string GetPackageName()
        {
            return "Horizon Based Ambient Occlusion";
        }

#endregion

#region Methods exposed by Gaia as buttons must be prefixed with GX_

        public static void GX_About()
        {
            EditorUtility.DisplayDialog("About Horizon Based Ambient Occlusion ", "HBAO is a post processing image effect to use in order to add realism to your scenes. It helps accentuating small surface details and reproduce light attenuation due to occlusion.\n\nNote: This Post FX should be the first in your effect stack.", "OK");
        }

        public static void GX_Presets_FastestPerformance()
        {
            HBAO hbao = StackPostFXOnTop();
            if (hbao != null)
            {
                hbao.ApplyPreset(HBAO.Preset.FastestPerformance);
                MarkDirty(hbao);
            }
        }

        public static void GX_Presets_FastPerformance()
        {
            HBAO hbao = StackPostFXOnTop();
            if (hbao != null)
            {
                hbao.ApplyPreset(HBAO.Preset.FastPerformance);
                MarkDirty(hbao);
            }
        }

        public static void GX_Presets_Normal()
        {
            HBAO hbao = StackPostFXOnTop();
            if (hbao != null)
            {
                hbao.ApplyPreset(HBAO.Preset.Normal);
                MarkDirty(hbao);
            }
        }

        public static void GX_Presets_HighQuality()
        {
            HBAO hbao = StackPostFXOnTop();
            if (hbao != null)
            {
                hbao.ApplyPreset(HBAO.Preset.HighQuality);
                MarkDirty(hbao);
            }
        }


        public static void GX_Presets_HighestQuality()
        {
            HBAO hbao = StackPostFXOnTop();
            if (hbao != null)
            {
                hbao.ApplyPreset(HBAO.Preset.HighestQuality);
                MarkDirty(hbao);
            }
        }

#endregion

#region Helper methods

        private static HBAO StackPostFXOnTop()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = FindObjectOfType<Camera>();
            }
            if (camera == null)
            {
                EditorUtility.DisplayDialog("OOPS!", "Could not find camera to add camera effects to. Please add a camera to your scene.", "OK");
                return null;
            }

            // add HBAO to camera
            HBAO hbao = camera.GetComponent<HBAO>();
            if (hbao != null)
            {
                DestroyImmediate(hbao);
            }
            hbao = camera.gameObject.AddComponent<HBAO>();

            // stack it on top
            while (camera.GetComponents<MonoBehaviour>()[0] != hbao)
            {
                UnityEditorInternal.ComponentUtility.MoveComponentUp(hbao);
            }

            return hbao;
        }

        private static void MarkDirty(HBAO hbao)
        {
            EditorUtility.SetDirty(hbao);
            if (!EditorApplication.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
        }

#endregion
    }
}

#endif
