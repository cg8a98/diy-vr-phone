using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DiyVr.EditorTools
{
    /// <summary>CI entry point: applies ProjectSetup, then builds the Android APK.</summary>
    public static class CiBuild
    {
        public static void BuildAndroid()
        {
            ProjectSetup.Configure();
            var path = Environment.GetEnvironmentVariable("BUILD_PATH_FULL");
            if (string.IsNullOrEmpty(path)) path = "build/Android";
            System.IO.Directory.CreateDirectory(path);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = path + "/diy-vr.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Build failed: " + report.summary.result);
                EditorApplication.Exit(1);
            }
        }
    }
}
