// Assets/_Game/2. Scripts/Systems/CardEffectContext.cs
using System.Collections.Generic;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    public class CardEffectContext : ICardEffectContext
    {
        private readonly GameState          _gameState;
        private readonly IHandLayoutManager _handLayout;
        private readonly System.Func<bool>  _secondaryDraw;

        public int PlayerHP        => _gameState.PlayerHP;
        public int EnemyHP         => _gameState.EnemyHP;
        public int PlayerDeckCount => _gameState.PlayerDeck.Count;
        public int EnemyDeckCount  => _gameState.EnemyDeck.Count;
        public int PlayerMana      => _gameState.PlayerMana;
        public int PlayerMaxMana   => _gameState.PlayerMaxMana;
        public int EnemyMana       => _gameState.EnemyMana;

        public CardEffectContext(GameState gameState, IHandLayoutManager handLayout,
                                 System.Func<bool> secondaryDraw = null)
        {
            _gameState     = gameState;
            _handLayout    = handLayout;
            _secondaryDraw = secondaryDraw;
        }

        public void ApplyDamage(DamageTarget target, int amount) =>
            _gameState.ApplyDamage(target, amount);

        public void HealPlayer(int amount) =>
            _gameState.HealPlayer(amount);

        public void SetSirenActive() =>
            _gameState.SetSirenActive();

        public void ApplyDot(DamageTarget target, int damagePerTurn, int turns) =>
            _gameState.AddDotEffect(new DotEffect(target, damagePerTurn, turns));

        public void SpendPlayerMana(int amount) =>
            _gameState.SpendPlayerMana(amount);

        public void AddPlayerMaxMana(int amount) =>
            _gameState.AddPlayerMaxMana(amount);

        public void StealEnemyMana(int amount) =>
            _gameState.StealEnemyMana(amount);

        public void AddDeadMansTurnCharge() =>
            _gameState.AddDeadMansTurnCharge();

        public void AddBloodForBloodCharge() =>
            _gameState.AddBloodForBloodCharge();

        public void ReturnFromSnapshot(int count)
        {
            var cards = _gameState.GetRandomFromSnapshot(count);
            foreach (var card in cards)
                _gameState.PlayerDeck.ReturnCard(card);
        }

        public void DrawOneCard() => _secondaryDraw?.Invoke();

        public void AddCardToPlayerHand(ICard card)
        {
            var cardSO = card as CardSO;
            if (cardSO == null) return;
            _gameState.PlayerHand.AddCard(cardSO, _gameState.PlayerDeck.Count);
            _handLayout.AddCardToPlayerHand(card);
            GameEventBus.FireCardDrawn(card);
        }

        public void StealFromEnemyHand()
        {
            var enemyHand = _gameState.EnemyHand.CardsSO;
            if (enemyHand.Count == 0) return;

            int    index = UnityEngine.Random.Range(0, enemyHand.Count);
            CardSO card  = enemyHand[index];
            _gameState.EnemyHand.RemoveCard(card);
            _gameState.PlayerHand.AddCard(card, _gameState.PlayerDeck.Count);
            _handLayout.StealCardFromEnemyHand(card);
            GameEventBus.FireCardDrawn(card);
        }

        public void RetrieveFromDiscard(int count)
        {
            var retrieved = _gameState.RetrieveFromPlayerDiscard(count);
            foreach (var card in retrieved)
                _gameState.PlayerDeck.ReturnCard(card);
        }

        public IReadOnlyList<ICard> GetEnemyHand()  => _gameState.EnemyHand.Cards;
        public IReadOnlyList<ICard> GetPlayerHand() => _gameState.PlayerHand.Cards;
    }
}