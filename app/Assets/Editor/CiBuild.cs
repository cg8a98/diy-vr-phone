using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DiyVr.EditorTools
{
    /// <summary>CI entry point: applies ProjectSetup, then builds the Android APK.</summary>
    public static class CiBuild
    {
        static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        public static void BuildAndroid()
        {
            ProjectSetup.Configure();
            // GameCI passes the full output file path as -customBuildPath.
            var file = GetArg("-customBuildPath");
            if (string.IsNullOrEmpty(file)) file = System.IO.Path.GetFullPath("../build/Android/diy-vr.apk");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = file,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Build failed: " + report.summary.result);
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("APK written to " + file);
        }
    }
}
