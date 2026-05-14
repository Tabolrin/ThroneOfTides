using System;
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

        private Action<CardSO> _onCardDrawn;

        public PlayerTurnState(GameState gameState, TurnStateMachine machine, GameConfigSO config)
        {
            _gameState = gameState;
            _machine   = machine;
            _config    = config;
        }

        public void SetOnCardDrawn(Action<CardSO> callback) => _onCardDrawn = callback;

        public void Enter()
        {
            // Reset card play tracking for this turn
            _gameState.ResetTurnCardPlays();

            if (_gameState.PlayerHand.Count < _config.MaxHandSize)
            {
                CardSO drawn = _gameState.PlayerDeck.Draw();
                if (drawn != null)
                {
                    _gameState.PlayerHand.AddCard(drawn, _config.MaxHandSize);
                    _onCardDrawn?.Invoke(drawn);
                    GameEventBus.FireCardDrawn(drawn);
                }
            }

            _gameState.IsPlayerTurn = true;
            GameEventBus.FireTurnPhaseChanged(TurnPhase.Draw);
            Debug.Log($"Player Turn - HP: {_gameState.PlayerHP}, Combo: {_gameState.ComboStackCount}");
        }

        public void Tick() { }

        public void Exit() => Debug.Log("Player Turn Ended");
    }
}