using DG.Tweening;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    // Continuous idle oscillation simulating a ship sailing on the ocean
    public class ShipOscillator : MonoBehaviour
    {
        [Header("Vertical bob")]
        [SerializeField] private float _yAmplitude = 0.3f;
        [SerializeField] private float _yDuration  = 2.5f;

        [Header("Horizontal drift")]
        [SerializeField] private float _xAmplitude = 0.1f;
        [SerializeField] private float _xDuration  = 3.8f;

        [Header("Rotation sway")]
        [SerializeField] private float _rotAmplitude = 1.5f;
        [SerializeField] private float _rotDuration  = 3.0f;

        private Vector3 _startPos;
        private Vector3 _startRot;

        private void Start()
        {
            _startPos = transform.localPosition;
            _startRot = transform.localEulerAngles;

            StartOscillation();
        }

        private void StartOscillation()
        {
            // Each axis uses a different duration for organic non-repeating motion
            transform.DOLocalMoveY(_startPos.y + _yAmplitude, _yDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            transform.DOLocalMoveX(_startPos.x + _xAmplitude, _xDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            transform.DOLocalRotate(new Vector3(0, 0, _rotAmplitude), _rotDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}