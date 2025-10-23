using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Template.Multiplayer.NGO.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    ///<summary>
    ///Performs additional operations before/after the build is done
    ///</summary>
    public class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static readonly string[] k_BuildOnlySymbols = new string[]
        {
            //"LIVE", //this is an example, add your own symbols instead
        };

        static readonly string[] k_EditorOnlySymbols = new string[]
        {
            //"DEV", //this is an example, add your own symbols instead
        };

        /// <summary>
        /// CallbackOrder of the preprocessing and postprocessing calls.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Called at the beginning of the build process
        /// </summary>
        /// <param name="report">The generated build report.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            AssetDatabase.SaveAssets();
            ApplyPreBuildChanges();

            string parentDirectory = Path.GetFileName(Path.GetDirectoryName(report.summary.outputPath));
            DeleteOutputFolder(parentDirectory);

            string definesString = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            List<string> allDefines = definesString.Split(';').ToList();
            if (k_BuildOnlySymbols.Length > 0)
            {
                allDefines.AddRange(k_BuildOnlySymbols.Except(allDefines));
            }
            if (k_EditorOnlySymbols.Length > 0)
            {
                allDefines.RemoveAll(def => k_EditorOnlySymbols.Contains(def));
            }
            Debug.Log($"Symbols used for build: {string.Join(";", allDefines.ToArray())}");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), string.Join(";", allDefines.ToArray()));
        }
        /// <summary>
        /// Called at the end of the build process
        /// </summary>
        /// <param name="report">The generated build report.</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            RevertPreBuildChanges();
            string definesString = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            List<string> allDefines = definesString.Split(';').ToList();

            if (k_BuildOnlySymbols.Length > 0)
            {
                allDefines.RemoveAll(def => k_BuildOnlySymbols.Contains(def));
            }
            if (k_EditorOnlySymbols.Length > 0)
            {
                allDefines.AddRange(k_EditorOnlySymbols.Except(allDefines));
            }
            Debug.Log($"Symbols restored after build: {string.Join(";", allDefines.ToArray())}");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), string.Join(";", allDefines.ToArray()));
            AssetDatabase.SaveAssets();
#if !CLOUD_BUILD_WINDOWS && !CLOUD_BUILD_LINUX && !CLOUD_BUILD_MAC
            Debug.Log($"Manually Doing PostExport: {report.summary.outputPath}");
            bool isServerBuild = report.summary.outputPath.Contains(".x86_64", System.StringComparison.OrdinalIgnoreCase); //.x86_64 is the extension of the Linux build
            CloudBuildHelpers.PostExport(report.summary.outputPath, isServerBuild);
