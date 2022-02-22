//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace StylizedWater2
{
    public class HelpWindow : EditorWindow
    {
        //Window properties
        private static int width = 440;
        private static int height = 520;

        [SerializeField]
        private ShaderConfigurator.FogConfiguration fogConfig;

        private Texture HeaderImg;
        
        [MenuItem("Window/Stylized Water 2/Hub Window", false, 1001)]
        public static void ShowWindow()
        {
            HelpWindow editorWindow = EditorWindow.GetWindow<HelpWindow>(true, "", true);

            //Open somewhat in the center of the screen
            editorWindow.position = new Rect((Screen.currentResolution.width / 2f) - (width * 0.5f), (Screen.currentResolution.height / 2f) - (height * 0.5f), (width * 2), height);

            //Fixed size
            editorWindow.maxSize = new Vector2(width, height);
            editorWindow.minSize = new Vector2(width, height);
            
            //Init
            editorWindow.Init();
            editorWindow.Show();
        }

        [MenuItem("Window/Stylized Water 2/Forum", false, 1002)]
        public static void OpenForum()
        {
            AssetInfo.OpenForumPage();
        }

        private void OnEnable()
        {
            StylizedWaterEditor.DWP2.CheckInstallation();
            ShaderConfigurator.RefreshShaderFilePaths();
            ShaderConfigurator.GetCurrentFogConfiguration();
            fogConfig = ShaderConfigurator.CurrentFogConfiguration;
        }

        public void Init()
        {
            AssetInfo.VersionChecking.CheckForUpdate(false);
            AssetInfo.VersionChecking.CheckUnityVersion();

            HeaderImg = UI.CreateIcon(UI.HeaderImgData);
        }

        private void OnGUI()
        {
            DrawHeader();
  
            DrawInstallation();

            DrawDocumentation();

            DrawSupport();
            
            DrawFooter();
        }

        void DrawHeader()
        {
            Rect rect = EditorGUILayout.GetControlRect();
            rect.x -= 3f;
            rect.y -= 3f;
            rect.height = HeaderImg.height;
            rect.width = HeaderImg.width;
            GUI.DrawTexture(rect, HeaderImg);
            
            GUILayout.Space(-7f);
            EditorGUILayout.LabelField("<size=24><color=\"#ffffff\"><b>" + AssetInfo.ASSET_NAME + "</b></color></size>", UI.Styles.Header);
            GUILayout.Space(15f);
        }

        void DrawInstallation()
        {
            //Testing
            //AssetInfo.compatibleVersion = false;
            //AssetInfo.alphaVersion = true;
            //AssetInfo.IS_UPDATED = false;
            
            Texture2D dot = null;
            if (AssetInfo.compatibleVersion && !AssetInfo.alphaVersion) dot = UI.Styles.SmallGreenDot;
            if (AssetInfo.alphaVersion || !AssetInfo.IS_UPDATED) dot = UI.Styles.SmallOrangeDot;
            if (!AssetInfo.compatibleVersion) dot = UI.Styles.SmallRedDot;
            
            EditorGUILayout.LabelField(new GUIContent(" Installation", dot), UI.Styles.Tab);

            using (new EditorGUILayout.VerticalScope(UI.Styles.Section))
            {
                if (EditorApplication.isCompiling)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(new GUIContent(" Compiling scripts...", EditorGUIUtility.FindTexture("cs Script Icon")), UI.Styles.Header);
                    EditorGUILayout.Space();
                    return;
                }

                Color defaultColor = GUI.contentColor;

                if (AssetInfo.compatibleVersion == false)
                {
                    UI.DrawNotification("This version of Unity is not supported.", MessageType.Error);
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Please upgrade to at least Unity " + AssetInfo.MIN_UNITY_VERSION);
                    return;
                }
                
                //Version
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Version " + AssetInfo.INSTALLED_VERSION);

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.textField))
                    {
                        if (AssetInfo.IS_UPDATED)
                        {
                            GUI.contentColor = Color.green;
                            EditorGUILayout.LabelField("Latest");
                            GUI.contentColor = defaultColor;
                        }
                        else
                        {
                            GUI.contentColor = new Color(1f, 0.65f, 0f);
                            EditorGUILayout.LabelField("Outdated", EditorStyles.boldLabel);
                            GUI.contentColor = defaultColor;
                        }

                        GUILayout.Space(-100f);
                        if (GUILayout.Button("View changelog")) ChangelogWindow.Open();
                    }
                }
                
                UI.DrawNotification(!AssetInfo.IS_UPDATED, "Asset can be updated through the Package Manager", "Open", () => AssetInfo.OpenStorePage());

                //Unity version
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Unity " + AssetInfo.VersionChecking.GetUnityVersion());

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.textField))
                    {
                        if (AssetInfo.compatibleVersion && !AssetInfo.alphaVersion)
                        {
                            GUI.contentColor = Color.green;
                            EditorGUILayout.LabelField("Compatible");
                            GUI.contentColor = defaultColor;
                        }
                        if (AssetInfo.alphaVersion)
                        {
                            GUI.contentColor = new Color(1f, 0.65f, 0f);
                            EditorGUILayout.LabelField("Alpha/beta", EditorStyles.boldLabel);
                            GUI.contentColor = defaultColor;
                        }
                    }
                }
                
                UI.DrawNotification(AssetInfo.alphaVersion, "Only release Unity versions are subject to support and fixes. You may run into issues at own risk.", MessageType.Warning);

                //Mobile only
                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Target graphics API");

                        using (new EditorGUILayout.HorizontalScope(EditorStyles.textField))
                        {
                            if (PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)[0] != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2 || PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS)[0] != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2)
                            {
                                GUI.contentColor = Color.green;
                                EditorGUILayout.LabelField("OpenGL ES 3.0+");
                                GUI.contentColor = defaultColor;
                            }
                            else
                            {
                                GUI.contentColor = Color.red;
                                EditorGUILayout.LabelField("OpenGL ES 2.0 (Not supported)", EditorStyles.boldLabel);
                                GUI.contentColor = defaultColor;
                            }
                        }
                    }
                }

                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField("Third-party integrations", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Dynamic Water Physics 2");

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.textField))
                    {
                        EditorGUILayout.LabelField(StylizedWaterEditor.DWP2.isInstalled ? "Installed" : "Not Installed", GUILayout.MaxWidth((75f)));
                        GUILayout.FlexibleSpace();

                        using (new EditorGUI.DisabledScope(!StylizedWaterEditor.DWP2.isInstalled || StylizedWaterEditor.DWP2.dataProviderUnlocked))
                        {
                            if (GUILayout.Button("Install integration"))
                            {
                                StylizedWaterEditor.DWP2.UnlockDataProvider();
                                StylizedWaterEditor.DWP2.CheckInstallation();
                            }
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Fog integration");
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        fogConfig = (ShaderConfigurator.FogConfiguration)EditorGUILayout.EnumPopup(fogConfig);
                        if (GUILayout.Button("Change"))
                        {
                            ShaderConfigurator.SetFogConfiguration(fogConfig);
                            fogConfig = ShaderConfigurator.CurrentFogConfiguration;
                        }
                    }
                }
            }
        }

        void DrawDocumentation()
        {
            EditorGUILayout.LabelField("Documentation", UI.Styles.Tab);

            using (new EditorGUILayout.VerticalScope(UI.Styles.Section))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("<b><size=12>Documentation</size></b>\n<i>Usage instructions</i>", UI.Styles.Button))
                {
                    Application.OpenURL(AssetInfo.DOC_URL);
                }
                if (GUILayout.Button("<b><size=12>FAQ/Troubleshooting</size></b>\n<i>Common issues and solutions</i>", UI.Styles.Button))
                {
                    Application.OpenURL(AssetInfo.DOC_URL + "?section=troubleshooting-10");
                }
                EditorGUILayout.EndHorizontal();
            }

        }

        void DrawSupport()
        {
            EditorGUILayout.LabelField("Support", UI.Styles.Tab);

            using (new EditorGUILayout.VerticalScope(UI.Styles.Section))
            {
                UI.DrawNotification("Be sure to consult the documentation, it already covers common topics", MessageType.Info);

                EditorGUILayout.Space();

                //Buttons box
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("<b><size=12>Email</size></b>\n<i>Contact</i>", UI.Styles.Button))
                {
                    Application.OpenURL(AssetInfo.EMAIL_URL);
                }
                if (GUILayout.Button("<b><size=12>Twitter</size></b>\n<i>Follow developments</i>", UI.Styles.Button))
                {
                    Application.OpenURL("https://twitter.com/search?q=staggart%20creations&f=user");
                }
                if (GUILayout.Button("<b><size=12>Forum</size></b>\n<i>Join the discussion</i>", UI.Styles.Button))
                {
                    Application.OpenURL(AssetInfo.FORUM_URL);
                }
                EditorGUILayout.EndHorizontal(); //Buttons box

            }
        }

        private void DrawFooter()
        {
            UI.DrawFooter();
        }

        private class ChangelogWindow : EditorWindow
        {
            private const float height = 400f;
            private const float width = 600f;
            public static void Open()
            {
                ChangelogWindow editorWindow = GetWindow<ChangelogWindow>(true, AssetInfo.ASSET_NAME + " - Changelog", true);

                editorWindow.position = new Rect((Screen.currentResolution.width / 2f) - (width * 0.5f), (Screen.currentResolution.height / 2f) - (height * 0.5f), width, height);

                editorWindow.Init();
                editorWindow.Show();
            }

            private GUIStyle changelogStyle;
            private string changelogContent;
            private Vector2 scrollPos;
            
            private void Init()
            {
                string changelogPath = AssetDatabase.GUIDToAssetPath("f07fbdad38458814a87e9b627e287ccb");
                TextAsset textAsset = ((TextAsset)AssetDatabase.LoadAssetAtPath(changelogPath, typeof(TextAsset)));

                if (textAsset == null) return;

                changelogContent = textAsset.text;

                //Format version header
                changelogContent = Regex.Replace(changelogContent, @"^[0-9].*", "<size=18><b>Version $0</b></size>", RegexOptions.Multiline);
                //Format headers
                changelogContent = Regex.Replace(changelogContent, @"(\w*Added:|Changed:|Fixed:\w*)", "<size=14><b>$0</b></size>", RegexOptions.Multiline);
                //Indent items with dashes
                changelogContent = Regex.Replace(changelogContent, @"^-.*", "  $0", RegexOptions.Multiline);
                
                changelogStyle = new GUIStyle(GUI.skin.label);
                changelogStyle.richText = true;
                changelogStyle.wordWrap = true;
            }

            private void OnGUI()
            {
                if (changelogContent == null)
                {
                    UI.DrawNotification("Changelog text file could not be found. It probably wasn't imported from the asset store...", MessageType.Error);
                    return;
                }
                
                scrollPos = GUILayout.BeginScrollView(scrollPos);
                GUILayout.TextArea(changelogContent, changelogStyle);
                GUILayout.EndScrollView();
            }
        }
    }
}