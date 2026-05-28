using System.Collections;
using DG.Tweening;
using MoreMountains.Feedbacks;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.Systems
{
    public class CardVFXHandler : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Canvas & Camera (injected into creature prefabs at runtime)")]
        [SerializeField] private RectTransform _gameCanvasRect;
        [SerializeField] private Camera        _gameCamera;

        [Header("Spawn Points")]
        [SerializeField] private Transform _playerShipHitPoint;
        [SerializeField] private Transform _enemyShipHitPoint;
        [SerializeField] private Transform _playerShipSpawnPoint;
        [SerializeField] private Transform _playerDeckPoint;

        [Header("Cannonball")]
        [SerializeField] private GameObject _cannonballPrefab;
        [SerializeField] private float      _cannonballDuration  = 0.4f;
        [SerializeField] private float      _cannonballArcHeight = 1.5f;

        [Header("VFX Prefabs — Weapon")]
        [SerializeField] private GameObject _hitImpactStandardPrefab;
        [SerializeField] private GameObject _hitImpactExplosionPrefab;
        [SerializeField] private GameObject _hailStormPrefab;
        [SerializeField] private GameObject _lightningPrefab;
        [SerializeField] private GameObject _whirlpoolPrefab;
        [SerializeField] private GameObject _tidalWavePrefab;
        [SerializeField] private GameObject _gunpowderBarrelPrefab;
        [SerializeField] private GameObject _torchPrefab;
        [SerializeField] private GameObject _torchComboResolvePrefab;
        [SerializeField] private GameObject _krakenPrefab;
        [SerializeField] private GameObject _boardingPartyPrefab;

        [Header("VFX Prefabs — Action")]
        [SerializeField] private GameObject _sirenSongPrefab;
        [SerializeField] private GameObject _reconParrotPrefab;
        [SerializeField] private GameObject _highSpiritsPrefab;
        [SerializeField] private GameObject _deadMansTurnPrefab;
        [SerializeField] private GameObject _lockerReturnPrefab;
        [SerializeField] private GameObject _monkeyGrabPrefab;

        [Header("FEEL — Hit")]
        [SerializeField] private MMF_Player _feedbackLightHit;
        [SerializeField] private MMF_Player _feedbackMediumHit;
        [SerializeField] private MMF_Player _feedbackHeavyHit;

        [Header("FEEL — Combat")]
        [SerializeField] private MMF_Player _feedbackComboResolve;
        [SerializeField] private MMF_Player _feedbackDOTTick;
        [SerializeField] private MMF_Player _feedbackHeal;
        [SerializeField] private MMF_Player _feedbackLightningFlash;

        [Header("FEEL — Card")]
        [SerializeField] private MMF_Player _feedbackCardDraw;
        [SerializeField] private MMF_Player _feedbackCardPlay;
        [SerializeField] private MMF_Player _feedbackEndTurnPulse;
        [SerializeField] private MMF_Player _feedbackComboStackIncrement;
        [SerializeField] private MMF_Player _feedbackPlayZoneGlow;

        [Header("FEEL — Match")]
        [SerializeField] private MMF_Player _feedbackWin;
        [SerializeField] private MMF_Player _feedbackLoss;

        [Header("FEEL — Damage Numbers")]
        [SerializeField] private MMF_Player _feedbackDamageNumber;

        [Header("Timing")]
        [SerializeField] private float _vfxLifetime      = 2f;
        [SerializeField] private float _winSlowDuration  = 0.8f;
        [SerializeField] private float _lossSlowDuration = 0.5f;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            GameEventBus.OnDamageDealt       += OnDamageDealt;
            GameEventBus.OnCardPlayed        += OnCardPlayed;
            GameEventBus.OnCardPlayAccepted  += OnCardPlayAccepted;
            GameEventBus.OnCardDrawn         += OnCardDrawn;
            GameEventBus.OnComboResolved     += OnComboResolved;
            GameEventBus.OnComboStackChanged += OnComboStackChanged;
            GameEventBus.OnDOTTick           += OnDOTTick;
            GameEventBus.OnMatchWin          += OnMatchWin;
            GameEventBus.OnMatchLoss         += OnMatchLoss;
            GameEventBus.OnTurnPhaseChanged  += OnTurnPhaseChanged;
        }

        private void OnDisable()
        {
            GameEventBus.OnDamageDealt       -= OnDamageDealt;
            GameEventBus.OnCardPlayed        -= OnCardPlayed;
            GameEventBus.OnCardPlayAccepted  -= OnCardPlayAccepted;
            GameEventBus.OnCardDrawn         -= OnCardDrawn;
            GameEventBus.OnComboResolved     -= OnComboResolved;
            GameEventBus.OnComboStackChanged -= OnComboStackChanged;
            GameEventBus.OnDOTTick           -= OnDOTTick;
            GameEventBus.OnMatchWin          -= OnMatchWin;
            GameEventBus.OnMatchLoss         -= OnMatchLoss;
            GameEventBus.OnTurnPhaseChanged  -= OnTurnPhaseChanged;
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void OnDamageDealt(DamageTarget target, int amount)
        {
            Transform hitPoint = GetHitPoint(target);

            _feedbackDamageNumber?.PlayFeedbacks(hitPoint.position, amount);

            if (amount >= 8)
            {
                SpawnVFX(_hitImpactExplosionPrefab, hitPoint.position);
                _feedbackHeavyHit?.PlayFeedbacks();
            }
            else if (amount >= 4)
            {
                SpawnVFX(_hitImpactStandardPrefab, hitPoint.position);
                _feedbackMediumHit?.PlayFeedbacks();
            }
            else if (amount > 0)
            {
                SpawnVFX(_hitImpactStandardPrefab, hitPoint.position);
                _feedbackLightHit?.PlayFeedbacks();
            }
        }

        private void OnCardPlayed(ICard card)        => _feedbackCardPlay?.PlayFeedbacks();
        private void OnCardDrawn(ICard card)          => _feedbackCardDraw?.PlayFeedbacks();
        private void OnComboResolved()                => _feedbackComboResolve?.PlayFeedbacks();
        private void OnMatchWin()                     => StartCoroutine(WinSequence());
        private void OnMatchLoss()                    => StartCoroutine(LossSequence());

        private void OnComboStackChanged(int count)
        {
            if (count > 0) _feedbackComboStackIncrement?.PlayFeedbacks();
        }

        private void OnTurnPhaseChanged(TurnPhase phase)
        {
            if (phase == TurnPhase.Draw) _feedbackEndTurnPulse?.PlayFeedbacks();
        }

        private void OnDOTTick(DotEffect effect)
        {
            SpawnVFX(_hailStormPrefab, GetHitPoint(effect.Target).position);
            _feedbackDOTTick?.PlayFeedbacks();
        }

        private void OnCardPlayAccepted(ICard card)
        {
            var cardSO = card as CardSO;
            if (cardSO == null) return;
            StartCoroutine(PlayCardVFX(cardSO));
        }

        // ── Card VFX Routing ──────────────────────────────────────────────────

        private IEnumerator PlayCardVFX(CardSO card)
        {
            Transform source = _playerShipHitPoint;
            Transform target = _enemyShipHitPoint;

            switch (card.Name)
            {
                case "Cannonball":
                case "Grape Shot":
                case "Pistol":
                case "Chain Shot":
                case "Ballista":
                case "Anchor Drag":
                case "Whale Ram":
                case "Mega Cannon":
                    yield return StartCoroutine(FireCannonball(source.position, target.position));
                    break;

                case "Lightning":
                    SpawnVFX(_lightningPrefab, target.position);
                    _feedbackLightningFlash?.PlayFeedbacks();
                    break;

                case "Hail Storm":   SpawnVFX(_hailStormPrefab,       target.position); break;
                case "Whirlpool":    SpawnVFX(_whirlpoolPrefab,        target.position); break;
                case "Tidal Wave":   SpawnVFX(_tidalWavePrefab,        target.position); break;

                case "Gunpowder Barrel":
                    SpawnVFX(_gunpowderBarrelPrefab, target.position);
                    break;

                case "Torch":
                    // Use combo-resolve variant when available; falls back to standard torch.
                    SpawnVFX(_torchComboResolvePrefab != null ? _torchComboResolvePrefab : _torchPrefab,
                             target.position);
                    break;

                case "The Kraken":
                    // Kraken always targets the enemy ship.
                    HandleCreatureVFX(_krakenPrefab, DamageTarget.Enemy);
                    break;

                case "Boarding Party": SpawnVFX(_boardingPartyPrefab, target.position); break;

                case "Siren Song":
                    // Siren targets the enemy ship — the enchantment is cast upon them.
                    HandleCreatureVFX(_sirenSongPrefab, DamageTarget.Enemy);
                    break;

                case "Recon Parrot":    SpawnVFX(_reconParrotPrefab,  target.position); break;

                case "High Spirits":
                    SpawnVFX(_highSpiritsPrefab, source.position);
                    _feedbackHeal?.PlayFeedbacks();
                    break;

                case "Dead Man's Turn": SpawnVFX(_deadMansTurnPrefab, source.position); break;
                case "Locker's Return": SpawnVFX(_lockerReturnPrefab, source.position); break;
                case "Monkey Grab":     SpawnVFX(_monkeyGrabPrefab,   target.position); break;
            }
        }

        // ── Creature VFX ──────────────────────────────────────────────────────

        // Shared spawn, inject, and event-wire path for any creature VFX controller.
        // Creature prefabs appear to the left of the target ship via _canvasSpawnOffset
        // on the controller — adjust that field per prefab in the Inspector.
        private void HandleCreatureVFX(GameObject prefab, DamageTarget target)
        {
            if (prefab == null) return;

            // Instantiate under the canvas so the RectTransform resolves correctly.
            var instance = Instantiate(prefab, _gameCanvasRect);

            Vector3 worldPosition = GetHitPoint(target).position;

            if (prefab == _krakenPrefab)
            {
                var controller = instance.GetComponent<VFX.KrakenVFXController>();
                if (controller == null) return;

                controller.Inject(_gameCanvasRect, _gameCamera);
                controller.OnAttackMoment += GameEventBus.FireKrakenAttackMoment;
                controller.OnSequenceEnd  += () => Destroy(instance);
                controller.StartSequence(worldPosition);
            }
            else if (prefab == _sirenSongPrefab)
            {
                var controller = instance.GetComponent<VFX.SirenVFXController>();
                if (controller == null) return;

                controller.Inject(_gameCanvasRect, _gameCamera);
                controller.OnSirenReady  += GameEventBus.FireSirenSongActive;
                controller.OnSequenceEnd += () => Destroy(instance);
                controller.StartSequence(worldPosition);
            }
        }

        // ── VFX Sequences ─────────────────────────────────────────────────────

        private IEnumerator FireCannonball(Vector3 from, Vector3 to)
        {
            if (_cannonballPrefab == null) yield break;

            GameObject ball = Instantiate(_cannonballPrefab, from, Quaternion.identity);
            Vector3    mid  = Vector3.Lerp(from, to, 0.5f) + Vector3.up * _cannonballArcHeight;

            float elapsed = 0f;
            while (elapsed < _cannonballDuration)
            {
                elapsed                 += Time.deltaTime;
                float   t                = Mathf.Clamp01(elapsed / _cannonballDuration);
                Vector3 a                = Vector3.Lerp(from, mid, t);
                Vector3 b                = Vector3.Lerp(mid,  to,  t);
                ball.transform.position  = Vector3.Lerp(a, b, t);
                yield return null;
            }

            Destroy(ball);
        }

        private IEnumerator WinSequence()
        {
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(_winSlowDuration);
            Time.timeScale = 1f;
            _feedbackWin?.PlayFeedbacks();
        }

        private IEnumerator LossSequence()
        {
            Time.timeScale = 0.5f;
            yield return new WaitForSecondsRealtime(_lossSlowDuration);
            Time.timeScale = 1f;
            _feedbackLoss?.PlayFeedbacks();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Transform GetHitPoint(DamageTarget target) =>
            target == DamageTarget.Player ? _playerShipHitPoint : _enemyShipHitPoint;

        private void SpawnVFX(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return;
            Destroy(Instantiate(prefab, position, Quaternion.identity), _vfxLifetime);
        }
    }
}