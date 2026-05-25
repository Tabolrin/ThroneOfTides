using DG.Tweening;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public class ShipOscillator : MonoBehaviour
    {
        [Header("Vertical bob")]
        [SerializeField] private bool  _shouldDriftVert = true;
        [SerializeField] private float _yAmplitude = 0.3f;
        [SerializeField] private float _yDuration  = 2.5f;

        [Header("Horizontal drift")]
        [SerializeField] private bool  _shouldDriftHoriz = true;
        [SerializeField] private float _xAmplitude = 0.1f;
        [SerializeField] private float _xDuration  = 3.8f;

        [Header("Rotation sway")]
        [SerializeField] private bool  _shouldRotate   = false;
        [SerializeField] private float _rotAmplitude   = 1.5f;
        [SerializeField] private float _rotDuration    = 3.0f;

        // Resting transform cached before any tween runs.
        // RestartOscillation snaps back to these values so amplitude is always
        // measured from the correct origin regardless of where the tween was interrupted.
        private Vector3    _startPos;
        private Quaternion _startRot;

        private void Start()
        {
            _startPos = transform.localPosition;
            _startRot = transform.localRotation;
            StartOscillation();
        }

        // Kills all active tweens on this transform, resets to the resting pose,
        // then restarts the oscillation sequence with current serialized values.
        // Called by ShipOscillatorOverlay during Play Mode live-tuning.
        public void RestartOscillation()
        {
            transform.DOKill();
            transform.localPosition = _startPos;
            transform.localRotation = _startRot;
            StartOscillation();
        }

        private void StartOscillation()
        {
            // Different durations per axis create organic non-repeating motion
            if (_shouldDriftVert)
            {
                transform.DOLocalMoveY(_startPos.y + _yAmplitude, _yDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            if (_shouldDriftHoriz)
            {
                transform.DOLocalMoveX(_startPos.x + _xAmplitude, _xDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            if (_shouldRotate)
            {
                transform.DOLocalRotate(new Vector3(0, 0, _rotAmplitude), _rotDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        // DOKill targets this transform specifically, leaving any tweens on
        // child objects (e.g. card animations) unaffected.
        private void OnDestroy() => transform.DOKill();
    }
}