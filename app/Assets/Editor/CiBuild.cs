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

        // GameCI passes these as command-line args (and sometimes env vars); accept both.
        static string GetArgOrEnv(string arg, string env)
        {
            var v = GetArg(arg);
            return string.IsNullOrEmpty(v) ? Environment.GetEnvironmentVariable(env) : v;
        }

        static void ApplyVersion()
        {
            var name = GetArgOrEnv("-buildVersion", "VERSION");
            if (!string.IsNullOrEmpty(name)) PlayerSettings.bundleVersion = name;
            var code = GetArgOrEnv("-androidVersionCode", "ANDROID_VERSION_CODE");
            if (int.TryParse(code, out var c) && c > 0) PlayerSettings.Android.bundleVersionCode = c;
            Debug.Log("Version " + PlayerSettings.bundleVersion + " code " + PlayerSettings.Android.bundleVersionCode);
        }

        // Release signing. A custom buildMethod bypasses GameCI's own Builder.cs, so we apply the keystore here.
        // No keystore name given (pull requests) -> debug signing. A name given but file/passwords missing -> fail.
        static bool ApplySigning()
        {
            var name = GetArgOrEnv("-androidKeystoreName", "ANDROID_KEYSTORE_NAME");
            if (string.IsNullOrEmpty(name))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("No keystore configured: debug-signed build.");
                return true;
            }
            var path = System.IO.Path.GetFullPath(name);
            var storePass = GetArgOrEnv("-androidKeystorePass", "ANDROID_KEYSTORE_PASS");
            var alias = GetArgOrEnv("-androidKeyaliasName", "ANDROID_KEYALIAS_NAME");
            var aliasPass = GetArgOrEnv("-androidKeyaliasPass", "ANDROID_KEYALIAS_PASS");
            if (!System.IO.File.Exists(path) || string.IsNullOrEmpty(storePass) || string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(aliasPass))
            {
                Debug.LogError("Release signing requested but keystore file or passwords/alias are missing (" + path + ").");
                return false;
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = path;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = aliasPass;
            Debug.Log("Release signing enabled (alias " + alias + ").");
            return true;
        }

        public static void BuildAndroid()
        {
            ProjectSetup.Configure();
            ApplyVersion();
            if (!ApplySigning()) { EditorApplication.Exit(1); return; }
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
