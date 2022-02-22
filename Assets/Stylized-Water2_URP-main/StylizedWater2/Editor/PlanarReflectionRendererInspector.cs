using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace StylizedWater2
{
    [CustomEditor(typeof(PlanarReflectionRenderer))]
    public class PlanarReflectionRendererInspector : Editor
    {
        private PlanarReflectionRenderer renderer;
        
        //Rendering
        private SerializedProperty cullingMask;
        private SerializedProperty rendererIndex;
        private SerializedProperty offset;
        private SerializedProperty includeSkybox;
        
        //Quality
        private SerializedProperty renderRange;
        private SerializedProperty renderScale;
        private SerializedProperty maximumLODLevel;
        
        private SerializedProperty waterObjects;

        private Bounds curBounds;

        private void OnEnable()
        {
#if URP
            PipelineUtilities.RefreshRendererList();
            
            renderer = (PlanarReflectionRenderer)target;

            cullingMask = serializedObject.FindProperty("cullingMask");
            rendererIndex = serializedObject.FindProperty("rendererIndex");
            offset = serializedObject.FindProperty("offset");
            includeSkybox = serializedObject.FindProperty("includeSkybox");
            renderRange = serializedObject.FindProperty("renderRange");
            renderScale = serializedObject.FindProperty("renderScale");
            maximumLODLevel = serializedObject.FindProperty("maximumLODLevel");
            waterObjects = serializedObject.FindProperty("waterObjects");
            
            if (renderer.waterObjects.Count == 0 && WaterObject.Instances.Count == 1)
            {
                renderer.waterObjects.Add(WaterObject.Instances[0]);
                renderer.RecalculateBounds();
                renderer.EnableMaterialReflectionSampling();
                
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            curBounds = renderer.CalculateBounds();
#endif
        }

        public override void OnInspectorGUI()
        {
#if !URP
            UI.DrawNotification("The Universal Render Pipeline package v" + AssetInfo.MIN_URP_VERSION + " or newer is not installed", MessageType.Error);
#else
            UI.DrawNotification(UnityEngine.Rendering.XRGraphics.enabled, "Not supported with VR rendering", MessageType.Error);
            
            UI.DrawNotification(PlanarReflectionRenderer.AllowReflections == false, "Reflections have been globally disabled by an external script", MessageType.Warning);
            
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Status: " + (renderer.isRendering ? "Rendering (water in view)" : "Not rendering (no water in view)"), EditorStyles.miniLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(cullingMask);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUI.BeginChangeCheck();
            UI.DrawRendererProperty(rendererIndex);
            if (EditorGUI.EndChangeCheck())
            {
                renderer.SetRendererIndex(rendererIndex.intValue);
            }
            EditorGUILayout.PropertyField(offset);
            EditorGUILayout.PropertyField(includeSkybox);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Quality", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(renderRange);
            EditorGUILayout.PropertyField(renderScale);
            EditorGUILayout.PropertyField(maximumLODLevel);
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Affected water objects", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(waterObjects);
            if (EditorGUI.EndChangeCheck())
            {
                curBounds = renderer.CalculateBounds();
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if(GUILayout.Button(new GUIContent("Auto-find", "Assigns all active water objects currently in the scene"), EditorStyles.miniButton))
                {
                    renderer.waterObjects = new List<WaterObject>(WaterObject.Instances);
 
                    renderer.RecalculateBounds();
                    curBounds = renderer.bounds;
                    renderer.EnableMaterialReflectionSampling();
                    
                    EditorUtility.SetDirty(target);
                }
                if(GUILayout.Button("Clear", EditorStyles.miniButton))
                {
                    renderer.ToggleMaterialReflectionSampling(false);
                    renderer.waterObjects.Clear();
                    renderer.RecalculateBounds();
                    
                    EditorUtility.SetDirty(target);
                }
            }
            
            if (renderer.waterObjects != null)
            {
                UI.DrawNotification(renderer.waterObjects.Count == 0, "Assign at least one Water Object", MessageType.Info);
                
                if (renderer.waterObjects.Count > 0)
                {
                    UI.DrawNotification(curBounds.size != renderer.bounds.size || curBounds.center != renderer.bounds.center, "Water objects have changed or moved, bounds needs to be recalculated", "Recalculate",() => RecalculateBounds(), MessageType.Error);
                }
            }

#endif
            
            UI.DrawFooter();
        }

        private void RecalculateBounds()
        {
#if URP
            renderer.RecalculateBounds();
            curBounds = renderer.bounds;
            EditorUtility.SetDirty(target);
#endif
        }
    }
}
