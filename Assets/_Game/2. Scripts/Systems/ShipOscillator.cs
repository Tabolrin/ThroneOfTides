using DG.Tweening;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public class ShipOscillator : MonoBehaviour
    {
        [Header("Vertical bob")]
        [SerializeField] private bool _shouldDriftVert = true;
        [SerializeField] private float _yAmplitude = 0.3f;
        [SerializeField] private float _yDuration  = 2.5f;

        [Header("Horizontal drift")]
        [SerializeField] private bool _shouldDriftHoriz = true;
        [SerializeField] private float _xAmplitude = 0.1f;
        [SerializeField] private float _xDuration  = 3.8f;

        [Header("Rotation sway")]
        [SerializeField] private bool _shouldRotate = false;
        [SerializeField] private float _rotAmplitude = 1.5f;
        [SerializeField] private float _rotDuration  = 3.0f;

        private Vector3 _startPos;

        private void Start()
        {
            _startPos = transform.localPosition;
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

        private void OnDestroy() => transform.DOKill();
    }
}