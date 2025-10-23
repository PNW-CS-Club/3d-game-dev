using System.IO;
using UnityEngine;
namespace Unity.Template.Multiplayer.NGO.Editor
{
    ///<summary>
    ///A set of methods invoked by Unity Cloud Build during the build process
    ///</summary>
    public static class CloudBuildHelpers
    {
        internal const string k_AdditionalClientBuildFilesFolder = "Assets/AdditionalBuildFiles/Client/";
        const string k_AdditionalServerBuildFilesFolder = "Assets/AdditionalBuildFiles/Server/";

        /// <summary>
        /// Method called from CloudBuild when the build finishes.
        /// Needs to be referenced in the settings in CloudBuild's dashboard
        /// </summary>
        /// <param name="exportPath">The path where the build is</param>
        /// <param name="isServerBuild">Is this a server build?</param>
        public static void PostExport(string exportPath, bool isServerBuild)
        {
            if (!isServerBuild)
            {
                bool mobileBuild = false;
#if UNITY_ANDROID || UNITY_IOS
                mobileBuild = true;
#endif
                if (mobileBuild)
                {
                    //mobile builds need to have StartupConfiguration bundled as part of the apk/aab, so the Post-Export pass is useless
                    return;
                }
            }
            FileAttributes attr = File.GetAttributes(exportPath);
            string directory;
            if (attr.HasFlag(FileAttributes.Directory))
            {
                if (exportPath.EndsWith(".app"))
                {
                    Debug.Log("Copying files next to OSX .app. This ensures additional build files are in the right place.");
                    directory = Path.GetDirectoryName(exportPath);
                }
                else
                {
                    directory = exportPath;
                }
            }
            else
            {
                directory = Path.GetDirectoryName(exportPath);
            }
            CopyDirectory(isServerBuild ? k_AdditionalServerBuildFilesFolder : k_AdditionalClientBuildFilesFolder, directory, true);
        }
        
        static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");
            }

            // If the destination directory doesn't exist, create it.
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.Extension != ".meta") //must-have in Unity, useless in builds
                {
                    file.CopyTo(Path.Combine(destDirName, file.Name), false);
                }
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dir.GetDirectories())
                {
                    CopyDirectory(subdir.FullName, Path.Combine(destDirName, subdir.Name), copySubDirs);
                }
            }
        }
    }
}
