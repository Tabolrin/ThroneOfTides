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

        public void Enter() => Debug.Log($"Game Over - Winner: {_gameState.GetWinner()}");
        public void Tick()  { }
        public void Exit()  { }
    }
}