// Assets/_Game/2. Scripts/App/GameBootstrapper.cs
// Only the Start() method changes — applies upgrade modifiers to GameState init.
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

        [Header("Player")]
        // Optional — when assigned, upgrade levels are applied to HP and Mana at match start.
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
        [SerializeField] private DeadMansTurnPrompt    _deadMansTurnPrompt;
        [SerializeField] private ResultsPanel          _resultsPanel;
        [SerializeField] private TurnCoordinator       _turnCoordinator;

        [Header("Captain — fallback for testing without level select")]
        [SerializeField] private CaptainSO _fallbackCaptain;

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
                _playerInventory?.PlayerDeck ?? _playerDeckDefinition;

            var originalSnapshot = deckToUse.BuildDeck();
            var playerDeck       = new Deck(originalSnapshot, _config.LowDeckThreshold);
            var enemyDeck        = new Deck(
                _activeCaptain.DeckDefinition.BuildDeck(), _config.LowDeckThreshold);

            // ── Construct game systems ──────────────────────────────────────
            _gameState    = new GameState(effectiveMaxHP, effectiveMaxMana,
                                          playerDeck, enemyDeck, originalSnapshot);
            _stateMachine = new TurnStateMachine(_gameState, _config);

            var combatResolver = new CombatResolver(_gameState);
            var enemyAI        = new EnemyAI(_activeCaptain);

            _turnCoordinator.Initialise(
                _gameState, _stateMachine, enemyAI,
                _handLayoutManager, combatResolver, _config);

            _turnCoordinator.OnHPChanged          += RefreshHUD;
            _turnCoordinator.OnTurnChanged        += RefreshHUD;
            _turnCoordinator.OnShowReactionPrompt += ShowReactionPrompt;

            _stateMachine.SetCoroutineRunner(e => StartCoroutine(e));

            SubscribeToEvents();
            DealOpeningHand();

            _endTurnButton.onClick.AddListener(() => _turnCoordinator.EndTurn());
            RefreshHUD();
        }

        private void DealOpeningHand()
        {
            for (int i = 0; i < _config.MaxHandSize; i++)
            {
                CardSO card = _gameState.EnemyDeck.Draw();
                if (card == null) break;
                _gameState.EnemyHand.AddCard(card, _config.MaxHandSize);
                _handLayoutManager.AddCardToEnemyHand(card);
            }

            StartCoroutine(DealPlayerOpeningHandRoutine());
        }

        private IEnumerator DealPlayerOpeningHandRoutine()
        {
            _endTurnButton.interactable    = false;
            DeckClickHandler.OnDeckClicked -= OnDeckClicked;

            var normalCards   = new List<CardSO>();
            var reactionCards = new List<CardSO>();

            for (int i = 0; i < _config.MaxHandSize; i++)
            {
                CardSO card = _gameState.PlayerDeck.Draw();
                if (card == null) break;

                if (card.CardType == CardType.Reaction)
                    reactionCards.Add(card);
                else
                {
                    _gameState.PlayerHand.AddCard(card, _config.MaxHandSize);
                    normalCards.Add(card);
                }
            }

            foreach (var card in reactionCards)
                ChargeReactionCard(card);

            _gameState.SetHasDrawnThisTurn();

            yield return StartCoroutine(_handLayoutManager.DealOpeningHandAnimated(normalCards));

            foreach (var card in reactionCards)
                yield return StartCoroutine(_handLayoutManager.AnimateReactionDraw(card));

            DeckClickHandler.OnDeckClicked += OnDeckClicked;
            RefreshHUD();
        }

        private void ChargeReactionCard(CardSO card)
        {
            if (card.Name == "Dead Man's Turn")      _gameState.AddDeadMansTurnCharge();
            else if (card.Name == "Blood for Blood") _gameState.AddBloodForBloodCharge();
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
                _turnCoordinator.OnHPChanged          -= RefreshHUD;
                _turnCoordinator.OnTurnChanged        -= RefreshHUD;
                _turnCoordinator.OnShowReactionPrompt -= ShowReactionPrompt;
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

        private void OnEnemyCardPlayed(ICard card)
        {
            var cardSO = card as CardSO;
            if (cardSO == null) return;
            StartCoroutine(EnemyCardAnimationRoutine(cardSO));
        }

        private IEnumerator EnemyCardAnimationRoutine(CardSO card)
        {
            yield return StartCoroutine(
                _handLayoutManager.PlayEnemyCardAnimation(
                    card, _playZone,
                    () => GameEventBus.OnEnemyCardAnimationComplete?.Invoke()));
        }

        private void ShowReactionPrompt(CardSO card, int damage, string blockCost,
                                        System.Action onNegate, System.Action onTakeHit)
        {
            _deadMansTurnPrompt.Show(card, damage, blockCost, onNegate, onTakeHit);
        }

        private void RefreshHUD()
        {
            _gameHUD.Refresh(
                _gameState.PlayerHP,    _config.StartingHP,
                _gameState.EnemyHP,     _config.StartingHP,
                _gameState.PlayerDeck.Count,
                _gameState.IsPlayerTurn,
                _gameState.PlayerMana,
                _gameState.PlayerMaxMana);

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