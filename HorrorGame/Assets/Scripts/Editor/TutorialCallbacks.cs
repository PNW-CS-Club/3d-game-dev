using UnityEngine;
using UnityEditor;
using Unity.Tutorials.Core.Editor;
using Unity.Netcode;
using System.IO;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using static UnityEditor.Progress;

namespace Unity.Template.Multiplayer.NGO.Editor
{

    /// <summary>
    /// Implement your Tutorial callbacks here.
    /// </summary>
    [CreateAssetMenu(fileName = DefaultFileName, menuName = "Tutorials/" + DefaultFileName + " Instance")]
    public class TutorialCallbacks : ScriptableObject
    {
        [SerializeField] SceneAsset m_MetagameScene;

        /// <summary>
        /// The default file name used to create asset of this class type.
        /// </summary>
        public const string DefaultFileName = "TutorialCallbacks";

        /// <summary>
        /// Creates a TutorialCallbacks asset and shows it in the Project window.
        /// </summary>
        /// <param name="assetPath">
        /// A relative path to the project's root. If not provided, the Project window's currently active folder path is used.
        /// </param>
        /// <returns>The created asset</returns>
        public static ScriptableObject CreateAndShowAsset(string assetPath = null)
        {
            if (assetPath == null)
            {
                assetPath = $"{TutorialEditorUtils.GetActiveFolderPath()}/{DefaultFileName}.asset";
            }
            var asset = CreateInstance<TutorialCallbacks>();
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(assetPath));
            EditorUtility.FocusProjectWindow(); // needed in order to make the selection of newly created asset to really work
            Selection.activeObject = asset;
            return asset;
        }

        public void StartTutorial(Tutorial tutorial)
        {
            TutorialWindow.StartTutorial(tutorial);
        }

        public void FocusGameView()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }

        public void FocusSceneView()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Scene");
        }

        public bool IsRunningAsHost()
        {
            return NetworkManager.Singleton && NetworkManager.Singleton.IsHost;
        }

        public bool IsRunningAsServerOnly()
        {
            return NetworkManager.Singleton && NetworkManager.Singleton.IsServer
                                            && !NetworkManager.Singleton.IsClient;
        }

        public bool IsRunningAsClientOnly()
        {
            return NetworkManager.Singleton && !NetworkManager.Singleton.IsServer
                                            && NetworkManager.Singleton.IsClient;
        }

        public bool IsProjectLinkedToUnityCloud()
        {
            return !string.IsNullOrEmpty(Application.cloudProjectId);
        }

        public bool IsUnityCloudEnvironmentSet()
        {
            return EnvironmentsApi.Instance.ActiveEnvironmentName == "production";
        }

        public bool IsUploadingBuildToCloud()
        {
            foreach (var provider in Deployments.Instance.DeploymentProviders)
            {
                foreach (var item in provider.DeploymentItems)
                {
                    if (item.Name == "LinuxTestBuild.build")
                    {
                        return item.Progress > 0;
                    }
                }
            }
            return false;
        }

        public bool WaitUntilBuildIsUploadedToCloud()
        {
            foreach (var provider in Deployments.Instance.DeploymentProviders)
            {
                foreach (var item in provider.DeploymentItems)
                {
                    if (item.Name == "LinuxTestBuild.build")
                    {
                        return item.Status.Message == "Deployed";
                    }
                }
            }
            return false;
        }

        public bool WaitUntilFleetAndConfigurationAreDeployed()
        {
            bool fleetConfigDeployed = false;
            bool buildConfigDeployed = false;
            foreach (var provider in Deployments.Instance.DeploymentProviders)
            {
                foreach (var item in provider.DeploymentItems)
                {
                    if (item.Name == "LinuxTestFleet.fleet")
                    {
                        fleetConfigDeployed = item.Status.Message == "Deployed";
                    }
                    else if (item.Name == "LinuxBuildConfiguration.buildConfig")
                    {
                        buildConfigDeployed = item.Status.Message == "Deployed";
                    }
                }
            }
            return fleetConfigDeployed && buildConfigDeployed;
        }

        public bool WaitUntilMatchmakerConfigurationIsDeployed()
        {
            bool matchmakerQueueConfigDeployed = false;
            bool matchmakerEnvironmentConfigDeployed = false;
            foreach (var provider in Deployments.Instance.DeploymentProviders)
            {
                foreach (var item in provider.DeploymentItems)
                {
                    if (item.Name == "MatchmakerQueue.mmq")
                    {
                        matchmakerQueueConfigDeployed = item.Status.Message == "Deployed";
                    }
                    else if (item.Name == "MatchmakerEnvironment.mme")
                    {
                        matchmakerEnvironmentConfigDeployed = item.Status.Message == "Deployed";
                    }
                }
            }
            return matchmakerQueueConfigDeployed && matchmakerEnvironmentConfigDeployed;
        }

        public bool LinuxServerBuildExists()
        {
            string serverBuildPath = Path.Combine(Application.dataPath, "..", "Builds", "Server");
            var directoryInfo = new DirectoryInfo(serverBuildPath);
            if (!directoryInfo.Exists)
            {
                return false;
            }
            foreach (var file in directoryInfo.EnumerateFiles())
            {
                if (file.Extension == ".x86_64")
                {
                    return true;
                }
            }
            return false;
        }

        public bool LinuxServerBuildTargetInstalled()
        {
            return BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
        }

        public void OpenURL(string url)
        {
            TutorialEditorUtils.OpenUrl(url);
        }

        public void LoadMetagameScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(m_MetagameScene));
        }

        public bool IsBootstrapperSetupProperly(int requiredPlayers)
        {
            var boostrapperWindow = EditorWindow.GetWindow<BootstrapperWindow>("Bootstrapper", false);
            return boostrapperWindow.AutoConnectOnStartup
                && boostrapperWindow.OverrideMultiplayerRole
                && boostrapperWindow.HostSelf
                && boostrapperWindow.MaxPlayers == requiredPlayers;
        }

        public void EnableMultiplayInCloudDashboard()
        {
            OpenURL($"https://cloud.unity3d.com/organizations/{CloudProjectSettings.organizationKey}/projects/{CloudProjectSettings.projectId}/environments/{EnvironmentsApi.Instance.ActiveEnvironmentId}/multiplay/overview");
        }
    }
}