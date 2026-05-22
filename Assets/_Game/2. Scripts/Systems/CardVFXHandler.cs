using System.Collections;
using DG.Tweening;
using MoreMountains.Feedbacks;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    // Subscribes to GameEventBus events and triggers VFX and FEEL feedbacks
    // All slots serialized - assign prefabs and MMF_Players in Inspector
    // No hardcoded asset names - fully data driven
    public class CardVFXHandler : MonoBehaviour
    {
        [Header("Spawn Points")]
        [SerializeField] private Transform _playerShipHitPoint;
        [SerializeField] private Transform _enemyShipHitPoint;
        [SerializeField] private Transform _playerDeckPoint;

        [Header("Cannonball")]
        [SerializeField] private GameObject _cannonballPrefab;
        [SerializeField] private float      _cannonballDuration = 0.4f;
        [SerializeField] private float      _cannonballArcHeight = 1.5f;

        [Header("VFX Prefabs - Weapon")]
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

        [Header("VFX Prefabs - Action")]
        [SerializeField] private GameObject _sirenSongPrefab;
        [SerializeField] private GameObject _reconParrotPrefab;
        [SerializeField] private GameObject _highSpiritsPrefab;
        [SerializeField] private GameObject _deadMansTurnPrefab;
        [SerializeField] private GameObject _lockerReturnPrefab;
        [SerializeField] private GameObject _monkeyGrabPrefab;

        [Header("FEEL - Hit Feedbacks")]
        [SerializeField] private MMF_Player _feedbackLightHit;
        [SerializeField] private MMF_Player _feedbackMediumHit;
        [SerializeField] private MMF_Player _feedbackHeavyHit;

        [Header("FEEL - Combat Feedbacks")]
        [SerializeField] private MMF_Player _feedbackComboResolve;
        [SerializeField] private MMF_Player _feedbackDOTTick;
        [SerializeField] private MMF_Player _feedbackHeal;
        [SerializeField] private MMF_Player _feedbackLightningFlash;

        [Header("FEEL - Card Feedbacks")]
        [SerializeField] private MMF_Player _feedbackCardDraw;
        [SerializeField] private MMF_Player _feedbackCardPlay;
        [SerializeField] private MMF_Player _feedbackEndTurnPulse;
        [SerializeField] private MMF_Player _feedbackComboStackIncrement;
        [SerializeField] private MMF_Player _feedbackPlayZoneGlow;

        [Header("FEEL - Match Feedbacks")]
        [SerializeField] private MMF_Player _feedbackWin;
        [SerializeField] private MMF_Player _feedbackLoss;

        [Header("FEEL - Damage Numbers")]
        [SerializeField] private MMF_Player _feedbackDamageNumber;

        [Header("Timing")]
        [SerializeField] private float _vfxLifetime      = 2f;
        [SerializeField] private float _winSlowDuration  = 0.8f;
        [SerializeField] private float _lossSlowDuration = 0.5f;

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

        // --- Event Handlers ---

        private void OnDamageDealt(DamageTarget target, int amount)
        {
            Transform hitPoint = target == DamageTarget.Player
                ? _playerShipHitPoint
                : _enemyShipHitPoint;

            // Damage number popup
            if (_feedbackDamageNumber != null)
                _feedbackDamageNumber.PlayFeedbacks(hitPoint.position, amount);

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

        private void OnCardPlayed(ICard card) =>
            _feedbackCardPlay?.PlayFeedbacks();

        private void OnCardPlayAccepted(ICard card)
        {
            var cardSO = card as CardSO;
            if (cardSO == null) return;
            StartCoroutine(PlayCardVFX(cardSO, DamageTarget.Enemy));
        }

        private void OnCardDrawn(ICard card) =>
            _feedbackCardDraw?.PlayFeedbacks();

        private void OnComboResolved() =>
            _feedbackComboResolve?.PlayFeedbacks();

        private void OnComboStackChanged(int count)
        {
            if (count > 0)
                _feedbackComboStackIncrement?.PlayFeedbacks();
        }

        private void OnDOTTick(DotEffect effect)
        {
            Transform hitPoint = effect.Target == DamageTarget.Player
                ? _playerShipHitPoint
                : _enemyShipHitPoint;

            SpawnVFX(_hailStormPrefab, hitPoint.position);
            _feedbackDOTTick?.PlayFeedbacks();
        }

        private void OnTurnPhaseChanged(TurnPhase phase)
        {
            if (phase == TurnPhase.Draw)
                _feedbackEndTurnPulse?.PlayFeedbacks();
        }

        private void OnMatchWin()  => StartCoroutine(WinSequence());
        private void OnMatchLoss() => StartCoroutine(LossSequence());

        // --- VFX Sequences ---

        private IEnumerator PlayCardVFX(CardSO card, DamageTarget target)
        {
            Transform sourcePoint = target == DamageTarget.Enemy
                ? _playerShipHitPoint
                : _enemyShipHitPoint;
            Transform targetPoint = target == DamageTarget.Enemy
                ? _enemyShipHitPoint
                : _playerShipHitPoint;

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
                    yield return StartCoroutine(
                        FireCannonball(sourcePoint.position, targetPoint.position));
                    break;

                case "Lightning":
                    SpawnVFX(_lightningPrefab, targetPoint.position);
                    _feedbackLightningFlash?.PlayFeedbacks();
                    break;

                case "Hail Storm":
                    SpawnVFX(_hailStormPrefab, targetPoint.position);
                    break;

                case "Whirlpool":
                    SpawnVFX(_whirlpoolPrefab, targetPoint.position);
                    break;

                case "Tidal Wave":
                    SpawnVFX(_tidalWavePrefab, targetPoint.position);
                    break;

                case "Gunpowder Barrel":
                    SpawnVFX(_gunpowderBarrelPrefab, targetPoint.position);
                    break;

                case "Torch":
                    // Use combo resolve VFX if combo was active
                    SpawnVFX(_torchComboResolvePrefab ?? _torchPrefab, targetPoint.position);
                    break;

                case "The Kraken":
                    SpawnVFX(_krakenPrefab, targetPoint.position);
                    break;

                case "Boarding Party":
                    SpawnVFX(_boardingPartyPrefab, targetPoint.position);
                    break;

                case "Siren Song":
                    SpawnVFX(_sirenSongPrefab, targetPoint.position);
                    break;

                case "Recon Parrot":
                    SpawnVFX(_reconParrotPrefab, targetPoint.position);
                    break;

                case "High Spirits":
                    SpawnVFX(_highSpiritsPrefab, sourcePoint.position);
                    _feedbackHeal?.PlayFeedbacks();
                    break;

                case "Dead Man's Turn":
                    SpawnVFX(_deadMansTurnPrefab, sourcePoint.position);
                    break;

                case "Locker's Return":
                    SpawnVFX(_lockerReturnPrefab, sourcePoint.position);
                    break;

                case "Monkey Grab":
                    SpawnVFX(_monkeyGrabPrefab, targetPoint.position);
                    break;
            }
        }

        private IEnumerator FireCannonball(Vector3 from, Vector3 to)
        {
            if (_cannonballPrefab == null) yield break;

            GameObject ball = Instantiate(_cannonballPrefab, from, Quaternion.identity);
            Vector3    mid  = Vector3.Lerp(from, to, 0.5f) + Vector3.up * _cannonballArcHeight;

            float elapsed = 0f;
            while (elapsed < _cannonballDuration)
            {
                elapsed += Time.deltaTime;
                float   t = Mathf.Clamp01(elapsed / _cannonballDuration);
                Vector3 a = Vector3.Lerp(from, mid, t);
                Vector3 b = Vector3.Lerp(mid,  to,  t);
                ball.transform.position = Vector3.Lerp(a, b, t);
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

        private void SpawnVFX(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return;
            GameObject vfx = Instantiate(prefab, position, Quaternion.identity);
            Destroy(vfx, _vfxLifetime);
        }
    }
}