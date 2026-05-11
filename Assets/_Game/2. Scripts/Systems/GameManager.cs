using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using ThroneOfTides.Systems;
using ThroneOfTides.UI;

namespace ThroneOfTides.Systems
{
    public class GameManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfigSO _config;

        [Header("Decks")]
        [SerializeField] private DeckDefinitionSO _playerDeckDefinition;
        [SerializeField] private DeckDefinitionSO _enemyDeckDefinition;

        [Header("References")]
        [SerializeField] private GameHUD               _gameHUD;
        [SerializeField] private HandLayoutManager     _handLayoutManager;
        [SerializeField] private UnityEngine.UI.Button _endTurnButton;

        private GameState                 _gameState;
        private TurnStateMachine          _stateMachine;
        private ThroneOfTidesInputActions _inputActions;

        private void Awake()
        {
            _inputActions = new ThroneOfTidesInputActions();
            _inputActions.Gameplay.EndTurn.performed += OnEndTurnPressed;
        }

        private void OnEnable()  => _inputActions.Enable();
        private void OnDisable() => _inputActions.Disable();

        private void Start()
        {
            var playerDeck = new Deck(_playerDeckDefinition.BuildDeck(), _config.LowDeckThreshold);
            var enemyDeck  = new Deck(_enemyDeckDefinition.BuildDeck(),  _config.LowDeckThreshold);

            _gameState = new GameState(_config.StartingHP, playerDeck, enemyDeck);

            SubscribeToEvents();
            DealOpeningHand();

            _stateMachine = new TurnStateMachine(_gameState, _config);
            _stateMachine.SetOnCardDrawn(_handLayoutManager.AddCardToPlayerHand);
            _stateMachine.SetCoroutineRunner(e => StartCoroutine(e));

            _endTurnButton.onClick.AddListener(() => EndTurn());

            RefreshHUD();
        }

        private void DealOpeningHand()
        {
            for (int i = 0; i < _config.MaxHandSize; i++)
            {
                CardSO card = _gameState.PlayerDeck.Draw();
                if (card == null) break;
                _gameState.PlayerHand.AddCard(card, _config.MaxHandSize);
                _handLayoutManager.AddCardToPlayerHand(card);
            }
        }

        private void Update() => _stateMachine?.Tick();

        private void SubscribeToEvents()
        {
            _gameState.PlayerDeck.OnDeckStateChanged += OnPlayerDeckStateChanged;
            _gameState.PlayerHand.OnHandStateChanged += OnPlayerHandStateChanged;
            _gameState.EnemyDeck.OnDeckStateChanged  += OnEnemyDeckStateChanged;
            _gameState.OnEnemyTurnReady              += OnEnemyTurnReady;

            GameEventBus.OnCardPlayed         += OnCardPlayed;
            GameEventBus.OnPlayerCardRemoved  += OnPlayerCardRemoved;
            GameEventBus.OnMatchWin           += OnMatchWin;
            GameEventBus.OnMatchLoss          += OnMatchLoss;
        }

        private void OnDestroy()
        {
            _inputActions.Gameplay.EndTurn.performed -= OnEndTurnPressed;
            _inputActions.Dispose();
            _endTurnButton.onClick.RemoveAllListeners();
            GameEventBus.ClearAllListeners();
        }

        private void OnEndTurnPressed(InputAction.CallbackContext context) => EndTurn();

        private void EndTurn()
        {
            if (!_gameState.IsPlayerTurn) return;
            _stateMachine.TransitionTo(_stateMachine.EnemyTurn);
        }

        private void OnCardPlayed(ICard card)
        {
            // Cast to CardSO - safe since all cards in this game are CardSO
            var cardSO = card as CardSO;
            if (cardSO == null) return;

            // TODO - route to CombatSystem when built
            _gameState.PlayerHand.RemoveCard(cardSO);
            _gameState.NotifyPlayerCardRemoved(cardSO);
            _gameState.ApplyDamage(DamageTarget.Enemy, cardSO.Damage);
            RefreshHUD();

            if (_gameState.IsGameOver())
            {
                if (_gameState.GetWinner() == Winner.Player)
                    GameEventBus.FireMatchWin();
                else
                    GameEventBus.FireMatchLoss();
            }
        }

        private void OnEnemyTurnReady() => StartCoroutine(EnemyTurnRoutine());

        private IEnumerator EnemyTurnRoutine()
        {
            float delay = Random.Range(_config.EnemyThinkTimeMin, _config.EnemyThinkTimeMax);
            yield return new WaitForSeconds(delay);

            // TODO - replace with real AI card selection when combat system is built
            _gameState.ApplyDamage(DamageTarget.Player, 1);
            RefreshHUD();

            if (_gameState.IsGameOver())
            {
                if (_gameState.GetWinner() == Winner.Player)
                    GameEventBus.FireMatchWin();
                else
                    GameEventBus.FireMatchLoss();
                yield break;
            }

            _stateMachine.TransitionTo(_stateMachine.PlayerTurn);
        }

        private void RefreshHUD()
        {
            _gameHUD.Refresh(
                _gameState.PlayerHP, _config.StartingHP,
                _gameState.EnemyHP,  _config.StartingHP,
                _gameState.PlayerDeck.Count,
                _gameState.IsPlayerTurn);

            _endTurnButton.interactable = _gameState.IsPlayerTurn;
        }

        private void OnPlayerCardRemoved(ICard card) =>
            _handLayoutManager.RemoveCardFromPlayerHand(card as CardSO);

        private void OnMatchWin()  => Debug.Log("Match Won");
        private void OnMatchLoss() => Debug.Log("Match Lost");

        private void OnPlayerDeckStateChanged(DeckState state) =>
            Debug.Log($"Player deck: {state}");

        private void OnPlayerHandStateChanged(HandState state) =>
            Debug.Log($"Player hand: {state}");

        private void OnEnemyDeckStateChanged(DeckState state) =>
            Debug.Log($"Enemy deck: {state}");
    }
}