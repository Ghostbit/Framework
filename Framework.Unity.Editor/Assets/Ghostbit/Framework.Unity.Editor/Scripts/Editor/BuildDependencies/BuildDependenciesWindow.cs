using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using Ghostbit.Framework.Unity.Editor.Utils;

namespace Ghostbit.Framework.Unity.Editor.BuildDependencies
{
    public class BuildDependenciesWindow : EditorWindow
    {
        [MenuItem("Ghostbit/Build Dependencies/Open")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<BuildDependenciesWindow>();
        }

        [MenuItem("Ghostbit/Build Dependencies/Pull")]
        public static void PullDependencies()
        {
            foreach(Dependency dep in dependencies.Values)
            {
                if(dep.enabled)
                {
                    DirectoryInfo di = new DirectoryInfo(Path.Combine(rootPath, dep.pullFromPath));
                    FileInfo[] files = di.GetFiles("*", SearchOption.AllDirectories);
                    foreach(FileInfo fi in files)
                    {
                        //Debug.Log("Found dependency file (" + fi.Extension + "):" + fi.FullName);
                        if(fi.Extension != ".meta")
                        {
                            
                            string relativePath = PathUtil.MakeRelativePath(Path.Combine(rootPath, dep.pullFromPath), fi.FullName);
                            //Debug.Log("relativePath = " + relativePath);
                            string srcPath = fi.FullName.Replace(@"\", "/");
                            string dstPath = Path.Combine(Directory.GetCurrentDirectory(), dep.pullToPath);
                            dstPath = Path.Combine(dstPath, relativePath);
                            dstPath = dstPath.Replace(@"\", "/");
                            FileInfo dstFi = new FileInfo(dstPath);
                            if(!dstFi.Directory.Exists)
                            {
                                dstFi.Directory.Create();
                            }

                            FileUtil.ReplaceFile(srcPath, dstPath);
                        }
                    }
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.Default);
        }

        private class Dependency
        {
            public string name;
            public string pullFromPath;
            public string pullToPath;
            public bool enabled;
        }

        private static string rootPath = "";
        private static bool autoPull = false;
        private static Dictionary<string, Dependency> dependencies = new Dictionary<string, Dependency>();

        public BuildDependenciesWindow()
        {
            AddDependency("UnityVS", "Framework.Unity.Editor/Assets/UnityVS", "Assets/UnityVS/", false);
            AddDependency("StrangeIoC", "libs/strangeioc/StrangeIoC/scripts/", "Assets/Ghostbit/StrangeIoC/Source/", false);
            AddDependency("Ash.Net Core", "libs/Ash.Net/Ash/Core/", "Assets/Ghostbit/Ash.Net/Source/Core/", false);
            AddDependency("Ash.Net Tools", "libs/Ash.Net/Ash/Tools/", "Assets/Ghostbit/Ash.Net/Source/Tools/", false);
            AddDependency("Tweaker.Core", "libs/TweakerUnity/Tweaker/Tweaker.Core/src/", "Assets/Ghostbit/Tweaker/Source/", false);
            AddDependency("TweakerUnity", "libs/TweakerUnity/Assets/Ghostbit/TweakerUnity/", "Assets/Ghostbit/TweakerUnity/", false);
            AddDependency("Framework.Core", "Framework.Core/src", "Assets/Ghostbit/Framework.Core/Source/", false);
            AddDependency("Framework.Unity", "Framework.Unity/Assets/Ghostbit/Framework.Unity/", "Assets/Ghostbit/Framework.Unity/", false);

            if(!ValidateDependencies())
            {
                EditorUtility.DisplayDialog("Validate Dependencies Failed",
                                            "Some dependencies had errors and have been disabled.",
                                            "Okay");
            }
        }

        public void AddDependency(string name, string pullFromPath, string pullToPath, bool enabled)
        {
            if (string.IsNullOrEmpty(name) || dependencies.ContainsKey(name))
            {
                EditorUtility.DisplayDialog("Invalid Dependency Name",
                                            "The dependency name '" + name + "' is invalid or already used. New dependency will not be added.",
                                            "Okay");
                return;
            }

            Dependency dep = new Dependency();
            dep.name = name;
            dep.pullFromPath = pullFromPath;
            dep.pullToPath = pullToPath;
            dep.enabled = enabled;
            dependencies.Add(name, dep);
        }

        private bool ValidateDependencies()
        {
            if(string.IsNullOrEmpty(rootPath))
            {
                EditorUtility.DisplayDialog("Root Path Not Set", 
                                            "The root path is not set. Please set the root path.",
                                            "Okay");
                return false;
            }

            if(!Directory.Exists(rootPath))
            {
                EditorUtility.DisplayDialog("Root Path Does Not Exist",
                                            "The root path does not exist. Please set the root path.",
                                            "Okay");
                return false;
            }

            bool error = false;

            foreach(Dependency dep in dependencies.Values)
            {
                error = !ValidateDependency(dep);
            }

            return !error;
        }

        private bool ValidateDependency(Dependency dep)
        {
            Debug.Log("ValidateDependency...");
            Debug.Log("Directory.Exists(" + Path.Combine(rootPath, dep.pullFromPath) + ") = " + Directory.Exists(Path.Combine(rootPath, dep.pullFromPath)));
            Debug.Log("Uri.IsWellFormedUriString(dep.pullFromPath, UriKind.RelativeOrAbsolute) = " + Uri.IsWellFormedUriString(dep.pullFromPath, UriKind.RelativeOrAbsolute));

            if (string.IsNullOrEmpty(dep.pullFromPath) ||
                !Uri.IsWellFormedUriString(dep.pullFromPath, UriKind.RelativeOrAbsolute) ||
                !Directory.Exists(Path.Combine(rootPath, dep.pullFromPath)))
            {
                EditorUtility.DisplayDialog("Invalid Dependency pullFromPath",
                                            "The path '" + dep.pullFromPath + "' is invalid. New dependency will not be added.",
                                            "Okay");
                dep.enabled = false;
                return false;
            }

            if (!string.IsNullOrEmpty(dep.pullToPath) &&
                !Uri.IsWellFormedUriString(dep.pullToPath, UriKind.RelativeOrAbsolute))
            {
                EditorUtility.DisplayDialog("Invalid Dependency pullToPath",
                                            "The path '" + dep.pullToPath + "' is invalid. New dependency will not be added.",
                                            "Okay");
                dep.enabled = false;
                return false;
            }

            return true;
        }

        void OnGUI()
        {
            GUILayout.Label("Options");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Root Path"))
            {
                rootPath = EditorUtility.OpenFolderPanel("Set Root Path", "", "");
                
            }
            GUILayout.Label(rootPath);
            GUILayout.EndHorizontal();
            autoPull = EditorGUILayout.Toggle("Auto Pull", autoPull);
            
            GUILayout.Label("Commands");
            MakeCommand("Pull", "Pull all dependencies into current project.", PullDependencies);

            GUILayout.Label("Dependencies");
            foreach (Dependency dep in dependencies.Values)
            {
                MakeDependency(dep);
            }
        }

        private void MakeDependency(Dependency dep)
        {
            dep.enabled = EditorGUILayout.Toggle(dep.name, dep.enabled);
            if(dep.enabled)
            {
                ValidateDependency(dep);
            }
        }

        private void MakeCommand(string cmd, string label, Action callback)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(cmd))
            {
                callback();
            }
            GUILayout.Label(label);
            GUILayout.EndHorizontal();
        }
    }
}