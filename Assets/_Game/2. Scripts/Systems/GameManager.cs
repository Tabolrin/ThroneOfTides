using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using ThroneOfTides.Systems;
using ThroneOfTides.UI;

namespace ThroneOfTides.Systems
{
    public class GameManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfigSO _config;

        [Header("Decks")]
        [SerializeField] private DeckDefinitionSO _playerDeckDefinition;
        [SerializeField] private DeckDefinitionSO _enemyDeckDefinition;

        [Header("References")]
        [SerializeField] private GameHUD               _gameHUD;
        [SerializeField] private HandLayoutManager     _handLayoutManager;
        [SerializeField] private UnityEngine.UI.Button _endTurnButton;

        private GameState                 _gameState;
        private TurnStateMachine          _stateMachine;
        private ThroneOfTidesInputActions _inputActions;

        private void Awake()
        {
            _inputActions = new ThroneOfTidesInputActions();
            _inputActions.Gameplay.EndTurn.performed += OnEndTurnPressed;
        }

        private void OnEnable()  => _inputActions.Enable();
        private void OnDisable() => _inputActions.Disable();

        private void Start()
        {
            var playerDeck = new Deck(_playerDeckDefinition.BuildDeck(), _config.LowDeckThreshold);
            var enemyDeck  = new Deck(_enemyDeckDefinition.BuildDeck(),  _config.LowDeckThreshold);

            _gameState = new GameState(_config.StartingHP, playerDeck, enemyDeck);

            SubscribeToEvents();
            DealOpeningHand();

            _stateMachine = new TurnStateMachine(_gameState, _config);
            _stateMachine.SetOnCardDrawn(_handLayoutManager.AddCardToPlayerHand);
            _stateMachine.SetCoroutineRunner(e => StartCoroutine(e));

            _endTurnButton.onClick.AddListener(() => EndTurn());

            RefreshHUD();
        }

        private void DealOpeningHand()
        {
            for (int i = 0; i < _config.MaxHandSize; i++)
            {
                CardSO card = _gameState.PlayerDeck.Draw();
                if (card == null) break;
                _gameState.PlayerHand.AddCard(card, _config.MaxHandSize);
                _handLayoutManager.AddCardToPlayerHand(card);
            }
        }

        private void Update() => _stateMachine?.Tick();

        private void SubscribeToEvents()
        {
            _gameState.PlayerDeck.OnDeckStateChanged += OnPlayerDeckStateChanged;
            _gameState.PlayerHand.OnHandStateChanged += OnPlayerHandStateChanged;
            _gameState.EnemyDeck.OnDeckStateChanged  += OnEnemyDeckStateChanged;
            _gameState.OnEnemyTurnReady              += OnEnemyTurnReady;

            GameEventBus.OnCardPlayed        += OnCardPlayed;
            GameEventBus.OnPlayerCardRemoved += OnPlayerCardRemoved;
            GameEventBus.OnMatchWin          += OnMatchWin;
            GameEventBus.OnMatchLoss         += OnMatchLoss;
        }

        private void OnDestroy()
        {
            _inputActions.Gameplay.EndTurn.performed -= OnEndTurnPressed;
            _inputActions.Dispose();
            _endTurnButton.onClick.RemoveAllListeners();
            GameEventBus.ClearAllListeners();
        }

        private void OnEndTurnPressed(InputAction.CallbackContext context) => EndTurn();

        private void EndTurn()
        {
            if (!_gameState.IsPlayerTurn) return;

            // Break combo if no combo card played this turn
            if (_gameState.ComboStackCount > 0 && !_gameState.DamageCardPlayedThisTurn)
                _gameState.ResetCombo();

            _stateMachine.TransitionTo(_stateMachine.EnemyTurn);
        }

        private void OnCardPlayed(ICard card)
        {
            var cardSO = card as CardSO;
            if (cardSO == null) return;

            if (!_gameState.CanPlayCard(cardSO))
            {
                Debug.Log($"Cannot play {cardSO.Name} - card play limit reached");
                return;
            }

            _gameState.RegisterCardPlayed(cardSO);
            _gameState.PlayerHand.RemoveCard(cardSO);
            _gameState.NotifyPlayerCardRemoved(cardSO);
            _gameState.DiscardPlayerCard(cardSO);

            int damage = ResolveDamage(cardSO);
            if (damage > 0)
                _gameState.ApplyDamage(DamageTarget.Enemy, damage);

            RefreshHUD();

            if (_gameState.IsGameOver())
            {
                if (_gameState.GetWinner() == Winner.Player)
                    GameEventBus.FireMatchWin();
                else
                    GameEventBus.FireMatchLoss();
            }
        }

