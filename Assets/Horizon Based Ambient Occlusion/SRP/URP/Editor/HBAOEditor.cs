using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HorizonBasedAmbientOcclusion.Universal
{
    [VolumeComponentEditor(typeof(HBAO))]
    public class HBAOEditor : VolumeComponentEditor
    {
        private HBAO m_HBAO;
        private Texture2D m_HBAOTex;
        private GUIStyle m_SettingsGroupStyle;
        private GUIStyle m_TitleLabelStyle;
        private int m_SelectedPreset;
        private PropertyFetcher<HBAO> m_PropertyFetcher;

        // settings group <setting, property reference>
        private Dictionary<HBAO.SettingsGroup, List<MemberInfo>> m_GroupFields = new Dictionary<HBAO.SettingsGroup, List<MemberInfo>>();
        private readonly Dictionary<int, HBAO.Preset> m_Presets = new Dictionary<int, HBAO.Preset>()
        {
            { 0, HBAO.Preset.Normal },
            { 1, HBAO.Preset.FastPerformance },
            { 2, HBAO.Preset.FastestPerformance },
            { 3, HBAO.Preset.Custom },
            { 4, HBAO.Preset.HighQuality },
            { 5, HBAO.Preset.HighestQuality }
        };

#if UNITY_2021_2_OR_NEWER
        public override bool hasAdditionalProperties => false;
#else
        public override bool hasAdvancedMode => false;
#endif

        public override void OnEnable()
        {
            base.OnEnable();

            m_HBAO = (HBAO)target;
            m_HBAOTex = Resources.Load<Texture2D>("hbao_urp");

            //var o = new PropertyFetcher<HBAO>(serializedObject);
            m_PropertyFetcher = new PropertyFetcher<HBAO>(serializedObject);


            var settings = m_HBAO.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(t => t.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                            .Where(t => (t.IsPublic && t.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length == 0) ||
                                        (t.GetCustomAttributes(typeof(SerializeField), false).Length > 0))
                            .Where(t => t.GetCustomAttributes(typeof(HideInInspector), false).Length == 0)
                            .Where(t => t.GetCustomAttributes(typeof(HBAO.SettingsGroup), false).Any());
            foreach (var setting in settings)
            {
                //Debug.Log("setting name: " + setting.Name);

                foreach (var attr in setting.GetCustomAttributes(typeof(HBAO.SettingsGroup)) as IEnumerable<HBAO.SettingsGroup>)
                {
                    if (!m_GroupFields.ContainsKey(attr))
                        m_GroupFields[attr] = new List<MemberInfo>();

                    m_GroupFields[attr].Add(setting);
                }
            }

            m_SelectedPreset = m_Presets.Values.ToList().IndexOf(m_HBAO.GetCurrentPreset());
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SetStyles();

            EditorGUILayout.BeginVertical();
            {
                // header
                GUILayout.Space(10.0f);
                GUILayout.Label(m_HBAOTex, m_TitleLabelStyle, GUILayout.ExpandWidth(true));

                //if (m_HBAO.GetComponents<MonoBehaviour>()[0] != m_HBAO)
                //{
                //GUILayout.Space(6.0f);
                //EditorGUILayout.HelpBox("This Post FX should be one of the first in your effect stack", MessageType.Info);
                //}

                Event e = Event.current;

                // settings groups
                foreach (var group in m_GroupFields)
                {
                    GUILayout.Space(6.0f);
                    Rect rect = GUILayoutUtility.GetRect(16f, 22f, m_SettingsGroupStyle);
                    GUI.Box(rect, ObjectNames.NicifyVariableName(group.Key.GetType().Name), m_SettingsGroupStyle);
                    if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
                    {
                        group.Key.isExpanded = !group.Key.isExpanded;
                        e.Use();
                    }

                    if (!group.Key.isExpanded)
                        continue;

                    // presets is a special case
                    if (group.Key.GetType() == typeof(HBAO.Presets))
                    {
                        GUILayout.Space(6.0f);
                        m_SelectedPreset = GUILayout.SelectionGrid(m_SelectedPreset, m_Presets.Values.Select(x => ObjectNames.NicifyVariableName(x.ToString())).ToArray(), 3);
                        GUILayout.Space(6.0f);
                        if (GUILayout.Button("Apply Preset"))
                        {
                            Undo.RecordObject(target, "Apply Preset");
                            m_HBAO.ApplyPreset(m_Presets[m_SelectedPreset]);
                            EditorUtility.SetDirty(target);
                            /*if (!EditorApplication.isPlaying)
                            {
                                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                            }*/
                        }

                        continue;
                    }

                    foreach (var field in group.Value)
                    {
#if !URP_10_0_0_OR_NEWER
                        // warn about URP10+ required when mode is LitAO
                        if (group.Key.GetType() == typeof(HBAO.GeneralSettings) && field.Name == "quality")
                        {
                            if (m_HBAO.mode.overrideState && m_HBAO.mode.value == HBAO.Mode.LitAO)
                            {
                                GUILayout.Space(6.0f);
                                EditorGUILayout.HelpBox("LitAO mode requires URP 10 or newer!", MessageType.Warning);
                            }
                        }
#endif
                        // hide resolution when deinterleaved HBAO is on
                        if (group.Key.GetType() == typeof(HBAO.GeneralSettings) && field.Name == "resolution")
                        {
                            if (m_HBAO.deinterleaving.overrideState && m_HBAO.GetDeinterleaving() != HBAO.Deinterleaving.Disabled)
                            {
                                continue;
                            }
                        }
                        // warn about deinterleaving not supported with SPSR
                        /*else if (group.Key.GetType() == typeof(HBAO.GeneralSettings) && field.Name == "debugMode")
                        {
                            if (m_HBAO.GetDeinterleaving() != HBAO.Deinterleaving.Disabled &&
                                IsVrRunning() && PlayerSettings.stereoRenderingPath == StereoRenderingPath.SinglePass)
                            {
                                GUILayout.Space(6.0f);
                                EditorGUILayout.HelpBox("Deinterleaving is not supported with Single Pass Stereo Rendering...", MessageType.Warning);
                            }
                        }*/
                        // hide noise type when deinterleaved HBAO is on
                        else if (group.Key.GetType() == typeof(HBAO.GeneralSettings) && field.Name == "noiseType")
                        {
                            if (m_HBAO.deinterleaving.overrideState && m_HBAO.GetDeinterleaving() != HBAO.Deinterleaving.Disabled)
                            {
                                continue;
                            }
                        }
                        // warn about HBAO being disabled by default
                        else if (group.Key.GetType() == typeof(HBAO.AOSettings) && field.Name == "radius")
                        {
                            if (m_HBAO.intensity.overrideState == false)
                            {
                                GUILayout.Space(6.0f);
                                EditorGUILayout.HelpBox("The effect is disabled by default, you need to override intensity value in order to enable HBAO.", MessageType.Warning);
                            }
                        }
                        // hide useMultiBounce setting when mode is LitAO
                        else if (group.Key.GetType() == typeof(HBAO.AOSettings) && field.Name == "useMultiBounce")
                        {
                            if (m_HBAO.mode.overrideState && m_HBAO.mode.value == HBAO.Mode.LitAO)
                            {
                                continue;
                            }
                        }
                        // hide multiBounceInfluence setting when not used or when mode is LitAO
                        else if (group.Key.GetType() == typeof(HBAO.AOSettings) && field.Name == "multiBounceInfluence")
                        {
                            if (!m_HBAO.useMultiBounce.overrideState || !m_HBAO.UseMultiBounce() ||
                                (m_HBAO.mode.overrideState && m_HBAO.mode.value == HBAO.Mode.LitAO))
                            {
                                continue;
                            }
                        }
                        // hide directLightingStrength setting when mode is normal
                        else if (group.Key.GetType() == typeof(HBAO.AOSettings) && field.Name == "directLightingStrength")
                        {
                            if (!m_HBAO.mode.overrideState || m_HBAO.mode.value == HBAO.Mode.Normal)
                            {
                                continue;
                            }
                        }
                        // warn about distance falloff greater than max distance
                        else if (group.Key.GetType() == typeof(HBAO.AOSettings) && field.Name == "perPixelNormals")
                        {
                            if ((m_HBAO.distanceFalloff.overrideState || m_HBAO.maxDistance.overrideState) && m_HBAO.GetAoDistanceFalloff() > m_HBAO.GetAoMaxDistance())
                            {
                                GUILayout.Space(6.0f);
                                EditorGUILayout.HelpBox("Distance Falloff shoudn't be superior to Max Distance", MessageType.Warning);
                            }
                        }
                        // warn about distance falloff greater than max distance
                        /*else if (group.Key.GetType() == typeof(HBAO.AOSettings) && field.Name == "baseColor")
                        {
                            if (m_HBAO.perPixelNormals.overrideState && m_HBAO.GetAoPerPixelNormals() == HBAO.PerPixelNormals.Camera)
                            {
                                GUILayout.Space(6.0f);
                                EditorGUILayout.HelpBox("Currently Universal Render Pipeline does not generate camera normals, awaiting support...", MessageType.Warning);
                            }
                        }*/
                        else if (group.Key.GetType() == typeof(HBAO.TemporalFilterSettings) && field.Name == "temporalFilterEnabled")
                        {
                            GUILayout.Space(6.0f);
                            EditorGUILayout.HelpBox("Currently Universal Render Pipeline does not generate motion vectors, awaiting support...", MessageType.Warning);

                            if (m_HBAO.IsTemporalFilterEnabled())
                                m_HBAO.EnableTemporalFilter(false);
                        }

                        var parameter = Unpack(m_PropertyFetcher.Find(field.Name));
                        var displayName = parameter.displayName;
                        var hasDisplayName = field.GetCustomAttributes(typeof(HBAO.ParameterDisplayName)).Any();
                        if (hasDisplayName)
                        {
                            var displayNameAttribute = field.GetCustomAttributes(typeof(HBAO.ParameterDisplayName)).First() as HBAO.ParameterDisplayName;
                            displayName = displayNameAttribute.name;
                        }

                        PropertyField(parameter, new GUIContent(displayName));
                    }

                    GUILayout.Space(6.0f);
                }
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void SetStyles()
        {
            // set banner label style
            m_TitleLabelStyle = new GUIStyle(GUI.skin.label);
            m_TitleLabelStyle.alignment = TextAnchor.MiddleCenter;
            m_TitleLabelStyle.contentOffset = new Vector2(0f, 0f);

            // get shuriken module title style
            GUIStyle skurikenModuleTitleStyle = "ShurikenModuleTitle";

            // clone it as to not interfere with the original, and adjust it
            m_SettingsGroupStyle = new GUIStyle(skurikenModuleTitleStyle);
            m_SettingsGroupStyle.font = (new GUIStyle("Label")).font;
            m_SettingsGroupStyle.fontStyle = FontStyle.Bold;
            m_SettingsGroupStyle.border = new RectOffset(15, 7, 4, 4);
            m_SettingsGroupStyle.fixedHeight = 22;
            m_SettingsGroupStyle.contentOffset = new Vector2(10f, -2f);
        }

        List<UnityEngine.XR.XRDisplaySubsystemDescriptor> displaysDescs = new List<UnityEngine.XR.XRDisplaySubsystemDescriptor>();
        List<UnityEngine.XR.XRDisplaySubsystem> displays = new List<UnityEngine.XR.XRDisplaySubsystem>();

        private bool IsVrRunning()
        {
            bool vrIsRunning = false;
            displays.Clear();
            SubsystemManager.GetInstances(displays);
            foreach (var displaySubsystem in displays)
            {
                if (displaySubsystem.running)
                {
                    vrIsRunning = true;
                    break;
                }
            }

            return vrIsRunning;
        }

        [VolumeParameterDrawer(typeof(HBAO.MinMaxFloatParameter))]
        public class MaxFloatParameterDrawer : VolumeParameterDrawer
        {
            public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
            {
                if (parameter.value.propertyType == SerializedPropertyType.Vector2)
                {
                    var o = parameter.GetObjectRef<HBAO.MinMaxFloatParameter>();
                    var range = o.value;
                    float x = range.x;
                    float y = range.y;

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.MinMaxSlider(title, ref x, ref y, o.min, o.max);
                    if (EditorGUI.EndChangeCheck())
                    {
                        range.x = x;
                        range.y = y;
                        o.SetValue(new HBAO.MinMaxFloatParameter(range, o.min, o.max));
                    }
                    return true;
                }
                else
                {
                    EditorGUILayout.LabelField(title, "Use only with Vector2");
                    return false;
                }
            }
        }
    }
}
