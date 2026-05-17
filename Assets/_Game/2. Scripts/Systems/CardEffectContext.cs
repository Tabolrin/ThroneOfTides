using System.Collections.Generic;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using ThroneOfTides.UI;

namespace ThroneOfTides.Systems
{
    public class CardEffectContext : ICardEffectContext
    {
        private readonly GameState         _gameState;
        private readonly HandLayoutManager _handLayout;

        public int PlayerHP        => _gameState.PlayerHP;
        public int EnemyHP         => _gameState.EnemyHP;
        public int PlayerDeckCount => _gameState.PlayerDeck.Count;
        public int EnemyDeckCount  => _gameState.EnemyDeck.Count;

        public CardEffectContext(GameState gameState, HandLayoutManager handLayout)
        {
            _gameState  = gameState;
            _handLayout = handLayout;
        }

        public void ApplyDamage(DamageTarget target, int amount) =>
            _gameState.ApplyDamage(target, amount);

        public void HealPlayer(int amount) =>
            _gameState.HealPlayer(amount);

        public void SetSirenActive() =>
            _gameState.SetSirenActive();

        public void ApplyDot(DamageTarget target, int damagePerTurn, int turns) =>
            _gameState.AddDotEffect(new DotEffect(target, damagePerTurn, turns));
        
        public void SetDeadMansTurnActive() =>
            _gameState.SetDeadMansTurnActive();

        public void AddCardToPlayerHand(ICard card)
        {
            var cardSO = card as CardSO;
            if (cardSO == null) return;
            _gameState.PlayerHand.AddCard(cardSO, _gameState.PlayerDeck.Count);
            _handLayout.AddCardToPlayerHand(cardSO);
            GameEventBus.FireCardDrawn(cardSO);
        }

        public void StealFromEnemyHand()
        {
            var enemyHand = _gameState.EnemyHand.CardsSO;
            if (enemyHand.Count == 0) return;

            int    index = UnityEngine.Random.Range(0, enemyHand.Count);
            CardSO card  = enemyHand[index];
            _gameState.EnemyHand.RemoveCard(card);

            // Re-parent visual and add to player hand
            _handLayout.StealCardFromEnemyHand(card);
            _gameState.PlayerHand.AddCard(card, _gameState.PlayerDeck.Count);
            GameEventBus.FireCardDrawn(card);
        }

        public void RetrieveFromDiscard(int count)
        {
            var retrieved = _gameState.RetrieveFromPlayerDiscard(count);
            foreach (var card in retrieved)
                _gameState.PlayerDeck.ReturnCard(card);
        }

        public IReadOnlyList<ICard> GetEnemyHand() => _gameState.EnemyHand.Cards;
        public IReadOnlyList<ICard> GetPlayerHand() => _gameState.PlayerHand.Cards;
    }
}