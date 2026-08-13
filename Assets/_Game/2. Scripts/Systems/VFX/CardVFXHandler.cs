// Assets/_Game/2. Scripts/Systems/VFX/CardVFXHandler.cs
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using ThroneOfTides.Systems.VFX;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public class CardVFXHandler : MonoBehaviour
    {
        [Header("Canvas & Camera")]
        [SerializeField] private RectTransform  _gameCanvasRect;
        [SerializeField] private Camera         _gameCamera;

        [Header("Spawn Points")]
        [SerializeField] private Transform _playerShipHitPoint;
        [SerializeField] private Transform _enemyShipHitPoint;
        [SerializeField] private Transform _playerShipSpawnPoint;
        [SerializeField] private Transform _playerDeckPoint;
        [Tooltip("Always used for Counter Gale's tornado, regardless of which side actually fired the reaction.")]
        [SerializeField] private Transform _playerSkyPoint;
        [Tooltip("Always used for Locker's Return's tentacle, regardless of which side actually played the card.")]
        [SerializeField] private Transform _playerSeaSurfaceRightPoint;

        [Header("VFX Prefabs — Weapon")]
        [SerializeField] private GameObject _hitImpactStandardPrefab;
        [SerializeField] private GameObject _hitImpactExplosionPrefab;
        [SerializeField] private GameObject _whirlpoolPrefab;

        [Header("VFX Prefabs — Action")]
        [SerializeField] private GameObject _reconParrotPrefab;
        [SerializeField] private GameObject _highSpiritsPrefab;
        [SerializeField] private GameObject _lockerReturnPrefab;
        [SerializeField] private GameObject _monkeyGrabPrefab;
        [SerializeField] private GameObject _rumPrefab;

        [Header("VFX Prefabs — Reaction")]
        [SerializeField] private GameObject _deadMansTurnPrefab;
        [SerializeField] private GameObject _counterGaleTornadoPrefab;
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

        [Header("Floating Combat Text")]
        [SerializeField] private FloatingCombatText _floatingTextPrefab;
        private static readonly Color DamageColor = new Color(0.90f, 0.15f, 0.15f);
        private static readonly Color HealColor   = new Color(0.25f, 0.85f, 0.30f);
        private static readonly Color ManaColor   = new Color(0.45f, 0.75f, 1.00f);

        [Header("Timing")]
        [SerializeField] private float _vfxLifetime      = 2f;
        [SerializeField] private float _winSlowDuration  = 0.8f;
        [SerializeField] private float _lossSlowDuration = 0.5f;

        // Default when nothing overrides it (e.g. via SetFloatingNumberDelay) — GameBootstrapper
        // owns the authoritative configured value so it's tunable in one place per scene.
        [SerializeField] private float _floatingNumberDelay = 0.15f;

        // Tracks previous mana value per side so OnPlayerManaChanged/OnEnemyManaChanged can
        // distinguish spend from gain without requiring additional event parameters.
        private int _previousPlayerMana = -1;
        private int _previousEnemyMana  = -1;

        // Several damage/heal/mana events can fire within the same frame (e.g. a combo hitting
        // multiple times, or a DOT tick landing alongside a card play) — queued and drained with
        // a delay between each so overlapping numbers don't stack unreadably on top of each other.
        private readonly Queue<(string text, Color color, Vector3 position)> _floatingNumberQueue = new();
        private Coroutine _floatingNumberQueueRoutine;

        /// <summary>Overrides the inspector default — see GameBootstrapper's configurable delay.</summary>
        public void SetFloatingNumberDelay(float delay) => _floatingNumberDelay = Mathf.Max(0f, delay);

        // Set by a self-driving ICardPlayEffect (e.g. Essence Plunder) that wants to show its own
        // "+N" popup timed to its own animation instead of the generic one OnPlayerManaChanged/
        // OnEnemyManaChanged would otherwise fire the instant the mana actually changes — which
        // happens synchronously as part of the same card resolution, before that animation even
        // starts. Consumed (reset) the next time either handler runs, so it never leaks into an
        // unrelated later mana change.
        private bool _suppressNextManaGainPopup;

        public void SuppressNextManaGainPopup() => _suppressNextManaGainPopup = true;

        /// <summary>Public entry point for a self-driving effect's own deferred "+N" mana popup — routed through the same queue as every other floating number.</summary>
        public void SpawnFloatingManaGain(int amount, Vector3 position) =>
            SpawnFloatingNumber($"+{amount}", ManaColor, position);

        // ── Unity ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            GameEventBus.OnDamageDealt       += OnDamageDealt;
            GameEventBus.OnHealApplied       += OnHealApplied;
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
            GameEventBus.OnEnemyManaChanged  += OnEnemyManaChanged;
            GameEventBus.OnReactionCharged   += OnReactionCharged;
            GameEventBus.OnReactionFired     += OnReactionFired;
        }

        private void OnDisable()
        {
            GameEventBus.OnDamageDealt       -= OnDamageDealt;
            GameEventBus.OnHealApplied       -= OnHealApplied;
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
            GameEventBus.OnEnemyManaChanged  -= OnEnemyManaChanged;
            GameEventBus.OnReactionCharged   -= OnReactionCharged;
            GameEventBus.OnReactionFired     -= OnReactionFired;
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void OnDamageDealt(DamageTarget target, int amount)
        {
            Transform hit = GetHitPoint(target);
            _feedbackDamageNumber?.PlayFeedbacks(hit.position, amount);
            SpawnFloatingNumber($"-{amount}", DamageColor, hit.position);

            if      (amount >= 8) { SpawnVFX(_hitImpactExplosionPrefab, hit.position); _feedbackHeavyHit?.PlayFeedbacks(); }
            else if (amount >= 4) { SpawnVFX(_hitImpactStandardPrefab,  hit.position); _feedbackMediumHit?.PlayFeedbacks(); }
            else if (amount > 0)  { SpawnVFX(_hitImpactStandardPrefab,  hit.position); _feedbackLightHit?.PlayFeedbacks(); }
        }

        private void OnHealApplied(DamageTarget target, int amount)
        {
            Transform hit = GetHitPoint(target);
            SpawnFloatingNumber($"+{amount}", HealColor, hit.position);
            _feedbackHeal?.PlayFeedbacks();
        }

        private void OnCardPlayed(ICard card)   => _feedbackCardPlay?.PlayFeedbacks();
        private void OnCardDrawn(ICard card)     => _feedbackCardDraw?.PlayFeedbacks();
        private void OnComboResolved()           => _feedbackComboResolve?.PlayFeedbacks();
        private void OnMatchWin()                => StartCoroutine(WinSequence());
        private void OnMatchLoss()               => StartCoroutine(LossSequence());

        private void OnComboStackChanged(DamageTarget side, int count)
        {
            if (side == DamageTarget.Player && count > 0) _feedbackComboStackIncrement?.PlayFeedbacks();
        }

        private void OnTurnPhaseChanged(TurnPhase phase)
        {
            if (phase == TurnPhase.Draw) _feedbackEndTurnPulse?.PlayFeedbacks();
        }

        private void OnDOTTick(DotEffect effect)
        {
            _feedbackDOTTick?.PlayFeedbacks();
        }

        private void OnPlayerManaChanged(int current, int max)
        {
            bool hadPrevious = _previousPlayerMana >= 0;
            bool wasSpent     = hadPrevious && current < _previousPlayerMana;
            bool wasGained    = hadPrevious && current > _previousPlayerMana;

            if (wasGained)
            {
                if (_suppressNextManaGainPopup) _suppressNextManaGainPopup = false;
                else SpawnFloatingNumber($"+{current - _previousPlayerMana}", ManaColor, GetHitPoint(DamageTarget.Player).position);
            }

            _previousPlayerMana = current;

            if (wasSpent) _feedbackManaSpent?.PlayFeedbacks();
            else          _feedbackManaGained?.PlayFeedbacks();
        }

        private void OnEnemyManaChanged(int current, int max)
        {
            bool hadPrevious = _previousEnemyMana >= 0;
            bool wasGained    = hadPrevious && current > _previousEnemyMana;

            if (wasGained)
            {
                if (_suppressNextManaGainPopup) _suppressNextManaGainPopup = false;
                else SpawnFloatingNumber($"+{current - _previousEnemyMana}", ManaColor, GetHitPoint(DamageTarget.Enemy).position);
            }

            _previousEnemyMana = current;
        }

        private void OnReactionCharged(ReactionType type, DamageTarget side, int charges) =>
            _feedbackReactionCharged?.PlayFeedbacks();

        private void OnReactionFired(ReactionType type, DamageTarget side)
        {
            _feedbackReactionFired?.PlayFeedbacks();

            switch (type)
            {
                // Anchor appears on whichever ship actually activated it.
                case ReactionType.DeadMansTurn:
                    if (_deadMansTurnPrefab != null)
                        Instantiate(_deadMansTurnPrefab, GetHitPoint(side).position, Quaternion.identity);
                    break;

                // Always the player's sky anchor, regardless of which side fired it.
                case ReactionType.CounterGale:
                    if (_counterGaleTornadoPrefab != null && _playerSkyPoint != null)
                        Instantiate(_counterGaleTornadoPrefab, _playerSkyPoint.position, Quaternion.identity);
                    break;
            }
        }

        private void OnCardPlayAccepted(ICard card, DamageTarget? selectedTarget)
        {
            var cardSO = card as CardSO;
            if (cardSO == null) return;
            PlayCardVFX(cardSO);
        }

        // ── Card VFX Routing ──────────────────────────────────────────────────

        private void PlayCardVFX(CardSO card)
        {
            Transform target = _enemyShipHitPoint;

            switch (card.Id)
            {
                // Pistol/Cannonball/Chain Shot and Whale Ram are migrated to
                // CardPresentationPlayer (PresentationEntries + ICardPlayEffect) — no case
                // needed here for their spawn logic.

                case CardId.Whirlpool: SpawnVFX(_whirlpoolPrefab, target.position); break;

                // Hail Storm/Lightning/Gunpowder Barrel/Torch/Kraken/Siren Song/Tidal Wave are
                // migrated to CardPresentationPlayer (PresentationEntries + ICardPlayEffect) —
                // no case needed here for their spawn logic.
                case CardId.Lightning:
                    _feedbackLightningFlash?.PlayFeedbacks();
                    break;

                case CardId.ReconParrot:    SpawnVFX(_reconParrotPrefab,   target.position); break;

                case CardId.LockersReturn:
                {
                    if (_lockerReturnPrefab != null && _playerSeaSurfaceRightPoint != null)
                    {
                        var instance = Instantiate(_lockerReturnPrefab, _playerSeaSurfaceRightPoint.position, Quaternion.identity);
                        var controller = instance.GetComponent<LockersReturnVFXController>();
                        controller?.Setup(card.Art, _playerDeckPoint,
                            () => SpawnFloatingNumber("+1", Color.white, _playerDeckPoint.position));
                    }
                    break;
                }

                case CardId.MonkeyGrab:     SpawnVFX(_monkeyGrabPrefab,    target.position); break;

                // Treasure Chest is migrated to CardPresentationPlayer (PresentationEntries +
                // ICardPlayEffect) — no case needed here for its spawn logic.

                case CardId.HighSpirits:
                    // Appears "between the ships" — always the player's sky anchor, matching
                    // Counter Gale's placement. Self-manages its own animation length and
                    // destroys itself, so it's not routed through SpawnVFX's fixed _vfxLifetime
                    // timer. Mana-gain feedback already fires generically via
                    // OnPlayerManaChanged, so no explicit call needed here.
                    if (_highSpiritsPrefab != null && _playerSkyPoint != null)
                        Instantiate(_highSpiritsPrefab, _playerSkyPoint.position, Quaternion.identity);
                    break;

                // Rum is SFX-only, migrated to CardPresentationPlayer's SFX-only entry support —
                // no case needed here (and its heal feedback already fires generically via
                // OnHealApplied).

                // Reactions are charged on draw — OnReactionCharged/Fired handle their VFX
                case CardId.DeadMansTurn:
                case CardId.CounterGale:
                    break;
            }
        }

        // ── VFX Sequences ─────────────────────────────────────────────────────

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

        private void SpawnFloatingNumber(string text, Color color, Vector3 position)
        {
            if (_floatingTextPrefab == null) return;

            _floatingNumberQueue.Enqueue((text, color, position));
            _floatingNumberQueueRoutine ??= StartCoroutine(ProcessFloatingNumberQueue());
        }

        private IEnumerator ProcessFloatingNumberQueue()
        {
            // StartCoroutine runs synchronously up to the first yield — without this, several
            // SpawnFloatingNumber calls in the same frame (e.g. a multi-hit combo) would each
            // see a freshly-empty queue and drain their own single item immediately instead of
            // ever accumulating together, defeating the whole point of the delay. Yielding once
            // up front lets every same-frame call land in the queue before draining begins.
            yield return null;

            while (_floatingNumberQueue.Count > 0)
            {
                var (text, color, position) = _floatingNumberQueue.Dequeue();
                var instance = Instantiate(_floatingTextPrefab, position, Quaternion.identity);
                instance.Setup(text, color);

                if (_floatingNumberQueue.Count > 0)
                    yield return new WaitForSeconds(_floatingNumberDelay);
            }

            _floatingNumberQueueRoutine = null;
        }
    }
}