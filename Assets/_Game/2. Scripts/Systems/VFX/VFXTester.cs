using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Assembly: ThroneOfTides.Systems
// Location: Scripts/Systems/VFX/VFXTester.cs
// Attach to: any GameObject in the scene alongside the VFX controllers
// Remove or strip from builds — development testing only.

namespace ThroneOfTides.Systems.VFX
{
    public class VFXTester : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private KrakenVFXController     _krakenVFX;
        [SerializeField] private SirenVFXController      _sirenVFX;
        [SerializeField] private LightningVFXController  _lightningVFX;
        [SerializeField] private HailstormVFXController  _hailstormVFX;

        [Header("Test Position")]
        [SerializeField] private Vector3 _testWorldPosition = Vector3.zero;

        [Header("Shared Inject — required for canvas positioning")]
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private Camera        _gameCamera;

        [Header("Siren Inject")]
        [SerializeField] private ParticleSystem _musicNoteParticles;

        [Header("Lightning Inject")]
        [SerializeField] private ParticleSystem _strikeParticles;
        [SerializeField] private Image          _whiteoutImage;

        [Header("Hailstorm Inject")]
        [SerializeField] private ParticleSystem _hailParticles;

        private void Update()
        {
            // Space — Kraken attack
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _krakenVFX.Inject(_canvasRect, _gameCamera);
                _krakenVFX.StartSequence(_testWorldPosition);
            }

            // S — Siren song
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                _sirenVFX.Inject(_canvasRect, _gameCamera, _musicNoteParticles);
                _sirenVFX.StartSequence(_testWorldPosition);
            }

            // L — Lightning strike
            if (Keyboard.current.lKey.wasPressedThisFrame)
            {
                _lightningVFX.Inject(_canvasRect, _gameCamera, _strikeParticles, _whiteoutImage);
                _lightningVFX.StartSequence(_testWorldPosition);
            }

            // H — Hailstorm
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                _hailstormVFX.Inject(_canvasRect, _gameCamera, _hailParticles);
                _hailstormVFX.StartSequence(_testWorldPosition);
            }
        }
    }
}