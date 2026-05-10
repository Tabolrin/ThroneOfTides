using ThroneOfTides.Core;
using ThroneOfTides.Systems;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public class GameOverState : IState
    {
        private readonly GameState _gameState;

        public GameOverState(GameState gameState)
        {
            _gameState = gameState;
        }

        public void Enter()
        {
            Winner winner = _gameState.GetWinner();
            Debug.Log($"Game Over - Winner: {winner}");

            if (winner == Winner.Player)
                GameEventBus.FireMatchWin();
            else
                GameEventBus.FireMatchLoss();
        }

        public void Tick()  { }
        public void Exit()  { }
    }
}