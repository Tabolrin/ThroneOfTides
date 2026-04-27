using System;
using System.Collections;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    public class TurnStateMachine
    {
        private IState _currentState;

        public PlayerTurnState PlayerTurn { get; private set; }
        public EnemyTurnState  EnemyTurn  { get; private set; }
        public GameOverState   GameOver   { get; private set; }

        public TurnStateMachine(GameState gameState, GameConfigSO config)
        {
            PlayerTurn = new PlayerTurnState(gameState, this, config);
            EnemyTurn  = new EnemyTurnState(gameState, this);
            GameOver   = new GameOverState(gameState);

            TransitionTo(PlayerTurn);
        }

        public void TransitionTo(IState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        public void Tick() => _currentState?.Tick();

        // Passed in from GameManager so states can start coroutines without MonoBehaviour
        public void SetCoroutineRunner(Action<IEnumerator> runner) { }

        public void SetOnCardDrawn(Action<CardSO> callback) =>
            PlayerTurn.SetOnCardDrawn(callback);
    }
}