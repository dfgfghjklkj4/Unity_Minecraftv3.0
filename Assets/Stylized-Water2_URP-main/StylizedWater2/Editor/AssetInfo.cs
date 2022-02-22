//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System;
using System.Net;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace StylizedWater2
{
    public class AssetInfo
    {
        public const string ASSET_NAME = "Stylized Water 2";
        public const string ASSET_ID = "170386";
        public const string ASSET_ABRV = "SWS2";

        public const string INSTALLED_VERSION = "1.1.4";
        public const string MIN_UNITY_VERSION = "2019.3.7f1";
        public const string MIN_URP_VERSION = "7.4.1";

        public const string VERSION_FETCH_URL = "http://www.staggart.xyz/backend/versions/stylizedwater2.php";
        public const string DOC_URL = "http://staggart.xyz/unity/stylized-water-2/sws-2-docs/";
        public const string FORUM_URL = "https://forum.unity.com/threads/999132/";
        public const string EMAIL_URL = "mailto:contact@staggart.xyz?subject=Stylized Water 2";

        public static bool IS_UPDATED = true;
        public static bool compatibleVersion = true;
        public static bool alphaVersion = false;

#if !URP //Enabled when com.unity.render-pipelines.universal is below MIN_URP_VERSION
        [InitializeOnLoad]
        sealed class PackageInstaller : Editor
        {
            [InitializeOnLoadMethod]
            public static void Initialize()
            {
                GetLatestCompatibleURPVersion();

                if (EditorUtility.DisplayDialog(ASSET_NAME + " v" + INSTALLED_VERSION, "This package requires the Universal Render Pipeline " + MIN_URP_VERSION + " or newer, would you like to install or update it now?", "OK", "Later"))
                {
					Debug.Log("Universal Render Pipeline <b>v" + lastestURPVersion + "</b> will start installing in a moment. Please refer to the URP documentation for set up instructions");
					
                    InstallURP();
                }
            }

            public const string URP_PACKAGE_ID = "com.unity.render-pipelines.universal";
            private static PackageInfo urpPackage;

            private static string lastestURPVersion;

            private static PackageInfo GetURPPackage()
            {
                SearchRequest request = Client.Search(URP_PACKAGE_ID);
                
                while (request.Status == StatusCode.InProgress) { /* Waiting... */ }

                if (request.Status == StatusCode.Failure)
                {
                    Debug.Log("Failed to retrieve URP package from Package Manager...");
                    return null;
                }

                return request.Result[0];
            }
            
#if SWS_DEV
            [MenuItem("SWS/Get latest URP version")]
#endif
            private static void GetLatestCompatibleURPVersion()
            {
                if(urpPackage == null) urpPackage = GetURPPackage();
                if(urpPackage == null) return;
                
                lastestURPVersion = urpPackage.versions.latestCompatible;
                
#if SWS_DEV
                Debug.Log("Latest compatible URP version: " + lastestURPVersion);
#endif
            }

            private static void InstallURP()
            {
                if(urpPackage == null) urpPackage = GetURPPackage();
                if(urpPackage == null) return;
                
                lastestURPVersion = urpPackage.versions.latestCompatible;

                AddRequest addRequest = Client.Add(URP_PACKAGE_ID + "@" + lastestURPVersion);
                
                //Update Core and Shader Graph packages as well, doesn't always happen automatically
                for (int i = 0; i < urpPackage.dependencies.Length; i++)
                {
#if SWS_DEV
                    Debug.Log("Updating URP dependency <i>" + urpPackage.dependencies[i].name + "</i> to " + urpPackage.dependencies[i].version);
#endif
                    addRequest = Client.Add(urpPackage.dependencies[i].name + "@" + urpPackage.dependencies[i].version);
                }
                
                //Wait until finished
                while(!addRequest.IsCompleted || addRequest.Status == StatusCode.InProgress) { }
                
                ReimportShader();
            }

            private const string ShaderGUID = "f04c9486b297dd848909db26983c9ddb";

            //Required after installing the URP package, so references to shader libraries are caught up
            private static void ReimportShader()
            {
                string shaderPath = AssetDatabase.GUIDToAssetPath(ShaderGUID);

                if (shaderPath == string.Empty)
                {
                    Debug.LogError("[" + ASSET_NAME + "] Unable to locate shader by GUID, was it changed or not imported?");
                    return;
                }
                EditorUtility.DisplayProgressBar(ASSET_NAME, "Reimporting shaders...", 1f);
                
                AssetDatabase.ImportAsset(shaderPath);
                
                EditorUtility.ClearProgressBar();
            }
        }
#endif

        public static bool MeetsMinimumVersion(string versionMinimum)
        {
            Version curVersion = new Version(INSTALLED_VERSION);
            Version minVersion = new Version(versionMinimum);

            return curVersion >= minVersion;
        }

        public static void OpenStorePage()
        {
            Application.OpenURL("com.unity3d.kharma:content/" + ASSET_ID);
        }
        
        public static void OpenForumPage()
        {
            Application.OpenURL(FORUM_URL + "/page-999");
        }

        public static string PACKAGE_ROOT_FOLDER
        {
            get { return SessionState.GetString(ASSET_ABRV + "_BASE_FOLDER", string.Empty); }
            set { SessionState.SetString(ASSET_ABRV + "_BASE_FOLDER", value); }
        }

        public static string GetRootFolder()
        {
            //Get script path
            string scriptFilePath = AssetDatabase.GUIDToAssetPath("d5972a1c9cddd9941aec37a3343647aa");

            //Truncate to get relative path
            PACKAGE_ROOT_FOLDER = scriptFilePath.Replace("Editor/AssetInfo.cs", string.Empty);

#if SWS_DEV
            //Debug.Log("<b>Package root</b> " + PACKAGE_ROOT_FOLDER);
#endif

            return PACKAGE_ROOT_FOLDER;
        }

        public static class VersionChecking
        {
            public static string GetUnityVersion()
            {
                string version = UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion();
                
                //Remove GUID in parenthesis 
                return version.Substring(0, version.LastIndexOf(" ("));
            }
            
            public static void CheckUnityVersion()
            {
#if !UNITY_2019_3_OR_NEWER
                compatibleVersion = false;
#endif
#if UNITY_2019_3_OR_NEWER
                compatibleVersion = true;
#endif

                alphaVersion = GetUnityVersion().Contains("f") == false;
            }

            public static string fetchedVersionString;
            public static System.Version fetchedVersion;
            private static bool showPopup;

            public enum VersionStatus
            {
                UpToDate,
                Outdated
            }

            public enum QueryStatus
            {
                Fetching,
                Completed,
                Failed
            }
            public static QueryStatus queryStatus = QueryStatus.Completed;

#if SWS_DEV
            [MenuItem("SWS/Check for update")]
#endif
            public static void GetLatestVersionPopup()
            {
                CheckForUpdate(true);
            }

            private static int VersionStringToInt(string input)
            {
                //Remove all non-alphanumeric characters from version 
                input = input.Replace(".", string.Empty);
                input = input.Replace(" BETA", string.Empty);
                return int.Parse(input, System.Globalization.NumberStyles.Any);
            }

            public static void CheckForUpdate(bool showPopup = false)
            {
                VersionChecking.showPopup = showPopup;

                queryStatus = QueryStatus.Fetching;

                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadStringCompleted += new System.Net.DownloadStringCompletedEventHandler(OnRetreivedServerVersion);
                    webClient.DownloadStringAsync(new System.Uri(VERSION_FETCH_URL), fetchedVersionString);
                }
            }

            private static void OnRetreivedServerVersion(object sender, DownloadStringCompletedEventArgs e)
            {
                if (e.Error == null && !e.Cancelled)
                {
                    fetchedVersionString = e.Result;
                    fetchedVersion = new System.Version(fetchedVersionString);
                    System.Version installedVersion = new System.Version(INSTALLED_VERSION);

                    //Success
                    IS_UPDATED = (installedVersion >= fetchedVersion) ? true : false;

#if SWS_DEV
                    Debug.Log("<b>PackageVersionCheck</b> Up-to-date = " + IS_UPDATED + " (Installed:" + INSTALLED_VERSION + ") (Remote:" + fetchedVersionString + ")");
#endif

                    queryStatus = QueryStatus.Completed;

                    if (VersionChecking.showPopup)
                    {
                        if (!IS_UPDATED)
                        {
                            if (EditorUtility.DisplayDialog(ASSET_NAME + ", version " + INSTALLED_VERSION, "A new version is available: " + fetchedVersionString, "Open Package Manager", "Close"))
                            {
                                OpenStorePage();
                            }
                        }
                        else
                        {
                            if (EditorUtility.DisplayDialog(ASSET_NAME + ", version " + INSTALLED_VERSION, "Your current version is up-to-date!", "Close")) { }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[" + ASSET_NAME + "] Contacting update server failed: " + e.Error.Message);
                    queryStatus = QueryStatus.Failed;

                    //When failed, assume installation is up-to-date
                    IS_UPDATED = true;
                }
            }
        }
    }
}