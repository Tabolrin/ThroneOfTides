// Assets/_Game/2. Scripts/App/GameBootstrapper.cs
// Only the Start() method changes - applies upgrade modifiers to GameState init.
// Full file included for completeness.
using System.Collections;
using System.Collections.Generic;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using ThroneOfTides.Systems;
using ThroneOfTides.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThroneOfTides.App
{
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfigSO _config;

        [Header("Decks")]
        [SerializeField] private DeckDefinitionSO _playerDeckDefinition;
        [Tooltip("Build-time only. Set by ThroneOfTidesBuildPipeline.ApplySceneOverrides via a " +
                 "BuildDeckProfileSO before building, then restored afterward - takes priority " +
                 "over the Port-configured deck when assigned. Leave empty for normal Editor play.")]
        [SerializeField] private DeckDefinitionSO _playerDeckOverride;
        [Tooltip("Build-time only. Same as above but for the enemy - takes priority over the " +
                 "active Captain's own deck when assigned.")]
        [SerializeField] private DeckDefinitionSO _enemyDeckOverride;

        [Header("Player")]
        // Optional - when assigned, upgrade levels are applied to HP and Mana at match start.
        // Falls back to config base values when null (useful for testing scenes).
        [SerializeField] private PlayerInventory _playerInventory;

        [Header("Upgrades")]
        [SerializeField] private UpgradeSO _manaUpgrade;
        [SerializeField] private UpgradeSO _hpUpgrade;

        [Header("References")]
        [SerializeField] private GameHUD               _gameHUD;
        [SerializeField] private HandLayoutManager     _handLayoutManager;
        [SerializeField] private UnityEngine.UI.Button _endTurnButton;
        [SerializeField] private RectTransform         _playZone;
        [Tooltip("Where the enemy's played-card display animates to - kept off-center (right side, clear of the ships) so it doesn't cover the card's own VFX playing over the ships.")]
        [SerializeField] private RectTransform         _enemyPlayZone;
        [SerializeField] private DeadMansTurnPrompt    _deadMansTurnPrompt;
        [SerializeField] private TargetSelectionPrompt _targetSelectionPrompt;
        [SerializeField] private ResultsPanel          _resultsPanel;
        [SerializeField] private TurnCoordinator       _turnCoordinator;
        [SerializeField] private CheatsPanel           _cheatsPanel;
        [SerializeField] private CardCheatPanel        _cardCheatPanel;
        [SerializeField] private ForceEnemyCardCheatPanel _forceEnemyCardCheatPanel;
        [SerializeField] private EnemyHandRevealPanel  _enemyHandRevealPanel;
        [Tooltip("Wired here (App layer) rather than directly on CardPresentationPlayer, since Systems cannot reference the UI assembly EnemyHandRevealPanel lives in.")]
        [SerializeField] private ThroneOfTides.Systems.CardPresentationPlayer _cardPresentationPlayer;

        [Header("Debug - Card Registry")]
        [Tooltip("Flat registry of every CardSO - required for CardCheatPanel to list all cards.")]
        [SerializeField] private CardDatabaseSO _cardDatabase;

        [Header("Captain - fallback for testing without level select")]
        [SerializeField] private CaptainSO _fallbackCaptain;

        [Header("VFX Timing")]
        [Tooltip("CardVFXHandler in the scene - its floating damage/heal/mana number queue delay is configured from here so it's tunable in one place per scene.")]
        [SerializeField] private ThroneOfTides.Systems.CardVFXHandler _cardVFXHandler;
        [Tooltip("Seconds between each floating combat number when several need to appear in quick succession (e.g. a multi-hit combo) - keeps them from stacking unreadably on top of each other.")]
        [SerializeField] private float _floatingNumberDelay = 0.15f;

        private GameState                 _gameState;
        private TurnStateMachine          _stateMachine;
        private ThroneOfTidesInputActions _inputActions;
        private CaptainSO                 _activeCaptain;

        private void Awake()
        {
            _inputActions = new ThroneOfTidesInputActions();
            _inputActions.Gameplay.EndTurn.performed += OnEndTurnPressed;
        }

        private void OnEnable()  => _inputActions.Enable();
        private void OnDisable() => _inputActions.Disable();

        private void Start()
        {
            _activeCaptain = GameSession.SelectedCaptain ?? _fallbackCaptain;

            if (_cardVFXHandler != null)
                _cardVFXHandler.SetFloatingNumberDelay(_floatingNumberDelay);

            if (_cardPresentationPlayer != null && _enemyHandRevealPanel != null)
                _cardPresentationPlayer.ShowEnemyHandReveal = _enemyHandRevealPanel.ShowAndAwaitDismiss;

            if (_cardPresentationPlayer != null && _handLayoutManager != null)
            {
                _cardPresentationPlayer.GetHandAreaPosition = side =>
                    side == DamageTarget.Player ? _handLayoutManager.PlayerHandAreaPosition : _handLayoutManager.EnemyHandAreaPosition;
                _cardPresentationPlayer.FinalizeStolenCardVisual = (card, gainedBy) =>
                {
                    if (gainedBy == DamageTarget.Player) _handLayoutManager.StealCardFromEnemyHand(card as CardSO);
                    else _handLayoutManager.StealCardFromPlayerHand(card as CardSO);
                };
                _cardPresentationPlayer.SpawnStolenCardVisual = (card, parent) =>
                    _handLayoutManager.SpawnStolenCardPreview(card as CardSO, parent);
            }

            // ── Apply upgrade modifiers ─────────────────────────────────────
            // Base values from config, with optional per-upgrade level bonuses
            int effectiveMaxHP   = _config.StartingHP;
            int effectiveMaxMana = _config.StartingMaxMana;

            if (_playerInventory != null)
            {
                if (_hpUpgrade   != null)
                    effectiveMaxHP   += _hpUpgrade.GetValueAtLevel(
                        _playerInventory.GetUpgradeLevel(UpgradeType.MaxHP));

                if (_manaUpgrade != null)
                    effectiveMaxMana += _manaUpgrade.GetValueAtLevel(
                        _playerInventory.GetUpgradeLevel(UpgradeType.MaxMana));
            }

            // ── Deck resolution ─────────────────────────────────────────────
            // Use the player's Port-configured deck if available, fall back to
            // the serialized fallback deck definition for testing
            DeckDefinitionSO deckToUse =
                _playerDeckOverride != null ? _playerDeckOverride :
                _playerInventory?.PlayerDeck ?? _playerDeckDefinition;
            DeckDefinitionSO enemyDeckToUse =
                _enemyDeckOverride != null ? _enemyDeckOverride : _activeCaptain.DeckDefinition;

            var originalSnapshot = deckToUse.BuildDeck();
            var playerDeck       = new Deck(originalSnapshot, _config.LowDeckThreshold);
            var enemyDeck        = new Deck(enemyDeckToUse.BuildDeck(), _config.LowDeckThreshold);

            // A Captain with HP/MaxMana left at 0 (unconfigured) falls back to the shared config
            // base values, so half-configured Captains don't accidentally get 0 HP.
            int enemyMaxHP   = _activeCaptain.HP      > 0 ? _activeCaptain.HP      : _config.StartingHP;
            int enemyMaxMana = _activeCaptain.MaxMana > 0 ? _activeCaptain.MaxMana : _config.StartingMaxMana;

            // ── Construct game systems ──────────────────────────────────────
            _gameState    = new GameState(effectiveMaxHP, effectiveMaxMana, _config.MaxHandSize,
                                          playerDeck, enemyDeck, originalSnapshot,
                                          enemyMaxHP, enemyMaxMana, _config.BonusMaxHandSize);
            _stateMachine = new TurnStateMachine(_gameState, _config);

            var combatResolver = new CombatResolver(_gameState, _playerInventory);
            var enemyAI        = new EnemyAI(_activeCaptain);

            _turnCoordinator.Initialise(
                _gameState, _stateMachine, enemyAI,
                _handLayoutManager, combatResolver, _config);

            _turnCoordinator.OnHPChanged            += RefreshHUD;
            _turnCoordinator.OnTurnChanged          += RefreshHUD;
            _turnCoordinator.OnShowReactionPrompt   += ShowReactionPrompt;
            _turnCoordinator.OnShowTargetSelection  += ShowTargetSelectionPrompt;

            if (_cheatsPanel != null) _cheatsPanel.Initialise(_gameState, OnCheatApplied);

            if (_cardCheatPanel != null && _cardDatabase != null)
                _cardCheatPanel.Initialise(_gameState, _handLayoutManager, _cardDatabase, _config.MaxHandSize);

            if (_forceEnemyCardCheatPanel != null && _cardDatabase != null)
                _forceEnemyCardCheatPanel.Initialise(_turnCoordinator, _cardDatabase);

            _stateMachine.SetCoroutineRunner(e => StartCoroutine(e));

            SubscribeToEvents();
            DealOpeningHand();

            _endTurnButton.onClick.AddListener(() => _turnCoordinator.EndTurn());
            RefreshHUD();
        }

        private void DealOpeningHand()
        {
            // Draws a fixed number of cards (MaxHandSize, since the opening hand always starts
            // empty) rather than looping until the hand reaches MaxHandSize - a Reaction card
            // still counts as one of the opening draws even though it charges a badge instead of
            // occupying a hand slot, matching TurnCoordinator's own draw-phase rule. Charges are
            // always 0 at match start, but counting them here too keeps this the same formula
            // TurnCoordinator's per-turn refill uses, in case that ever changes.
            int drawsRemaining = _config.MaxHandSize - (_gameState.EnemyHand.Count + _gameState.EnemyDeadMansTurnCharges + _gameState.EnemyCounterGaleCharges);
            for (int i = 0; i < drawsRemaining && _gameState.EnemyDeck.Count > 0; i++)
            {
                CardSO card = _gameState.EnemyDeck.Draw();
                if (card == null) break;

                if (card.CardType == CardType.Reaction)
                {
                    ChargeReactionCard(card, DamageTarget.Enemy);
                    continue;
                }

                _gameState.EnemyHand.AddCard(card, _config.MaxHandSize);
                _handLayoutManager.AddCardToEnemyHand(card);
            }

            StartCoroutine(DealPlayerOpeningHandRoutine());
        }

        // How long a reaction card sits fully dealt into the hand before it flies off to the
        // reaction badge area and shrinks away - matches TurnCoordinator's own draw-phase delay.
        private const float ReactionAbsorbDelay = 0.4f;

        private IEnumerator DealPlayerOpeningHandRoutine()
        {
            _endTurnButton.interactable    = false;
            DeckClickHandler.OnDeckClicked -= OnDeckClicked;

            var dealtCards    = new List<CardSO>();
            var reactionCards = new List<CardSO>();

            // Same fixed-draw-count rule as DealOpeningHand - a Reaction card still counts as
            // one of the opening draws even though it ends up as a badge charge rather than a
            // hand card. Charges are always 0 at match start, but counting them here too keeps
            // this the same formula TurnCoordinator's per-turn refill uses.
            int drawsRemaining = _config.MaxHandSize - (_gameState.PlayerHand.Count + _gameState.PlayerDeadMansTurnCharges + _gameState.PlayerCounterGaleCharges);
            for (int i = 0; i < drawsRemaining && _gameState.PlayerDeck.Count > 0; i++)
            {
                CardSO card = _gameState.PlayerDeck.Draw();
                if (card == null) break;

                // Dealt into the visual hand in draw order regardless of type - a reaction card
                // reads exactly like any other opening-hand card until it later flies off to
                // charge its badge, instead of the charge silently appearing before the deal
                // animation even starts.
                dealtCards.Add(card);

                if (card.CardType == CardType.Reaction)
                    reactionCards.Add(card);
                else
                    _gameState.PlayerHand.AddCard(card, _config.MaxHandSize);
            }

            _gameState.SetHasDrawnThisTurn();

            yield return StartCoroutine(_handLayoutManager.DealOpeningHandAnimated(dealtCards));

            if (reactionCards.Count > 0)
            {
                yield return new WaitForSeconds(ReactionAbsorbDelay);
                foreach (var card in reactionCards)
                    StartCoroutine(_handLayoutManager.AnimateReactionAbsorb(card, () => ChargeReactionCard(card)));
            }

            DeckClickHandler.OnDeckClicked += OnDeckClicked;
            RefreshHUD();
        }

        private void ChargeReactionCard(CardSO card, DamageTarget side = DamageTarget.Player)
        {
            if (card.Id == CardId.DeadMansTurn)   _gameState.AddDeadMansTurnCharge(side);
            else if (card.Id == CardId.CounterGale) _gameState.AddCounterGaleCharge(side);
        }

        private void Update() => _stateMachine?.Tick();

        private void SubscribeToEvents()
        {
            _gameState.PlayerDeck.OnDeckStateChanged += OnPlayerDeckStateChanged;
            _gameState.PlayerHand.OnHandStateChanged += OnPlayerHandStateChanged;
            _gameState.EnemyDeck.OnDeckStateChanged  += OnEnemyDeckStateChanged;

            GameEventBus.OnCardPlayed      += OnCardPlayed;
            GameEventBus.OnEnemyCardPlayed += OnEnemyCardPlayed;
            GameEventBus.OnMatchWin        += OnMatchWin;
            GameEventBus.OnMatchLoss       += OnMatchLoss;

            DeckClickHandler.OnDeckClicked += OnDeckClicked;
        }

        private void OnDestroy()
        {
            _inputActions.Gameplay.EndTurn.performed -= OnEndTurnPressed;
            _inputActions.Dispose();

            if (_turnCoordinator != null)
            {
                _turnCoordinator.OnHPChanged           -= RefreshHUD;
                _turnCoordinator.OnTurnChanged         -= RefreshHUD;
                _turnCoordinator.OnShowReactionPrompt  -= ShowReactionPrompt;
                _turnCoordinator.OnShowTargetSelection -= ShowTargetSelectionPrompt;
            }

            if (_gameState != null)
            {
                _gameState.PlayerDeck.OnDeckStateChanged -= OnPlayerDeckStateChanged;
                _gameState.PlayerHand.OnHandStateChanged -= OnPlayerHandStateChanged;
                _gameState.EnemyDeck.OnDeckStateChanged  -= OnEnemyDeckStateChanged;
            }

            _endTurnButton.onClick.RemoveAllListeners();
            DeckClickHandler.OnDeckClicked -= OnDeckClicked;
            GameEventBus.ClearAllListeners();
        }

        private void OnEndTurnPressed(InputAction.CallbackContext ctx) =>
            _turnCoordinator.EndTurn();

        private void OnDeckClicked()
        {
            if (!_gameState.IsPlayerTurn) return;
            _turnCoordinator.TryDrawCard();
            RefreshHUD();
        }

        private void OnCardPlayed(ICard card)
        {
            var cardSO = card as CardSO;
            if (cardSO == null) return;
            _turnCoordinator.HandleCardPlayed(cardSO);
            RefreshHUD();
        }

        private void OnEnemyCardPlayed(ICard card, DamageTarget? selectedTarget)
        {
            var cardSO = card as CardSO;
            if (cardSO == null) return;
            StartCoroutine(EnemyCardAnimationRoutine(cardSO));
        }

        private IEnumerator EnemyCardAnimationRoutine(CardSO card)
        {
            RectTransform destination = _enemyPlayZone != null ? _enemyPlayZone : _playZone;
            yield return StartCoroutine(
                _handLayoutManager.PlayEnemyCardAnimation(
                    card, destination,
                    GameEventBus.FireEnemyCardAnimationComplete));
        }

        private void ShowReactionPrompt(CardSO card, int damage,
                                        string negateLabel, System.Action onNegate,
                                        string counterGaleLabel, System.Action onCounterGale,
                                        System.Action onTakeHit)
        {
            _deadMansTurnPrompt.Show(card, damage, negateLabel, onNegate, counterGaleLabel, onCounterGale, onTakeHit);
        }

        private void ShowTargetSelectionPrompt(CardSO card, System.Action<DamageTarget> onTargetChosen)
        {
            _targetSelectionPrompt.Show(card, onTargetChosen);
        }

        // CheatsPanel's HP/mana buttons bypass the normal turn flow, so unlike a real card play
        // or enemy attack, nothing would otherwise check whether the change just ended the match.
        private void OnCheatApplied()
        {
            RefreshHUD();
            _turnCoordinator.CheckGameOver();
        }

        private void RefreshHUD()
        {
            _gameHUD.Refresh(
                _gameState.PlayerHP,    _gameState.PlayerMaxHP,
                _gameState.EnemyHP,     _gameState.EnemyMaxHP,
                _gameState.PlayerDeck.Count,
                _gameState.IsPlayerTurn,
                _gameState.PlayerMana,
                _gameState.PlayerMaxMana,
                _gameState.EnemyMana,
                _gameState.EnemyMaxMana);

            _endTurnButton.interactable = _gameState.IsPlayerTurn;
        }

        private void OnMatchWin()
        {
            _endTurnButton.interactable = false;
            _resultsPanel.ShowWin(_activeCaptain.LevelReward, _gameState.PlayerHP);
        }

        private void OnMatchLoss()
        {
            _endTurnButton.interactable = false;
            _resultsPanel.ShowLoss(_activeCaptain.LevelReward, _gameState.PlayerHP);
        }

        private void OnPlayerDeckStateChanged(DeckState state) => Debug.Log($"Player deck: {state}");
        private void OnPlayerHandStateChanged(HandState state) => Debug.Log($"Player hand: {state}");
        private void OnEnemyDeckStateChanged(DeckState state)  => Debug.Log($"Enemy deck: {state}");
    }
}