        private int ResolveDamage(CardSO card)
        {
            switch (card.CardType)
            {
                case CardType.Combo:
                    // ComboStackBonus > 0 = initiator (Gunpowder)
                    if (card.ComboStackBonus > 0)
                    {
                        _gameState.IncrementCombo(card);
                        Debug.Log($"Gunpowder primed - stack: {_gameState.ComboStackCount}");
                        return 0;
                    }
                    // Torch - resolve if combo active
                    if (_gameState.ComboStackCount > 0 && _gameState.ActiveComboCard != null)
                    {
                        int comboDamage = _gameState.ResolveCombo();
                        Debug.Log($"Combo resolved - damage: {comboDamage}");
                        return comboDamage;
                    }
                    Debug.Log("Torch with no active Gunpowder - base damage only");
                    return card.Damage;

                case CardType.DOT:
                    _gameState.AddDotEffect(new DotEffect(DamageTarget.Enemy, card.DotDamagePerTurn, card.DotDuration));
                    Debug.Log($"DOT applied - {card.DotDamagePerTurn} dmg for {card.DotDuration} turns");
                    return 0;

                case CardType.Action:
                    // TODO - route to action effect system
                    Debug.Log($"Action card played: {card.Name}");
                    return 0;

                default:
                    return card.Damage;
            }
        }

        private void OnEnemyTurnReady() => StartCoroutine(EnemyTurnRoutine());

        private IEnumerator EnemyTurnRoutine()
        {
            float delay = Random.Range(_config.EnemyThinkTimeMin, _config.EnemyThinkTimeMax);
            yield return new WaitForSeconds(delay);
            
            // Draw enemy card into logical hand - no visual spawn
            if (_gameState.EnemyHand.Count < _config.MaxHandSize)
            {
                CardSO enemyDrawn = _gameState.EnemyDeck.Draw();
                if (enemyDrawn != null)
                    _gameState.EnemyHand.AddCard(enemyDrawn, _config.MaxHandSize);
            }

            // TODO - replace with real AI card selection when combat system is built
            if (_gameState.EnemyHand.Count > 0)
            {
                CardSO playedCard = _gameState.EnemyHand.CardsSO[0];
                _gameState.EnemyHand.RemoveCard(playedCard);
                _gameState.DiscardEnemyCard(playedCard);
                _gameState.ApplyDamage(DamageTarget.Player, playedCard.Damage);
                Debug.Log($"Enemy played: {playedCard.Name} - damage: {playedCard.Damage}");
            }

            // Process player DOT effects after enemy acts
            _gameState.ProcessDotEffects();

            RefreshHUD();

            if (_gameState.IsGameOver())
            {
                if (_gameState.GetWinner() == Winner.Player)
                    GameEventBus.FireMatchWin();
                else
                    GameEventBus.FireMatchLoss();
                yield break;
            }

            _stateMachine.TransitionTo(_stateMachine.PlayerTurn);
        }

        private void RefreshHUD()
        {
            _gameHUD.Refresh(
                _gameState.PlayerHP, _config.StartingHP,
                _gameState.EnemyHP,  _config.StartingHP,
                _gameState.PlayerDeck.Count,
                _gameState.IsPlayerTurn);

            _endTurnButton.interactable = _gameState.IsPlayerTurn;
        }

        private void OnPlayerCardRemoved(ICard card) =>
            _handLayoutManager.RemoveCardFromPlayerHand(card as CardSO);

        private void OnMatchWin()  => Debug.Log("Match Won");
        private void OnMatchLoss() => Debug.Log("Match Lost");

        private void OnPlayerDeckStateChanged(DeckState state) =>
            Debug.Log($"Player deck: {state}");

        private void OnPlayerHandStateChanged(HandState state) =>
            Debug.Log($"Player hand: {state}");

        private void OnEnemyDeckStateChanged(DeckState state) =>
            Debug.Log($"Enemy deck: {state}");
    }
}