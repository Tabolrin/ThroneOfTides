using ThroneOfTides.Core;
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
            _gameState.IsPlayerTurn = false;
            GameEventBus.FireTurnPhaseChanged(TurnPhase.EnemyDraw);

            // Notify GameManager to run the coroutine - draw and damage handled there
            _gameState.NotifyEnemyTurnReady();
        }

        public void Tick()  { }
        public void Exit()  => Debug.Log("Enemy Turn Ended");
    }
}