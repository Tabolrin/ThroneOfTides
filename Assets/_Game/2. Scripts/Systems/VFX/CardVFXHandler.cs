// Assets/_Game/2. Scripts/Systems/VFX/CardVFXHandler.cs
using System.Collections;
using DG.Tweening;
using MoreMountains.Feedbacks;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public class CardVFXHandler : MonoBehaviour
    {
        [Header("Canvas & Camera")]
        [SerializeField] private RectTransform  _gameCanvasRect;
        [SerializeField] private Camera         _gameCamera;
        [SerializeField] private ParticleSystem _musicNoteParticles;

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
        [SerializeField] private GameObject _ramTheHullPrefab;
        [SerializeField] private GameObject _krakenPrefab;

        [Header("VFX Prefabs — Action")]
        [SerializeField] private GameObject _sirenSongPrefab;
        [SerializeField] private GameObject _reconParrotPrefab;
        [SerializeField] private GameObject _highSpiritsPrefab;
        [SerializeField] private GameObject _lockerReturnPrefab;
        [SerializeField] private GameObject _monkeyGrabPrefab;
        [SerializeField] private GameObject _rumPrefab;
        [SerializeField] private GameObject _treasureChestPrefab;
        [SerializeField] private GameObject _stolenWindPrefab;

        [Header("VFX Prefabs — Reaction")]
        [SerializeField] private GameObject _deadMansTurnPrefab;
        [SerializeField] private GameObject _bloodForBloodPrefab;

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

        [Header("FEEL — Mana & Reactions")]
        [SerializeField] private MMF_Player _feedbackManaSpent;
        [SerializeField] private MMF_Player _feedbackManaGained;
        [SerializeField] private MMF_Player _feedbackReactionCharged;
        [SerializeField] private MMF_Player _feedbackReactionFired;

        [Header("FEEL — Match")]
        [SerializeField] private MMF_Player _feedbackWin;
        [SerializeField] private MMF_Player _feedbackLoss;

        [Header("FEEL — Damage Numbers")]
        [SerializeField] private MMF_Player _feedbackDamageNumber;

        [Header("Timing")]
        [SerializeField] private float _vfxLifetime      = 2f;
        [SerializeField] private float _winSlowDuration  = 0.8f;
        [SerializeField] private float _lossSlowDuration = 0.5f;

        // Tracks previous mana value so OnPlayerManaChanged can distinguish
        // spend from gain without requiring additional event parameters.
        private int _previousPlayerMana = -1;

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
            GameEventBus.OnPlayerManaChanged += OnPlayerManaChanged;
            GameEventBus.OnReactionCharged   += OnReactionCharged;
            GameEventBus.OnReactionFired     += OnReactionFired;
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
            GameEventBus.OnPlayerManaChanged -= OnPlayerManaChanged;
            GameEventBus.OnReactionCharged   -= OnReactionCharged;
            GameEventBus.OnReactionFired     -= OnReactionFired;
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void OnDamageDealt(DamageTarget target, int amount)
        {
            Transform hit = GetHitPoint(target);
            _feedbackDamageNumber?.PlayFeedbacks(hit.position, amount);

            if      (amount >= 8) { SpawnVFX(_hitImpactExplosionPrefab, hit.position); _feedbackHeavyHit?.PlayFeedbacks(); }
            else if (amount >= 4) { SpawnVFX(_hitImpactStandardPrefab,  hit.position); _feedbackMediumHit?.PlayFeedbacks(); }
            else if (amount > 0)  { SpawnVFX(_hitImpactStandardPrefab,  hit.position); _feedbackLightHit?.PlayFeedbacks(); }
        }

        private void OnCardPlayed(ICard card)   => _feedbackCardPlay?.PlayFeedbacks();
        private void OnCardDrawn(ICard card)     => _feedbackCardDraw?.PlayFeedbacks();
        private void OnComboResolved()           => _feedbackComboResolve?.PlayFeedbacks();
        private void OnMatchWin()                => StartCoroutine(WinSequence());
        private void OnMatchLoss()               => StartCoroutine(LossSequence());

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

        private void OnPlayerManaChanged(int current, int max)
        {
            bool wasSpent = _previousPlayerMana >= 0 && current < _previousPlayerMana;
            _previousPlayerMana = current;

            if (wasSpent) _feedbackManaSpent?.PlayFeedbacks();
            else          _feedbackManaGained?.PlayFeedbacks();
        }

        private void OnReactionCharged(ReactionType type, int charges) =>
            _feedbackReactionCharged?.PlayFeedbacks();

        private void OnReactionFired(ReactionType type) =>
            _feedbackReactionFired?.PlayFeedbacks();

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
                case "Pistol":
                case "Canon Ball":
                case "Whale Ram":
                case "Chain Shot":
                case "Tidal Wave":
                    yield return StartCoroutine(FireCannonball(source.position, target.position));
                    break;

                case "Ram the Hull":
                    SpawnVFX(_ramTheHullPrefab != null
                        ? _ramTheHullPrefab
                        : _hitImpactExplosionPrefab, target.position);
                    _feedbackHeavyHit?.PlayFeedbacks();
                    break;

                case "Hail Storm":  SpawnVFX(_hailStormPrefab,  target.position); break;
                case "Whirlpool":   SpawnVFX(_whirlpoolPrefab,  target.position); break;

                case "Lightning":
                    SpawnVFX(_lightningPrefab, target.position);
                    _feedbackLightningFlash?.PlayFeedbacks();
                    break;

                case "Gunpowder Barrel":
                    SpawnVFX(_gunpowderBarrelPrefab, source.position);
                    break;

                case "Torch":
                    SpawnVFX(_torchComboResolvePrefab != null
                        ? _torchComboResolvePrefab
                        : _torchPrefab, target.position);
                    break;

                case "The Kraken":
                    HandleCreatureVFX(_krakenPrefab, DamageTarget.Enemy);
                    break;

                case "Siren Song":
                    HandleCreatureVFX(_sirenSongPrefab, DamageTarget.Enemy);
                    break;

                case "Recon Parrot":    SpawnVFX(_reconParrotPrefab,   target.position); break;
                case "Locker's Return": SpawnVFX(_lockerReturnPrefab,  source.position); break;
                case "Monkey Grab":     SpawnVFX(_monkeyGrabPrefab,    target.position); break;
                case "Treasure Chest":  SpawnVFX(_treasureChestPrefab, source.position); break;

                case "High Spirits":
                    SpawnVFX(_highSpiritsPrefab, source.position);
                    _feedbackManaGained?.PlayFeedbacks();
                    break;

                case "Rum":
                    SpawnVFX(_rumPrefab, source.position);
                    _feedbackHeal?.PlayFeedbacks();
                    break;

                case "Stolen Wind":
                    SpawnVFX(_stolenWindPrefab, source.position);
                    break;

                // Reactions are charged on draw — OnReactionCharged/Fired handle their VFX
                case "Dead Man's Turn":
                case "Blood for Blood":
                    break;
            }
        }

        // ── Creature VFX ──────────────────────────────────────────────────────

        private void HandleCreatureVFX(GameObject prefab, DamageTarget target)
        {
            if (prefab == null) return;

            var instance     = Instantiate(prefab, _gameCanvasRect);
            Vector3 worldPos = GetHitPoint(target).position;

            if (prefab == _krakenPrefab)
            {
                var ctrl = instance.GetComponent<VFX.KrakenVFXController>();
                if (ctrl == null) return;
                ctrl.Inject(_gameCanvasRect, _gameCamera);
                ctrl.OnAttackMoment += GameEventBus.FireKrakenAttackMoment;
                ctrl.OnSequenceEnd  += () => Destroy(instance);
                ctrl.StartSequence(worldPos);
            }
            else if (prefab == _sirenSongPrefab)
            {
                var ctrl = instance.GetComponent<VFX.SirenVFXController>();
                if (ctrl == null) return;
                ctrl.Inject(_gameCanvasRect, _gameCamera, _musicNoteParticles);
                ctrl.OnSirenReady  += GameEventBus.FireSirenSongActive;
                ctrl.OnSequenceEnd += () => Destroy(instance);
                ctrl.StartSequence(worldPos);
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
                elapsed                += Time.deltaTime;
                float   t               = Mathf.Clamp01(elapsed / _cannonballDuration);
                ball.transform.position = Vector3.Lerp(Vector3.Lerp(from, mid, t),
                                                       Vector3.Lerp(mid,  to,  t), t);
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