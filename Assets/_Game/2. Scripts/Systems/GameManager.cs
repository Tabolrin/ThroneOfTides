using System.Collections;
using UnityEngine;
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
        [SerializeField] private GameHUD           _gameHUD;
        [SerializeField] private HandLayoutManager _handLayoutManager;

        private GameState        _gameState;
        private TurnStateMachine _stateMachine;

        private void Start()
        {
            var playerDeck = new Deck(_playerDeckDefinition.BuildDeck(), _config.LowDeckThreshold);
            var enemyDeck  = new Deck(_enemyDeckDefinition.BuildDeck(),  _config.LowDeckThreshold);

            _gameState = new GameState(_config.StartingHP, playerDeck, enemyDeck);

            SubscribeToStateEvents();

            _stateMachine = new TurnStateMachine(_gameState, _config);
            _stateMachine.SetOnCardDrawn(_handLayoutManager.AddCardToPlayerHand);
            _stateMachine.SetCoroutineRunner(e => StartCoroutine(e));

            RefreshHUD();
        }

        private void Update()
        {
            _stateMachine?.Tick();
        }

        private void SubscribeToStateEvents()
        {
            _gameState.PlayerDeck.OnDeckStateChanged += OnPlayerDeckStateChanged;
            _gameState.PlayerHand.OnHandStateChanged += OnPlayerHandStateChanged;
            _gameState.EnemyDeck.OnDeckStateChanged  += OnEnemyDeckStateChanged;
            _gameState.OnEnemyTurnReady              += OnEnemyTurnReady;
        }

        private void OnEnemyTurnReady()
        {
            StartCoroutine(EnemyTurnRoutine());
        }

        private IEnumerator EnemyTurnRoutine()
        {
            float delay = UnityEngine.Random.Range(_config.EnemyThinkTimeMin, _config.EnemyThinkTimeMax);
            yield return new WaitForSeconds(delay);

            _gameState.ApplyDamage(DamageTarget.Player, 1);
            RefreshHUD();

            _stateMachine.TransitionTo(
                _gameState.IsGameOver()
                    ? (IState)_stateMachine.GameOver
                    : _stateMachine.PlayerTurn);
        }

        private void RefreshHUD()
        {
            _gameHUD.Refresh(
                _gameState.PlayerHP, _config.StartingHP,
                _gameState.EnemyHP,  _config.StartingHP,
                _gameState.PlayerDeck.Count,
                _gameState.IsPlayerTurn);
        }

        // Placeholder handlers - replace with UI reactions in future steps
        private void OnPlayerDeckStateChanged(DeckState state) =>
            Debug.Log($"Player deck: {state}");

        private void OnPlayerHandStateChanged(HandState state) =>
            Debug.Log($"Player hand: {state}");

        private void OnEnemyDeckStateChanged(DeckState state) =>
            Debug.Log($"Enemy deck: {state}");
    }
}