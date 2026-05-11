using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public class EnemyTurnState : IState
    {
        private readonly GameState        _gameState;
        private readonly TurnStateMachine _machine;

        public EnemyTurnState(GameState gameState, TurnStateMachine machine)
        {
            _gameState = gameState;
            _machine   = machine;
        }

        public void Enter()
        {
            CardSO drawn = _gameState.EnemyDeck.Draw();
            if (drawn != null)
                _gameState.EnemyHand.AddCard(drawn, 5);

            _gameState.IsPlayerTurn = false;
            GameEventBus.FireTurnPhaseChanged(TurnPhase.EnemyDraw);

            // Notify GameManager to run the coroutine
            _gameState.NotifyEnemyTurnReady();
        }

        public void Tick()  { }
        public void Exit()  => Debug.Log("Enemy Turn Ended");
    }
}