#endif
        }

        void ApplyPreBuildChanges()
        {
            //add your code to apply changes to systems here, I.E: to reference different testing environments
            ApplyChangesToMetagameApplication();
            bool mobileBuild = false;
#if UNITY_ANDROID || UNITY_IOS
            mobileBuild = true;
#endif
            if (!mobileBuild)
            {
                return;
            }

            string relativePathToStreamingAssetsFolder = Path.Combine("Assets", "StreamingAssets");
            if (!Directory.Exists(relativePathToStreamingAssetsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "StreamingAssets");
            }

            string targetPath = Path.Combine(relativePathToStreamingAssetsFolder, "Client");
            Debug.Log($"Preprocess: Copying {CloudBuildHelpers.k_AdditionalClientBuildFilesFolder} to {targetPath} folder");
            if (Directory.Exists(targetPath))
            {
                FileUtil.DeleteFileOrDirectory(targetPath);
            }
            FileUtil.CopyFileOrDirectory(CloudBuildHelpers.k_AdditionalClientBuildFilesFolder, targetPath);

            Debug.Log($"Preprocess: Importing {targetPath}");

            foreach (var item in Directory.GetFiles(targetPath))
            {
                AssetDatabase.ImportAsset(item);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Applied changes before build");
        }

        void RevertPreBuildChanges()
        {
            RevertChangesToMetagameApplication();
            //add your code to revert changes to systems here, I.E: to reference different testing environments
            bool mobileBuild = false;
#if UNITY_ANDROID || UNITY_IOS
            mobileBuild = true;
#endif
            if (!mobileBuild)
            {
                return;
            }

            string relativePathToStreamingAssetsFolder = Path.Combine("Assets", "StreamingAssets", "Client");
            Debug.Log($"PostProcess: Deleting {relativePathToStreamingAssetsFolder}");
            AssetDatabase.DeleteAsset(relativePathToStreamingAssetsFolder);
            Debug.Log("Reverted changes before build");
        }

        void ApplyChangesToMetagameApplication()
        {
            MetagameApplication app = FindMetagameAppInProject();
            //add your code to apply changes to the MetagameApplication here, I.E: to reference different testing environments
            PrefabUtility.SavePrefabAsset(app.gameObject, out bool savedSuccessfully);
            if (!savedSuccessfully)
            {
                throw new BuildPlayerWindow.BuildMethodException("Failed to alter MetagameApplication before building");
            }
            Debug.Log("Updated MetagameApp before build");
        }

        void RevertChangesToMetagameApplication()
        {
            MetagameApplication app = FindMetagameAppInProject();
            //add your code to revert changes to the MetagameApplication here, I.E: to reference different testing environments
            PrefabUtility.SavePrefabAsset(app.gameObject, out bool savedSuccessfully);
            if (!savedSuccessfully)
            {
                throw new BuildPlayerWindow.BuildMethodException("Failed to restore MetagameApplication after building");
            }
            Debug.Log("Updated MetagameApp after build");
        }

        MetagameApplication FindMetagameAppInProject()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/Prefabs/Metagame" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = (GameObject)AssetDatabase.LoadMainAssetAtPath(path);
                if (root.GetComponent<MetagameApplication>())
                {
                    return root.GetComponent<MetagameApplication>();
                }
            }
            return null;
        }

        [MenuItem("Multiplayer/Builds/All")]
        static void MakeServerAndClientBuilds()
        {
            PerformStandaloneLinux64();
            PerformStandaloneWindows64();
            PerformStandaloneMac();
            PerformAndroid();
            PerformIOS();
        }

        [MenuItem("Multiplayer/Builds/Server_StandaloneLinux")]
        static void PerformStandaloneLinux64()
        {
            Debug.Log("Building server");
            if (EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Server, BuildTarget.StandaloneLinux64))
            {
                BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = GetScenePaths(),
                    locationPathName = "Builds/Server/Game.x86_64",
                    target = BuildTarget.StandaloneLinux64,
                    subtarget = (int)StandaloneBuildSubtarget.Server,
                });
            }
        }

        [MenuItem("Multiplayer/Builds/Client_StandaloneWindows64")]
        static void PerformStandaloneWindows64()
        {
            Debug.Log("Building Windows client");

            if (EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Standalone, BuildTarget.StandaloneWindows64))
            {
                BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = GetScenePaths(),
                    locationPathName = "Builds/Client/Game.exe",
                    target = BuildTarget.StandaloneWindows64,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                });
            }
        }

        [MenuItem("Multiplayer/Builds/Client_StandaloneMac")]
        static void PerformStandaloneMac()
        {
#if UNITY_EDITOR_OSX
            Debug.Log("Building Mac client");
            if (EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Standalone, BuildTarget.StandaloneOSX))
            {
                BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = GetScenePaths(),
                    locationPathName = "Builds/Client_Mac/Game.app",
                    target = BuildTarget.StandaloneOSX,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                });
            }
#else
            Debug.LogWarning("Could not build Standalone OSX on a non-OSX machine.");
#endif
        }

        [MenuItem("Multiplayer/Builds/Client_Android")]
        static void PerformAndroid()
        {
            Debug.Log("Building Android client (app bundle)");

            if (EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android))
            {
                EditorUserBuildSettings.buildAppBundle = true;
                BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = GetScenePaths(),
                    locationPathName = "Builds/Client_Android/Game.aab",
                    target = BuildTarget.Android,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                });
            }
        }

        [MenuItem("Multiplayer/Builds/Client_iOS")]
        static void PerformIOS()
        {
#if UNITY_EDITOR_OSX
            Debug.Log("Building iOS client");

            if (EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.iOS, BuildTarget.iOS))
            {
                BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = GetScenePaths(),
                    locationPathName = "Builds/Client_iOS/Game",
                    target = BuildTarget.iOS,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                });
            }
#else
            Debug.LogWarning("Could not build iOS app on a non-OSX machine.");
#endif
        }

        static void DeleteOutputFolder(string pathFromBuildsFolder)
        {
            string projectPath = Path.Combine(Application.dataPath, "..", "Builds", pathFromBuildsFolder);
            var directoryInfo = new DirectoryInfo(projectPath);
            if (directoryInfo.Exists)
            {
                Debug.Log("Deleting existing: " + directoryInfo.Name);
                directoryInfo.Delete(true);
            }
        }

        static string[] GetScenePaths()
        {
            var scenes = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                scenes[i] = EditorBuildSettings.scenes[i].path;
            }
            return scenes;
        }
    }
}
