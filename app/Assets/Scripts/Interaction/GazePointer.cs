using UnityEngine;

namespace DiyVr.Interaction
{
    /// <summary>
    /// Casts a ray along the head's forward direction every frame, tracks the <see cref="Gazeable"/> it hits
    /// and draws a ~1 degree reticle at the hit point (or 2 m ahead when nothing is hit).
    /// Put on the StereoRig object (GazeBootstrap does this). Pinch will "click" <see cref="CurrentTarget"/>.
    /// </summary>
    public sealed class GazePointer : MonoBehaviour
    {
        public float maxDistance = 20f;
        public float reticleDegrees = GazeMath.DefaultReticleDegrees;
        public float missDistance = GazeMath.DefaultMissDistance;
        public LayerMask hitMask = ~0;
        public Color reticleColor = Color.white;

        /// <summary>The Gazeable currently looked at, or null.</summary>
        public Gazeable CurrentTarget { get { return _switch.Current != null ? _switch.Current : null; } }
        /// <summary>World point where the gaze ray hit; head position + missDistance forward when nothing was hit.</summary>
        public Vector3 HitPoint { get; private set; }
        /// <summary>True when the ray hit any collider this frame.</summary>
        public bool HasHit { get; private set; }

        private readonly GazeTargetSwitch<Gazeable> _switch = new GazeTargetSwitch<Gazeable>();
        private Transform _head;
        private Transform _reticle;
        private Material _material;

        private void Awake()
        {
            _head = transform;
            BuildReticle();
        }

        private void LateUpdate()
        {
            Vector3 origin = _head.position;
            Vector3 dir = _head.forward;
            RaycastHit hit;
            HasHit = Physics.Raycast(origin, dir, out hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore);

            Gazeable target = null;
            if (HasHit)
            {
                target = hit.collider.GetComponentInParent<Gazeable>();
                if (target != null && !target.isActiveAndEnabled) target = null;
            }
            Switch(target);

            float dist = GazeMath.ReticleDistance(HasHit, hit.distance, missDistance);
            HitPoint = origin + dir * dist;
            PlaceReticle(origin, dist);
        }

        private void Switch(Gazeable target)
        {
            Gazeable exited, entered;
            if (!_switch.Update(target, out exited, out entered)) return;
            // A destroyed object compares equal to null in Unity, so these checks also skip dead targets.
            if (exited != null) exited.GazeExit();
            if (entered != null) entered.GazeEnter();
        }

        private void PlaceReticle(Vector3 origin, float dist)
        {
            if (_reticle == null) return;
            _reticle.position = origin + _head.forward * dist;
            // Same orientation as the head, so the quad faces the viewer (eyes sit only ~3 cm either side).
            _reticle.rotation = _head.rotation;
            float s = GazeMath.ReticleScale(dist, reticleDegrees);
            _reticle.localScale = new Vector3(s, s, 1f);
        }

        private void BuildReticle()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "GazeReticle";
            // Default layer: the eye cameras only exclude layers 29/30/31, so both eyes draw it.
            go.layer = 0;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col); // must never be hit by its own ray
            _reticle = go.transform;

            Shader shader = Resources.Load<Shader>("ReticleOverlay");
            if (shader == null) shader = Shader.Find("DiyVr/ReticleOverlay");
            if (shader == null) shader = Shader.Find("DiyVr/StereoUnlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            _material = new Material(shader) { name = "GazeReticleMat", color = reticleColor };
            _material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private void OnEnable()
        {
            if (_reticle != null) _reticle.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (_reticle != null) _reticle.gameObject.SetActive(false);
            Gazeable exited, entered;
            if (_switch.Update(null, out exited, out entered) && exited != null) exited.GazeExit();
        }

        private void OnDestroy()
        {
            if (_reticle != null) Destroy(_reticle.gameObject);
            if (_material != null) Destroy(_material);
        }
    }
}
