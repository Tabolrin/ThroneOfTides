using UnityEngine;
using UnityEngine.InputSystem;

// Assembly: ThroneOfTides.Systems
// Location: Scripts/Systems/VFX/VFXTester.cs
// Attach to: any GameObject in the scene alongside the VFX controllers
// Remove or strip from builds — development testing only.

namespace ThroneOfTides.Systems.VFX
{
    public class VFXTester : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private KrakenVFXController _krakenVFX;
        [SerializeField] private SirenVFXController  _sirenVFX;

        [Header("Test Position")]
        [SerializeField] private Vector3 _testWorldPosition = Vector3.zero;

        [Header("Inject — required for canvas positioning and particle rendering")]
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private Camera        _gameCamera;
        [SerializeField] private ParticleSystem _musicNoteParticles;

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _krakenVFX.Inject(_canvasRect, _gameCamera);
                _krakenVFX.StartSequence(_testWorldPosition);
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                _sirenVFX.Inject(_canvasRect, _gameCamera, _musicNoteParticles);
                _sirenVFX.StartSequence(_testWorldPosition);
            }
        }
    }
}