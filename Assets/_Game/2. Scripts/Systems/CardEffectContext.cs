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
        public DamageTarget  Caster { get; }
        public DamageTarget? SelectedTarget { get; }

        public CardEffectContext(GameState gameState, IHandLayoutManager handLayout,
                                 DamageTarget caster = DamageTarget.Player,
                                 System.Func<bool> secondaryDraw = null,
                                 DamageTarget? selectedTarget = null)
        {
            _gameState     = gameState;
            _handLayout    = handLayout;
            Caster         = caster;
            _secondaryDraw = secondaryDraw;
            SelectedTarget = selectedTarget;
        }

        public void ApplyDamage(DamageTarget target, int amount) =>
            _gameState.ApplyDamage(target, amount);

        // Heals whichever side cast this card — works for either side so Rum behaves
        // correctly when the enemy plays it too.
        public void HealPlayer(int amount)
        {
            if (Caster == DamageTarget.Player)
                _gameState.HealPlayer(amount);
            else
                _gameState.HealEnemy(amount);
        }

        public void SetSirenActive() =>
            _gameState.SetSirenActive(Caster);

        public void ApplyDot(DamageTarget target, int damagePerTurn, int turns, ShipStatusType source = ShipStatusType.None) =>
            _gameState.AddDotEffect(new DotEffect(target, damagePerTurn, turns, source));

        public void ClearComboStack(DamageTarget target) =>
            _gameState.ResetCombo(target);

        public void RegisterHighSpiritsPlayed() =>
            _gameState.RegisterHighSpiritsPlayed();

        public void SpendPlayerMana(int amount) =>
            _gameState.SpendPlayerMana(amount);

        public void AddPlayerMaxMana(int amount) =>
            _gameState.AddPlayerMaxMana(amount);

        // Steals mana from whoever did NOT cast this card, into the caster's own pool — works
        // for either side so Stolen Wind/Essence Plunder behave correctly when the enemy plays them.
        public void StealEnemyMana(int amount)
        {
            if (Caster == DamageTarget.Player)
                _gameState.StealEnemyMana(amount);
            else
                _gameState.StealPlayerMana(amount);
        }

        public void AddDeadMansTurnCharge() =>
            _gameState.AddDeadMansTurnCharge();

        public void AddCounterGaleCharge() =>
            _gameState.AddCounterGaleCharge();

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

        // Steals a random card from whoever did NOT cast this card, into the caster's own hand —
        // works for either side so Monkey Grab behaves correctly when the enemy plays it too.
        public void StealFromEnemyHand()
        {
            if (Caster == DamageTarget.Player)
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
            else
            {
                var playerHand = _gameState.PlayerHand.CardsSO;
                if (playerHand.Count == 0) return;

                int    index = UnityEngine.Random.Range(0, playerHand.Count);
                CardSO card  = playerHand[index];
                _gameState.PlayerHand.RemoveCard(card);
                _gameState.EnemyHand.AddCard(card, _gameState.EnemyDeck.Count);
                _handLayout.RemoveCardFromPlayerHand(card);
                _handLayout.AddCardToEnemyHand(card);
            }
        }

        // Discards a random card from whoever did NOT cast this card's hand — works for either
        // side so Chain Shot behaves correctly when the enemy plays it too.
        public void DiscardRandomFromOpponentHand()
        {
            bool opponentIsEnemy = Caster == DamageTarget.Player;
            var hand = opponentIsEnemy ? _gameState.EnemyHand.CardsSO : _gameState.PlayerHand.CardsSO;
            if (hand.Count == 0) return;

            int    index = UnityEngine.Random.Range(0, hand.Count);
            CardSO card  = hand[index];

            if (opponentIsEnemy)
            {
                _gameState.EnemyHand.RemoveCard(card);
                _gameState.DiscardEnemyCard(card);
            }
            else
            {
                _gameState.PlayerHand.RemoveCard(card);
                _gameState.DiscardPlayerCard(card);
            }
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