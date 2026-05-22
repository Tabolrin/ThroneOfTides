using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public class PlayerTurnState : IState
    {
        private readonly GameState        _gameState;
        private readonly TurnStateMachine _machine;
        private readonly GameConfigSO     _config;

        public PlayerTurnState(GameState gameState, TurnStateMachine machine, GameConfigSO config)
        {
            _gameState = gameState;
            _machine   = machine;
            _config    = config;
        }

        public void Enter()
        {
            // Reset turn tracking - no auto draw, player clicks deck
            _gameState.ResetTurnCardPlays();
            _gameState.IsPlayerTurn = true;
            GameEventBus.FireTurnPhaseChanged(TurnPhase.Draw);
            Debug.Log($"Player Turn - HP: {_gameState.PlayerHP}, Combo: {_gameState.ComboStackCount}");
        }

        public void Tick() { }

        public void Exit() => Debug.Log("Player Turn Ended");
    }
}