using System;
using UnityEngine;
using UnityEngine.Events;

namespace DiyVr.Interaction
{
    /// <summary>
    /// Marks an object (with a collider) that the gaze pointer can target. Raises enter / exit / select events
    /// and tints its renderers while looked at.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Gazeable : MonoBehaviour
    {
        public UnityEvent onGazeEnter = new UnityEvent();
        public UnityEvent onGazeExit = new UnityEvent();
        public UnityEvent onSelect = new UnityEvent();

        public event Action<Gazeable> GazeEntered;
        public event Action<Gazeable> GazeExited;
        public event Action<Gazeable> Selected;

        [Tooltip("Tint applied while looked at. Turn off for objects that draw their own feedback.")]
        public bool highlight = true;
        public Color highlightColor = new Color(0.4f, 1f, 0.5f, 1f);

        public bool IsGazedAt { get; private set; }

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _block;
        private Renderer[] _renderers;

        public void GazeEnter()
        {
            IsGazedAt = true;
            if (highlight) SetTint(true);
            onGazeEnter.Invoke();
            if (GazeEntered != null) GazeEntered(this);
        }

        public void GazeExit()
        {
            IsGazedAt = false;
            SetTint(false);
            onGazeExit.Invoke();
            if (GazeExited != null) GazeExited(this);
        }

        /// <summary>The "click": called by the pinch ticket (or anything else) on the current target.</summary>
        public void Select()
        {
            onSelect.Invoke();
            if (Selected != null) Selected(this);
        }

        private void OnDisable()
        {
            if (IsGazedAt) GazeExit();
        }

        private void SetTint(bool on)
        {
            if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>();
            if (_block == null) _block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                if (on)
                {
                    r.GetPropertyBlock(_block);
                    _block.SetColor(ColorId, highlightColor);
                    _block.SetColor(BaseColorId, highlightColor);
                    r.SetPropertyBlock(_block);
                }
                else
                {
                    r.SetPropertyBlock(null);
                }
            }
        }
    }
}
