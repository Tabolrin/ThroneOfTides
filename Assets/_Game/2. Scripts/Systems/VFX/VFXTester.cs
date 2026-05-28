using UnityEngine;
using UnityEngine.InputSystem;

namespace ThroneOfTides.Systems.VFX
{
    public class VFXTester : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private KrakenVFXController _krakenVFX;
        [SerializeField] private SirenVFXController  _sirenVFX;

        [Header("Test Position")]
        [SerializeField] private Vector3 _testWorldPosition = Vector3.zero;

        [Header("Inject — required for Siren and Kraken canvas positioning")]
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private Camera        _gameCamera;

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _krakenVFX.Inject(_canvasRect, _gameCamera);
                _krakenVFX.StartSequence(_testWorldPosition);
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                _sirenVFX.Inject(_canvasRect, _gameCamera);
                _sirenVFX.StartSequence(_testWorldPosition);
            }
        }
    }
}