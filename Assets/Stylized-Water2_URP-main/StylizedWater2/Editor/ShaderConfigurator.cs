//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace StylizedWater2
{
    public class ShaderConfigurator
    {
        private const string ShaderGUID = "f04c9486b297dd848909db26983c9ddb";
        private static string ShaderFilePath;
        private const string TessellationShaderGUID = "97d819883abaa514984aaeb4e870697d";
        private static string TessellationFilePath;

        private const string FogLibraryGUID = "8427feba489ff354ab7b22c82a15ba03";
        private static string FogLibraryFilePath;

        private const string ENVIRO_LIBRARY_GUID = "221026906cc779e4b9ab486bbbd30032";
        private const string AZURE_LIBRARY_GUID = "5da6209b193d6eb49b25c88c861e52fa";
        private const string AHF_LIBRARY_GUID = "8db8edf9bba0e9d48998019ca6c2f9ff";
        private const string SCPE_LIBRARY_GUID = "a66e4b0e5c776404e9a091531ccac3f2";

        public enum FogConfiguration
        {
            UnityFog,
            Enviro,
            Azure,
            AtmosphericHeightFog,
            SCPostEffects
        }

        public static FogConfiguration CurrentFogConfiguration
        {
            get { return (FogConfiguration)SessionState.GetInt("SWS2_FOG_INTEGRATION", (int)FogConfiguration.UnityFog); }
            set { SessionState.SetInt("SWS2_FOG_INTEGRATION", (int)value); }
        }

        public static void GetCurrentFogConfiguration()
        {
            FogConfiguration config;
            FogConfiguration.TryParse(GetConfiguration(FogLibraryFilePath), out config);
            
            CurrentFogConfiguration = config;
        }

        public static void SetFogConfiguration(FogConfiguration config)
        {
            if(config == FogConfiguration.UnityFog) ConfigureUnityFog();
            if(config == FogConfiguration.Enviro) ConfigureEnviroFog();
            if(config == FogConfiguration.Azure) ConfigureAzureFog();
            if(config == FogConfiguration.AtmosphericHeightFog) ConfigureAtmosphericHeightFog();
            if(config == FogConfiguration.SCPostEffects) ConfigureSCPEFog();
        }

        public struct CodeBlock
        {
            public int startLine;
            public int endLine;
        }

        public static void RefreshShaderFilePaths()
        {
            ShaderFilePath = AssetDatabase.GUIDToAssetPath(ShaderGUID);
            TessellationFilePath = AssetDatabase.GUIDToAssetPath(TessellationShaderGUID);
            FogLibraryFilePath = AssetDatabase.GUIDToAssetPath(FogLibraryGUID);
        }
        
        public static void ConfigureUnityFog()
        {
            RefreshShaderFilePaths();

            EditorUtility.DisplayProgressBar("Stylized Water 2", "Modifying shader...", 1f);
            {
                ToggleCodeBlock(FogLibraryFilePath, "UnityFog", true);
                ToggleCodeBlock(FogLibraryFilePath, "Enviro", false);
                ToggleCodeBlock(FogLibraryFilePath, "Azure", false);
                ToggleCodeBlock(FogLibraryFilePath, "AtmosphericHeightFog", false);
                ToggleCodeBlock(FogLibraryFilePath, "SCPE", false);

                //multi_compile keyword
                ToggleCodeBlock(ShaderFilePath, "UnityFog", true);
                ToggleCodeBlock(ShaderFilePath, "AtmosphericHeightFog", false);
                ToggleCodeBlock(TessellationFilePath, "UnityFog", true);
                ToggleCodeBlock(TessellationFilePath, "AtmosphericHeightFog", false);
            }
            EditorUtility.ClearProgressBar();

            CurrentFogConfiguration = FogConfiguration.UnityFog;
            Debug.Log("Shader file modified to use " + CurrentFogConfiguration + " fog rendering");
        }
        
        public static void ConfigureEnviroFog()
        {
            RefreshShaderFilePaths();

            EditorUtility.DisplayProgressBar("Stylized Water 2", "Modifying shader...", 1f);
            {
                ToggleCodeBlock(FogLibraryFilePath, "UnityFog", false);
                ToggleCodeBlock(FogLibraryFilePath, "Enviro", true);
                ToggleCodeBlock(FogLibraryFilePath, "Azure", false);
                ToggleCodeBlock(FogLibraryFilePath, "AtmosphericHeightFog", false);
                ToggleCodeBlock(FogLibraryFilePath, "SCPE", false);

                //multi_compile keyword
                ToggleCodeBlock(ShaderFilePath, "UnityFog", false);
                ToggleCodeBlock(ShaderFilePath, "AtmosphericHeightFog", false);
                ToggleCodeBlock(TessellationFilePath, "UnityFog", false);
                ToggleCodeBlock(TessellationFilePath, "AtmosphericHeightFog", false);
                
                string libraryFilePath = AssetDatabase.GUIDToAssetPath(ENVIRO_LIBRARY_GUID);
                if(libraryFilePath == string.Empty) Debug.LogError("Fog shader library could not be found with GUID " + ENVIRO_LIBRARY_GUID + ". This means it was changed by the author. Please notify me!");
                else SetIncludePath(FogLibraryFilePath, "Enviro", libraryFilePath);
            }
            EditorUtility.ClearProgressBar();

            CurrentFogConfiguration = FogConfiguration.Enviro;
            Debug.Log("Shader file modified to use " + CurrentFogConfiguration + " fog rendering");
        }
        
        public static void ConfigureAzureFog()
        {
            RefreshShaderFilePaths();

            EditorUtility.DisplayProgressBar("Stylized Water 2", "Modifying shader...", 1f);
            {
                ToggleCodeBlock(FogLibraryFilePath, "UnityFog", false);
                ToggleCodeBlock(FogLibraryFilePath, "Enviro", false);
                ToggleCodeBlock(FogLibraryFilePath, "Azure", true);
                ToggleCodeBlock(FogLibraryFilePath, "AtmosphericHeightFog", false);
                ToggleCodeBlock(FogLibraryFilePath, "SCPE", false);

                //multi_compile keyword
                ToggleCodeBlock(ShaderFilePath, "UnityFog", false);
                ToggleCodeBlock(ShaderFilePath, "AtmosphericHeightFog", false);
                ToggleCodeBlock(TessellationFilePath, "UnityFog", false);
                ToggleCodeBlock(TessellationFilePath, "AtmosphericHeightFog", false);

                string libraryFilePath = AssetDatabase.GUIDToAssetPath(AZURE_LIBRARY_GUID);
                if(libraryFilePath == string.Empty) Debug.LogError("Fog shader library could not be found with GUID " + AZURE_LIBRARY_GUID + ". This means it was changed by the author. Please notify me!");
                else SetIncludePath(FogLibraryFilePath, "Azure", libraryFilePath);
            }
            EditorUtility.ClearProgressBar();

            CurrentFogConfiguration = FogConfiguration.Azure;
            Debug.Log("Shader file modified to use " + CurrentFogConfiguration + " fog rendering");
        }
        
        public static void ConfigureAtmosphericHeightFog()
        {
            RefreshShaderFilePaths();

            EditorUtility.DisplayProgressBar("Stylized Water 2", "Modifying shader...", 1f);
            {
                ToggleCodeBlock(FogLibraryFilePath, "UnityFog", false);
                ToggleCodeBlock(FogLibraryFilePath, "Enviro", false);
                ToggleCodeBlock(FogLibraryFilePath, "Azure", false);
                ToggleCodeBlock(FogLibraryFilePath, "AtmosphericHeightFog", true);
                ToggleCodeBlock(FogLibraryFilePath, "SCPE", false);

                //multi_compile keyword
                ToggleCodeBlock(ShaderFilePath, "UnityFog", false);
                ToggleCodeBlock(ShaderFilePath, "AtmosphericHeightFog", true);
                ToggleCodeBlock(TessellationFilePath, "UnityFog", false);
                ToggleCodeBlock(TessellationFilePath, "AtmosphericHeightFog", true);
                
                string libraryFilePath = AssetDatabase.GUIDToAssetPath(AHF_LIBRARY_GUID);
                if(libraryFilePath == string.Empty) Debug.LogError("AHF fog shader library could not be found with GUID " + AHF_LIBRARY_GUID + ". This means it was changed by the author. Please notify me!");
                else SetIncludePath(FogLibraryFilePath, "AtmosphericHeightFog", libraryFilePath);

            }
            EditorUtility.ClearProgressBar();

            CurrentFogConfiguration = FogConfiguration.AtmosphericHeightFog;
            Debug.Log("Shader file modified to use " + CurrentFogConfiguration + " fog rendering");
        }

        public static void ConfigureSCPEFog()
        {
            RefreshShaderFilePaths();

            EditorUtility.DisplayProgressBar("Stylized Water 2", "Modifying shader...", 1f);
            {
                ToggleCodeBlock(FogLibraryFilePath, "UnityFog", false);
                ToggleCodeBlock(FogLibraryFilePath, "Enviro", false);
                ToggleCodeBlock(FogLibraryFilePath, "Azure", false);
                ToggleCodeBlock(FogLibraryFilePath, "AtmosphericHeightFog", false);
                ToggleCodeBlock(FogLibraryFilePath, "SCPE", true);
                
                //multi_compile keyword
                ToggleCodeBlock(ShaderFilePath, "UnityFog", false);
                ToggleCodeBlock(ShaderFilePath, "AtmosphericHeightFog", false);
                ToggleCodeBlock(TessellationFilePath, "UnityFog", false);
                ToggleCodeBlock(TessellationFilePath, "AtmosphericHeightFog", false);

                string libraryFilePath = AssetDatabase.GUIDToAssetPath(SCPE_LIBRARY_GUID);
                if(libraryFilePath == string.Empty) Debug.LogError("SCPE fog shader library could not be found with GUID " + SCPE_LIBRARY_GUID + ". This means it was changed by the author. Please notify me!");
                SetIncludePath(FogLibraryFilePath, "SCPE", libraryFilePath);

            }
            EditorUtility.ClearProgressBar();

            CurrentFogConfiguration = FogConfiguration.SCPostEffects;
            Debug.Log("Shader file modified to use " + CurrentFogConfiguration + " fog rendering");
        }

        //TODO: Process multiple keywords in one read/write pass
        private static void ToggleCodeBlock(string filePath, string id, bool enable)
        {
            string[] lines = File.ReadAllLines(filePath);

            List<CodeBlock> codeBlocks = new List<CodeBlock>();

            //Find start and end line indices
            for (int i = 0; i < lines.Length; i++)
            {
                bool blockEndReached = false;

                if (lines[i].Contains("/* Configuration: ") && enable)
                {
                    lines[i] = lines[i].Replace(lines[i], "/* Configuration: " + id + " */");
                }

                if (lines[i].Contains("start " + id))
                {
                    CodeBlock codeBlock = new CodeBlock();

                    codeBlock.startLine = i;

                    //Find related end point
                    for (int l = codeBlock.startLine; l < lines.Length; l++)
                    {
                        if (blockEndReached == false)
                        {
                            if (lines[l].Contains("end " + id))
                            {
                                codeBlock.endLine = l;

                                blockEndReached = true;
                            }
                        }
                    }

                    codeBlocks.Add(codeBlock);
                    blockEndReached = false;
                }

            }

            if (codeBlocks.Count == 0)
            {
                //Debug.Log("No code blocks with the marker \"" + id + "\" were found in file");

                return;
            }

            foreach (CodeBlock codeBlock in codeBlocks)
            {
                if (codeBlock.startLine == codeBlock.endLine) continue;

                //Debug.Log((enable ? "Enabled" : "Disabled") + " \"" + id + "\" code block. Lines " + (codeBlock.startLine + 1) + " through " + (codeBlock.endLine + 1));

                for (int i = codeBlock.startLine + 1; i < codeBlock.endLine; i++)
                {
                    //Uncomment lines
                    if (enable == true)
                    {
                        if (lines[i].StartsWith("//") == true) lines[i] = lines[i].Remove(0, 2);
                    }
                    //Comment out lines
                    else
                    {
                        if (lines[i].StartsWith("//") == false) lines[i] = "//" + lines[i];
                    }
                }
            }

            File.WriteAllLines(filePath, lines);

            AssetDatabase.ImportAsset(filePath);
        }

        private static void SetIncludePath(string filePath, string id, string libraryPath)
        {
            string[] lines = File.ReadAllLines(filePath);

            //Find start and end line indices
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("include " + id))
                {
                    lines[i + 1] = String.Format("#include \"{0}\"", libraryPath);
                    
                    File.WriteAllLines(filePath, lines);
                    AssetDatabase.ImportAsset(filePath);

                    return;
                }
            }
        }

        private static string GetConfiguration(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            string configStr = lines[0].Replace("/* Configuration: ", string.Empty);
            configStr = configStr.Replace(" */", string.Empty);

            return configStr;
        }
    }
    
    //Strips keywords from the shader for extensions not installed. Would otherwise trow errors during the build process, blocking it
    //This allows the use of 1 single shader, and extensions to be updated individually
    class KeywordStripper : IPreprocessShaders
    {
        private Shader waterShader;
        ShaderKeyword UNDERWATER_ENABLED = new ShaderKeyword("UNDERWATER_ENABLED");
        
        //URP 12+
        ShaderKeyword _REFLECTION_PROBE_BLENDING = new ShaderKeyword("_REFLECTION_PROBE_BLENDING");
        ShaderKeyword _REFLECTION_PROBE_BOX_PROJECTION = new ShaderKeyword("_REFLECTION_PROBE_BOX_PROJECTION");
        ShaderKeyword DEBUG_DISPLAY = new ShaderKeyword("DEBUG_DISPLAY");

        private bool isInstalledUnderwater;
        public KeywordStripper()
        {
            waterShader = Shader.Find("Universal Render Pipeline/FX/Stylized Water 2");

            //Checking for UnderwaterRenderer.cs
            isInstalledUnderwater = AssetDatabase.GUIDToAssetPath("6a52edc7a3652d84784e10be859d5807") != string.Empty;
            #if SWS_DEV
            Debug.Log("Underwater Rendering installed: " + isInstalledUnderwater);
            #endif
        }

        public int callbackOrder { get { return 0; } }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {
            if (object.Equals(shader, waterShader) == false) return;
            
            #if SWS_DEV
            Debug.Log("OnProcessShader running for " + shader.name + ". Pass: " + snippet.passName);
            #endif
            
            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                StripKeyword(UNDERWATER_ENABLED, ref shaderCompilerData, ref i, isInstalledUnderwater == false);
                
                #if !URP_12_0_OR_NEWER
                StripKeyword(_REFLECTION_PROBE_BLENDING, ref shaderCompilerData, ref i);
                StripKeyword(_REFLECTION_PROBE_BOX_PROJECTION, ref shaderCompilerData, ref i);
                StripKeyword(DEBUG_DISPLAY, ref shaderCompilerData, ref i);
                #endif
            }
        }

        private void StripKeyword(ShaderKeyword keyword, ref IList<ShaderCompilerData> shaderCompilerData, ref int i, bool condition = true)
        {
            if (condition && shaderCompilerData[i].shaderKeywordSet.IsEnabled(keyword))
            {
                shaderCompilerData.RemoveAt(i);
                --i;
                
                #if SWS_DEV
                Debug.Log("Stripped " + ShaderKeyword.GetKeywordName(waterShader, keyword));
                #endif
            }
        }
    }
}