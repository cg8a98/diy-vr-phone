using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DiyVr.Updater
{
    /// <summary>
    /// Once per launch (Android builds only) asks GitHub for the latest release. If it is newer than this build,
    /// shows <see cref="UpdatePrompt"/>; on "install" downloads the APK, checks its size and starts Android's installer.
    /// Sends no token and no user data. Failures only log a warning.
    /// </summary>
    public sealed class UpdateChecker : MonoBehaviour
    {
        public const string LatestUrl = "https://api.github.com/repos/cg8a98/diy-vr-phone/releases/latest";
        public const string UserAgent = "diy-vr-phone-app";
        private const int CheckTimeoutSeconds = 10;
        private const int DownloadTimeoutSeconds = 900;

        private UpdatePrompt _prompt;
        private UpdateOffer _offer;
        private bool _busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<UpdateChecker>() != null) return;
            var go = new GameObject("UpdateChecker");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            go.AddComponent<UpdateChecker>();
#endif
        }

        private IEnumerator Start()
        {
            _prompt = gameObject.AddComponent<UpdatePrompt>();
            _prompt.InstallPressed += OnInstall;
            _prompt.LaterPressed += OnLater;

            using (var req = UnityWebRequest.Get(LatestUrl))
            {
                req.SetRequestHeader("User-Agent", UserAgent);
                req.SetRequestHeader("Accept", "application/vnd.github+json");
                req.timeout = CheckTimeoutSeconds;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Updater: check failed: " + req.result);
                    yield break;
                }
                _offer = UpdateLogic.TryGetOffer(req.downloadHandler.text, Application.version);
            }
            if (_offer == null) yield break;
            _prompt.Show(UpdateLogic.OfferText(_offer.Version), true);
        }

        private void OnLater()
        {
            _prompt.Hide();
        }

        private void OnInstall()
        {
            if (_busy || _offer == null) return;
            if (!UpdateInstaller.CanInstall())
            {
                UpdateInstaller.OpenUnknownSourcesSettings();
                _prompt.Show("One-time step: switch on \"Allow from this source\" for this app,\n" +
                             "then come back and press A (or tap) again. B: later", true);
                return;
            }
            StartCoroutine(DownloadAndInstall(_offer));
        }

        private IEnumerator DownloadAndInstall(UpdateOffer offer)
        {
            _busy = true;
            _prompt.Show("Downloading v" + offer.Version + "...", false);

            string dir = Path.Combine(Application.temporaryCachePath, "updates");
            string path = Path.Combine(dir, "diy-vr-" + offer.Version + ".apk");
            bool ok = false;
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
            }
            catch (System.Exception e) { Debug.LogWarning("Updater: cache folder failed: " + e.Message); }

            using (var req = UnityWebRequest.Get(offer.Url))
            {
                req.SetRequestHeader("User-Agent", UserAgent);
                req.timeout = DownloadTimeoutSeconds;
                req.downloadHandler = new DownloadHandlerFile(path) { removeFileOnAbort = true };
                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    _prompt.Show("Downloading v" + offer.Version + "... " + Mathf.RoundToInt(req.downloadProgress * 100f) + "%", false);
                    yield return null;
                }
                if (req.result == UnityWebRequest.Result.Success)
                {
                    long got = File.Exists(path) ? new FileInfo(path).Length : -1;
                    ok = got == offer.Size;
                    if (!ok) Debug.LogWarning("Updater: size mismatch, expected " + offer.Size + " got " + got);
                }
                else Debug.LogWarning("Updater: download failed: " + req.result);
            }

            _busy = false;
            if (ok && UpdateInstaller.Install(path))
            {
                _prompt.Hide();
                yield break;
            }
            try { if (File.Exists(path)) File.Delete(path); } catch (System.Exception) { }
            _prompt.Show("Update failed. Check the Wi-Fi.\nA: try again, B: later", true);
        }
    }
}
