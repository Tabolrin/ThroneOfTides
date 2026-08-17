// Assets/_Game/2. Scripts/Systems/CardEffectContext.cs
using System.Collections.Generic;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    public class CardEffectContext : ICardEffectContext
    {
        private readonly GameState             _gameState;
        private readonly IHandLayoutManager    _handLayout;
        private readonly System.Func<bool, bool> _secondaryDraw;
        private readonly PlayerInventory       _playerInventory;

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
                                 System.Func<bool, bool> secondaryDraw = null,
                                 DamageTarget? selectedTarget = null,
                                 PlayerInventory playerInventory = null)
        {
            _gameState       = gameState;
            _handLayout      = handLayout;
            Caster           = caster;
            _secondaryDraw   = secondaryDraw;
            SelectedTarget   = selectedTarget;
            _playerInventory = playerInventory;
        }

        public void ApplyDamage(DamageTarget target, int amount) =>
            _gameState.ApplyDamage(target, amount);

        // Heals whichever side cast this card - works for either side so Rum behaves
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
            _gameState.RegisterHighSpiritsPlayed(Caster);

        public void SpendPlayerMana(int amount) =>
            _gameState.SpendPlayerMana(amount);

        // Grants max mana to whoever cast this card - works for either side so Treasure
        // Chest/High Spirits behave correctly when the enemy plays them too.
        public void AddPlayerMaxMana(int amount)
        {
            if (Caster == DamageTarget.Player)
                _gameState.AddPlayerMaxMana(amount);
            else
                _gameState.AddEnemyMaxMana(amount);
        }

        // Steals mana from whoever did NOT cast this card, into the caster's own pool - works
        // for either side so Stolen Wind/Essence Plunder behave correctly when the enemy plays them.
        public void StealEnemyMana(int amount)
        {
            if (Caster == DamageTarget.Player)
                _gameState.StealEnemyMana(amount);
            else
                _gameState.StealPlayerMana(amount);
        }

        // Charges whichever side cast this card - works for either side so an enemy-drawn
        // reaction card charges the enemy's own counter, not the player's.
        public void AddDeadMansTurnCharge() =>
            _gameState.AddDeadMansTurnCharge(Caster);

        public void AddCounterGaleCharge() =>
            _gameState.AddCounterGaleCharge(Caster);

        // Draws from the shared original-deck snapshot pool but returns the cards into whoever
        // cast this card's own deck - works for either side so Treasure Chest behaves correctly
        // when the enemy plays it too.
        public void ReturnFromSnapshot(int count)
        {
            var cards = _gameState.GetRandomFromSnapshot(count);
            var deck  = Caster == DamageTarget.Player ? _gameState.PlayerDeck : _gameState.EnemyDeck;
            foreach (var card in cards)
                deck.ReturnCard(card);
        }

        public void DrawOneCard(bool ignoreHandLimit = false) => _secondaryDraw?.Invoke(ignoreHandLimit);

        public void AddCardToPlayerHand(ICard card)
        {
            var cardSO = card as CardSO;
            if (cardSO == null) return;

            if (_gameState.PlayerHand.Count >= _gameState.MaxHandSize)
            {
                GameDebug.Log($"Player hand at max capacity ({_gameState.MaxHandSize}) - card skipped.");
                GameEventBus.FireMatchNote($"Hand is full (max {_gameState.MaxHandSize}) - a card was skipped.");
                return;
            }

            _gameState.PlayerHand.AddCard(cardSO, _gameState.MaxHandSize);
            _handLayout.AddCardToPlayerHand(card);
            GameEventBus.FireCardDrawn(card);
        }

        // Steals a random card from whoever did NOT cast this card, into the caster's own hand -
        // works for either side so Monkey Grab behaves correctly when the enemy plays it too.
        // The GameState ownership change happens instantly here (Execute() has no async
        // capability), but the persistent hand-visual CardView is deliberately NOT touched -
        // MonkeyGrabVFXController subscribes to OnCardStolen and decides the moment its own
        // "carrying the card home" animation calls context.FinalizeStolenCardVisual, which is
        // what actually invokes _handLayout.StealCardFrom*Hand under the hood.
        //
        // The receiving hand is checked BEFORE removing the card from the victim's hand - Monkey
        // Grab is a guaranteed bonus gain (like Treasure Chest), so it's allowed past the normal
        // MaxHandSize up to GameState.BonusMaxHandSize, but never past that absolute ceiling.
        // Checking first (rather than removing then finding AddCard silently no-ops when full)
        // avoids the card vanishing into neither hand.
        public void StealFromEnemyHand()
        {
            if (Caster == DamageTarget.Player)
            {
                var enemyHand = _gameState.EnemyHand.CardsSO;
                if (enemyHand.Count == 0) return;

                if (_gameState.PlayerHand.Count >= _gameState.BonusMaxHandSize)
                {
                    GameDebug.Log($"Player hand at max capacity ({_gameState.BonusMaxHandSize}) - Monkey Grab steal skipped.");
                    GameEventBus.FireMatchNote($"Hand is full (max {_gameState.BonusMaxHandSize}) - the stolen card was skipped.");
                    return;
                }

                int    index = UnityEngine.Random.Range(0, enemyHand.Count);
                CardSO card  = enemyHand[index];
                _gameState.EnemyHand.RemoveCard(card);
                _gameState.PlayerHand.AddCard(card, _gameState.BonusMaxHandSize);
                GameEventBus.FireCardStolen(card, DamageTarget.Player);
            }
            else
            {
                var playerHand = _gameState.PlayerHand.CardsSO;
                if (playerHand.Count == 0) return;

                if (_gameState.EnemyHand.Count >= _gameState.BonusMaxHandSize)
                {
                    GameDebug.Log($"Enemy hand at max capacity ({_gameState.BonusMaxHandSize}) - Monkey Grab steal skipped.");
                    return;
                }

                int    index = UnityEngine.Random.Range(0, playerHand.Count);
                CardSO card  = playerHand[index];
                _gameState.PlayerHand.RemoveCard(card);
                _gameState.EnemyHand.AddCard(card, _gameState.BonusMaxHandSize);
                GameEventBus.FireCardStolen(card, DamageTarget.Enemy);
            }
        }

        // Discards a random card from whoever did NOT cast this card's hand - works for either
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

        // Retrieves from and returns into whoever cast this card's own discard/deck - works for
        // either side so Locker's Return behaves correctly when the enemy plays it too (it used
        // to always touch the player's discard/deck regardless of caster).
        public void RetrieveFromDiscard(int count)
        {
            bool isPlayer = Caster == DamageTarget.Player;
            var retrieved = isPlayer
                ? _gameState.RetrieveFromPlayerDiscard(count)
                : _gameState.RetrieveFromEnemyDiscard(count);
            var deck = isPlayer ? _gameState.PlayerDeck : _gameState.EnemyDeck;
            foreach (var card in retrieved)
                deck.ReturnCard(card);
        }

        public IReadOnlyList<ICard> GetEnemyHand()  => _gameState.EnemyHand.Cards;
        public IReadOnlyList<ICard> GetPlayerHand() => _gameState.PlayerHand.Cards;

        // Whoever did NOT cast this card's hand - works for either side so Recon Parrot
        // behaves correctly when the enemy plays it too (it used to always reveal the enemy's
        // own hand to itself regardless of caster).
        public IReadOnlyList<ICard> GetOpponentHand() =>
            Caster == DamageTarget.Player ? _gameState.EnemyHand.Cards : _gameState.PlayerHand.Cards;

        // Only the player has a tracked coin balance - a no-op when the enemy casts a card
        // that happens to touch coins (e.g. an enemy Treasure Chest).
        public void AddCoins(int amount)
        {
            if (Caster == DamageTarget.Player)
                _playerInventory?.AddCoins(amount);
        }

        public void LogNote(string message) => GameEventBus.FireMatchNote(message);
    }
}