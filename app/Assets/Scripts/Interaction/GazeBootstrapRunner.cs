using DiyVr.Stereo;
using UnityEngine;

namespace DiyVr.Interaction
{
    /// <summary>Retries for up to ~5 s (300 frames): attach GazePointer to the StereoRig, and make the test cube targetable.</summary>
    public sealed class GazeBootstrapRunner : MonoBehaviour
    {
        public const string TestCubeName = "TestCube";
        private const int MaxFrames = 300;

        private int _frames;
        private bool _rigDone;
        private bool _cubeDone;

        private void Update()
        {
            _frames++;
            if (!_rigDone)
            {
                var rig = FindFirstObjectByType<StereoRig>();
                if (rig != null)
                {
                    if (rig.GetComponent<GazePointer>() == null) rig.gameObject.AddComponent<GazePointer>();
                    _rigDone = true;
                }
            }
            if (!_cubeDone)
            {
                // StereoTestContent destroys the cube's collider (it is display-only there), so add one here.
                var cube = GameObject.Find(TestCubeName);
                if (cube != null)
                {
                    if (cube.GetComponent<Collider>() == null) cube.AddComponent<BoxCollider>();
                    if (cube.GetComponent<Gazeable>() == null) cube.AddComponent<Gazeable>();
                    _cubeDone = true;
                }
            }
            if ((_rigDone && _cubeDone) || _frames >= MaxFrames) Destroy(gameObject);
        }
    }
}
