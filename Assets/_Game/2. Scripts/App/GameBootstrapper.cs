using System.Collections;
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

        [Header("References")]
        [SerializeField] private GameHUD               _gameHUD;
        [SerializeField] private HandLayoutManager     _handLayoutManager;
        [SerializeField] private UnityEngine.UI.Button _endTurnButton;
        [SerializeField] private RectTransform         _playZone;
        [SerializeField] private DeadMansTurnPrompt    _deadMansTurnPrompt;
        [SerializeField] private ResultsPanel          _resultsPanel;
        [SerializeField] private TurnCoordinator       _turnCoordinator;

        [Header("Captain - fallback for testing without level select")]
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

            var playerDeck = new Deck(_playerDeckDefinition.BuildDeck(), _config.LowDeckThreshold);
            var enemyDeck  = new Deck(_activeCaptain.DeckDefinition.BuildDeck(), _config.LowDeckThreshold);

            _gameState    = new GameState(_activeCaptain.HP, playerDeck, enemyDeck);
            _stateMachine = new TurnStateMachine(_gameState, _config);

            var combatResolver = new CombatResolver(_gameState);
            var enemyAI        = new EnemyAI(_activeCaptain);

            _turnCoordinator.Initialise(
                _gameState, _stateMachine, enemyAI,
                _handLayoutManager, combatResolver, _config);

            _turnCoordinator.OnHPChanged              += RefreshHUD;
            _turnCoordinator.OnTurnChanged            += RefreshHUD;
            _turnCoordinator.OnShowDeadMansTurnPrompt += ShowDeadMansTurnPrompt;
            _turnCoordinator.OnCardDrawn              += _handLayoutManager.AddCardToPlayerHand;

            _stateMachine.SetCoroutineRunner(e => StartCoroutine(e));

            SubscribeToEvents();
            DealOpeningHand();

            _endTurnButton.onClick.AddListener(() => _turnCoordinator.EndTurn());

            RefreshHUD();
        }

        private void DealOpeningHand()
        {
            // Enemy opening hand only - player draws manually
            for (int i = 0; i < _config.MaxHandSize; i++)
            {
                CardSO card = _gameState.EnemyDeck.Draw();
                if (card == null) break;
                _gameState.EnemyHand.AddCard(card, _config.MaxHandSize);
                _handLayoutManager.AddCardToEnemyHand(card);
            }
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

            // Deck click - player manual draw
            DeckClickHandler.OnDeckClicked += OnDeckClicked;
        }

        private void OnDestroy()
        {
            _inputActions.Gameplay.EndTurn.performed -= OnEndTurnPressed;
            _inputActions.Dispose();
            _endTurnButton.onClick.RemoveAllListeners();
            DeckClickHandler.OnDeckClicked -= OnDeckClicked;
            GameEventBus.ClearAllListeners();
        }

        private void OnEndTurnPressed(InputAction.CallbackContext context) =>
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
                    card, _playZone, () =>
                    {
                        GameEventBus.OnEnemyCardAnimationComplete?.Invoke();
                    }));
        }

        private void ShowDeadMansTurnPrompt(CardSO card, int damage, string blockCost,
                                            System.Action onNegate, System.Action onTakeHit)
        {
            _deadMansTurnPrompt.Show(card, damage, blockCost, onNegate, onTakeHit);
        }

        private void RefreshHUD()
        {
            _gameHUD.Refresh(
                _gameState.PlayerHP, _config.StartingHP,
                _gameState.EnemyHP,  _config.StartingHP,
                _gameState.PlayerDeck.Count,
                _gameState.IsPlayerTurn);

            _endTurnButton.interactable = _gameState.IsPlayerTurn && _gameState.HasDrawnThisTurn;
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

        private void OnPlayerDeckStateChanged(DeckState state) =>
            Debug.Log($"Player deck: {state}");

        private void OnPlayerHandStateChanged(HandState state) =>
            Debug.Log($"Player hand: {state}");

        private void OnEnemyDeckStateChanged(DeckState state) =>
            Debug.Log($"Enemy deck: {state}");
    }
}