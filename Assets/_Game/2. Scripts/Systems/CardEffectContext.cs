using System.Collections.Generic;
using System.Linq;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    // Implements ICardEffectContext - wraps GameState
    // Keeps GameState internals protected from ActionEffectSO subclasses
    public class CardEffectContext : ICardEffectContext
    {
        private readonly GameState _gameState;

        public int PlayerHP        => _gameState.PlayerHP;
        public int EnemyHP         => _gameState.EnemyHP;
        public int PlayerDeckCount => _gameState.PlayerDeck.Count;
        public int EnemyDeckCount  => _gameState.EnemyDeck.Count;

        public CardEffectContext(GameState gameState)
        {
            _gameState = gameState;
        }

        public void ApplyDamage(DamageTarget target, int amount) =>
            _gameState.ApplyDamage(target, amount);

        public void HealPlayer(int amount) =>
            _gameState.HealPlayer(amount);

        public void SetSirenActive() =>
            _gameState.SetSirenActive();

        public void ApplyDot(DamageTarget target, int damagePerTurn, int turns) =>
            _gameState.AddDotEffect(new DotEffect(target, damagePerTurn, turns));

        // Cast to ICard - Hand stores CardSO which implements ICard
        public IReadOnlyList<ICard> GetEnemyHand() =>
            _gameState.EnemyHand.Cards;

        public IReadOnlyList<ICard> GetPlayerHand() =>
            _gameState.PlayerHand.Cards;
    }
}