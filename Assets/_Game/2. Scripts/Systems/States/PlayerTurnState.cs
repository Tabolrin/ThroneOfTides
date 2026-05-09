using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ThroneOfTides.Data;

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
            if (_gameState.PlayerHand.Count < _config.MaxHandSize)
            {
                CardSO drawn = _gameState.PlayerDeck.Draw();
                if (drawn != null)
                {
                    _gameState.PlayerHand.AddCard(drawn, _config.MaxHandSize);
                    _onCardDrawn?.Invoke(drawn);
                }
            }

            _gameState.IsPlayerTurn = true;
            Debug.Log($"Player Turn - HP: {_gameState.PlayerHP}, Combo: {_gameState.ComboStackCount}");
        }

        public void Tick()
        {
            if (Keyboard.current == null) return;

            // TODO - replace Space with End Turn button
            if (!Keyboard.current.spaceKey.wasPressedThisFrame) return;

            if (_gameState.ComboStackCount > 0)
                _gameState.ResetCombo();

            _machine.TransitionTo(_machine.EnemyTurn);
        }

        public void Exit() => Debug.Log("Player Turn Ended");
    }
}