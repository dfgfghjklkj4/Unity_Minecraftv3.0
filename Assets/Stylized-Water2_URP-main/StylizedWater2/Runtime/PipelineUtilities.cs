//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if URP
using UnityEngine.Rendering.Universal;

#if UNITY_2021_2_OR_NEWER
using ForwardRendererData = UnityEngine.Rendering.Universal.UniversalRendererData;
#endif
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StylizedWater2
{
	//Stay awesome Unity, locking everything behind internal UI code won't stop us, just makes it convoluted
    public static class PipelineUtilities
    {
        private const string renderDataListFieldName = "m_RendererDataList";
        
#if URP
        /// <summary>
        /// Retrieves a ForwardRenderer asset in the project, based on GUID
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static ForwardRendererData GetRenderer(string GUID)
        {
#if UNITY_EDITOR
            string assetPath = AssetDatabase.GUIDToAssetPath(GUID);
            ForwardRendererData renderer = (ForwardRendererData)AssetDatabase.LoadAssetAtPath(assetPath, typeof(ForwardRendererData));

            return renderer;
#else
            Debug.LogError("PipelineUtilities.GetRenderer() cannot be called in a build, it requires AssetDatabase. References to renderers should be saved beforehand!");
            return null;
#endif
        }

        public static void RefreshRendererList()
        {
            if (UniversalRenderPipeline.asset == null)
            {
                Debug.LogError("No pipeline is active, do not display UI that uses this function if it isn't!");
            }
            
            ScriptableRendererData[] m_rendererDataList = (ScriptableRendererData[]) typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UniversalRenderPipeline.asset);
              
            //Display names
            _rendererDisplayList = new GUIContent[m_rendererDataList.Length+1];

            int defaultIndex = GetDefaultRendererIndex(UniversalRenderPipeline.asset);
            _rendererDisplayList[0] = new GUIContent($"Default ({(m_rendererDataList[defaultIndex].name)})");
                    
            for (int i = 1; i < _rendererDisplayList.Length; i++)
            {
                _rendererDisplayList[i] = new GUIContent($"{(i - 1).ToString()}: {(m_rendererDataList[i-1]).name}");
            }
            
            //Indices
            _rendererIndexList = new int[m_rendererDataList.Length+1];
            for (int i = 0; i < _rendererIndexList.Length; i++)
            {
                _rendererIndexList[i] = i-1;
            }
        }

        private static GUIContent[] _rendererDisplayList;
        public static GUIContent[] rendererDisplayList
        {
            get
            {
                if (_rendererDisplayList == null) RefreshRendererList();
                return _rendererDisplayList;
            }
            set
            {
                _rendererDisplayList = value;
            }
        }

        private static int[] _rendererIndexList;
        public static int[] rendererIndexList
        {
            get
            {
                if (_rendererIndexList == null) RefreshRendererList();
                return _rendererIndexList;
            }
            set
            {
                _rendererIndexList = value;
            }
        }

        /// <summary>
        /// Given a renderer index, validates if there is actually a renderer at the index. Otherwise returns the index of the default renderer.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int ValidateRenderer(int index)
        {
            if (UniversalRenderPipeline.asset)
            {
                int defaultRendererIndex = GetDefaultRendererIndex(UniversalRenderPipeline.asset);
                ScriptableRendererData[] m_rendererDataList = (ScriptableRendererData[]) typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UniversalRenderPipeline.asset);
                
                //-1 is used to indicate the default renderer
                if (index == -1) index = defaultRendererIndex;

                //Check if any renderer exists at the current index
                if (!(index < m_rendererDataList.Length && m_rendererDataList[index] != null))
                {
                    Debug.LogWarning($"Renderer at <b>index {index.ToString()}</b> is missing, falling back to Default Renderer. <b>{m_rendererDataList[defaultRendererIndex].name}</b>", UniversalRenderPipeline.asset);
                    return defaultRendererIndex;
                }
                else
                {
                    //Valid
                    return index;
                }
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
                return 0;
            }
        }
        
        /// <summary>
        /// Checks if a ForwardRenderer has been assigned to the pipeline asset
        /// </summary>
        /// <param name="renderer"></param>
        public static bool IsRendererAdded(ScriptableRendererData renderer)
        {
            if (renderer == null)
            {
                Debug.LogError("Pass is null");
                return false;
            }

            if (UniversalRenderPipeline.asset)
            {
                BindingFlags bindings =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

                ScriptableRendererData[] m_rendererDataList =
                    (ScriptableRendererData[]) typeof(UniversalRenderPipelineAsset)
                        .GetField(renderDataListFieldName, bindings).GetValue(UniversalRenderPipeline.asset);
                bool isPresent = false;

                for (int i = 0; i < m_rendererDataList.Length; i++)
                {
                    if (m_rendererDataList[i] == renderer) isPresent = true;
                }

                return isPresent;
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
                return false;
            }
        }
        
        /// <summary>
        /// Adds a ForwardRenderer to the pipeline asset in use
        /// </summary>
        /// <param name="renderer"></param>
        private static void AddRendererToPipeline(ScriptableRendererData renderer)
        {
            if (renderer == null) return;

            if (UniversalRenderPipeline.asset)
            {
                BindingFlags bindings = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

                ScriptableRendererData[] m_rendererDataList = (ScriptableRendererData[]) typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, bindings).GetValue(UniversalRenderPipeline.asset);
                List<ScriptableRendererData> rendererDataList = new List<ScriptableRendererData>();

                for (int i = 0; i < m_rendererDataList.Length; i++)
                {
                    rendererDataList.Add(m_rendererDataList[i]);
                }

                rendererDataList.Add(renderer);

                typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, bindings).SetValue(UniversalRenderPipeline.asset, rendererDataList.ToArray());

#if UNITY_EDITOR
                EditorUtility.SetDirty(UniversalRenderPipeline.asset);
#endif
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
            }
        }

        private static int GetDefaultRendererIndex(UniversalRenderPipelineAsset asset)
        {
            return (int)typeof(UniversalRenderPipelineAsset).GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asset);;
        }

        /// <summary>
        /// Gets the renderer from the current pipeline asset that's marked as default
        /// </summary>
        /// <returns></returns>
        public static ScriptableRendererData GetDefaultRenderer()
        {
            if (UniversalRenderPipeline.asset)
            {
                ScriptableRendererData[] rendererDataList = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset)
                        .GetField(renderDataListFieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(UniversalRenderPipeline.asset);
                int defaultRendererIndex = GetDefaultRendererIndex(UniversalRenderPipeline.asset);

                return rendererDataList[defaultRendererIndex];
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
                return null;
            }
        }

        public static ScriptableRendererFeature GetRenderFeature<T>()
        {
            ScriptableRendererData forwardRenderer = GetDefaultRenderer();
            
            foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
            {
                if (feature.GetType() == typeof(T)) return feature;
            }

            return null;
        }

        /// <summary>
        /// Checks if a ScriptableRendererFeature is added to the default renderer
        /// </summary>
        /// <param name="addIfMissing"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool RenderFeatureAdded<T>(bool addIfMissing = false)
        {
            ScriptableRendererData forwardRenderer = GetDefaultRenderer();
            bool isPresent = false;

            foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
            {
                if (feature.GetType() == typeof(T)) isPresent = true;
            }
            
            if(!isPresent && addIfMissing) AddRenderFeature<T>(forwardRenderer);
            
            return isPresent;
        }

        /// <summary>
        /// Adds a ScriptableRendererFeature to the renderer (default is none is supplied)
        /// </summary>
        /// <param name="forwardRenderer"></param>
        /// <typeparam name="T"></typeparam>
        public static void AddRenderFeature<T>(ScriptableRendererData forwardRenderer = null)
        {
            if (forwardRenderer == null) forwardRenderer = GetDefaultRenderer();
            
            ScriptableRendererFeature feature = (ScriptableRendererFeature)ScriptableRendererFeature.CreateInstance(typeof(T).ToString());
            feature.name = typeof(T).ToString();
            
            //Add component https://github.com/Unity-Technologies/Graphics/blob/d0473769091ff202422ad13b7b764c7b6a7ef0be/com.unity.render-pipelines.universal/Editor/ScriptableRendererDataEditor.cs#L180
#if UNITY_EDITOR
            AssetDatabase.AddObjectToAsset(feature, forwardRenderer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out var guid, out long localId);
#endif

            //Get feature list
            FieldInfo renderFeaturesInfo = typeof(ScriptableRendererData).GetField("m_RendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
            List<ScriptableRendererFeature> m_RendererFeatures = (List<ScriptableRendererFeature>)renderFeaturesInfo.GetValue(forwardRenderer);

            //Modify and set list
            m_RendererFeatures.Add(feature);
            renderFeaturesInfo.SetValue(forwardRenderer, m_RendererFeatures);

            //Onvalidate will call ValidateRendererFeatures and update m_RendererPassMap
            MethodInfo validateInfo = typeof(ScriptableRendererData).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            validateInfo.Invoke(forwardRenderer, null);

#if UNITY_EDITOR
            EditorUtility.SetDirty(forwardRenderer);
            AssetDatabase.SaveAssets();
#endif
            
            Debug.Log("<b>" + feature.name + "</b> was added to the " + forwardRenderer.name + " renderer");
        }

        public static bool IsRenderFeatureEnabled<T>(ScriptableRendererData forwardRenderer = null, bool autoEnable = false)
        {
            if (forwardRenderer == null) forwardRenderer = GetDefaultRenderer();
            
            FieldInfo renderFeaturesInfo = typeof(ScriptableRendererData).GetField("m_RendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
            List<ScriptableRendererFeature> m_RendererFeatures = (List<ScriptableRendererFeature>)renderFeaturesInfo.GetValue(forwardRenderer);

            foreach (ScriptableRendererFeature feature in m_RendererFeatures)
            {
                if (feature.GetType() == typeof(T))
                {
                    if (feature.isActive == false && autoEnable)
                    {
                        feature.SetActive(true);
                        
                        #if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(forwardRenderer);
                        #endif
                    }
                    
                    return feature.isActive;
                }
            }

            //Fallback, if it is not even in the list
            return true;
        }

        public static void ToggleRenderFeature<T>(bool state)
        {
            ScriptableRendererData forwardRenderer = GetDefaultRenderer();
            
            foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
            {
                if (feature.GetType() == typeof(T)) feature.SetActive(state);
            }
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(forwardRenderer);
            #endif
        }

        public static void RemoveRendererFromPipeline(ScriptableRendererData renderer)
        {
            if (renderer == null) return;

            if (UniversalRenderPipeline.asset)
            {
                BindingFlags bindings = BindingFlags.NonPublic | BindingFlags.Instance;

                ScriptableRendererData[] m_rendererDataList =
                    (ScriptableRendererData[]) typeof(UniversalRenderPipelineAsset)
                        .GetField(renderDataListFieldName, bindings).GetValue(UniversalRenderPipeline.asset);
                List<ScriptableRendererData> rendererDataList = new List<ScriptableRendererData>(m_rendererDataList);

                if (rendererDataList.Contains(renderer))
                {
                    rendererDataList.Remove(renderer);
                    
                    typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, bindings)
                        .SetValue(UniversalRenderPipeline.asset, rendererDataList.ToArray());

#if UNITY_EDITOR
                    EditorUtility.SetDirty(UniversalRenderPipeline.asset);
                    AssetDatabase.SaveAssets();
#endif
                }
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
            }
        }

        /// <summary>
        /// Sets the renderer index of the related forward renderer
        /// </summary>
        /// <param name="camData"></param>
        /// <param name="renderer"></param>
        public static void AssignRendererToCamera(UniversalAdditionalCameraData camData, ScriptableRendererData renderer)
        {
            if (UniversalRenderPipeline.asset)
            {
                if (renderer)
                {
                    //list is internal, so perform reflection workaround
                    ScriptableRendererData[] rendererDataList = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UniversalRenderPipeline.asset);

                    for (int i = 0; i < rendererDataList.Length; i++)
                    {
                        if (rendererDataList[i] == renderer) camData.SetRenderer(i);
                    }
                }
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
            }
        }
        
        public static bool TransparentShadowsEnabled()
        {
            if (!UniversalRenderPipeline.asset) return false;

            ForwardRendererData main = (ForwardRendererData)GetDefaultRenderer();

            return main ? main.shadowTransparentReceive : false;
        }
#endif
    }
}
