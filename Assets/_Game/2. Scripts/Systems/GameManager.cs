using System.Collections;
using System.Linq;
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
        [SerializeField] private ResultsPanel _resultsPanel;
        [SerializeField] private HandLayoutManager     _handLayoutManager;
        [SerializeField] private UnityEngine.UI.Button _endTurnButton;
        [SerializeField] private RectTransform         _playZone;
        [SerializeField] private DeadMansTurnPrompt    _deadMansTurnPrompt;

        [Header("Captain")]
        [SerializeField] private CaptainSO _enemyCaptain;

        private GameState                 _gameState;
        private TurnStateMachine          _stateMachine;
        private ThroneOfTidesInputActions _inputActions;
        private EnemyAI                   _enemyAI;

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
            _enemyAI   = new EnemyAI(_enemyCaptain);

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

            for (int i = 0; i < _config.MaxHandSize; i++)
            {
                CardSO card = _gameState.EnemyDeck.Draw();
                if (card == null) break;
                _gameState.EnemyHand.AddCard(card, _config.MaxHandSize);
                _handLayoutManager.AddCardToEnemyHand(card);
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

            if (_gameState.ComboStackCount > 0 && !_gameState.DamageCardPlayedThisTurn)
                _gameState.ResetCombo();

            if (_gameState.SirenSongActive && !_gameState.DamageCardPlayedThisTurn)
                _gameState.ClearSiren();

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
                    if (card.ComboStackBonus > 0)
                    {
                        _gameState.IncrementCombo(card);
                        Debug.Log($"Gunpowder primed - stack: {_gameState.ComboStackCount}");
                        return 0;
                    }
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
                    if (card.ActionEffect != null)
                    {
                        var context = new CardEffectContext(_gameState, _handLayoutManager);
                        card.ActionEffect.Execute(context);
                    }
                    else
                        Debug.LogWarning($"Action card {card.Name} has no ActionEffect assigned");
                    return 0;

                case CardType.Weapon:
                    if (card.Name == "Boarding Party")
                    {
                        _gameState.ApplyDamage(DamageTarget.Player, 2);
                        Debug.Log("Boarding Party - sacrificed 2 HP");
                    }
                    if (card.Name == "Tidal Wave" && _gameState.ComboStackCount > 0)
                    {
                        _gameState.ResetCombo();
                        Debug.Log("Tidal Wave - combo broken");
                    }
                    return card.Damage;

                default:
                    return card.Damage;
            }
        }

        private void OnEnemyTurnReady() => StartCoroutine(EnemyTurnRoutine());

        private IEnumerator EnemyTurnRoutine()
        {
            float delay = Random.Range(_config.EnemyThinkTimeMin, _config.EnemyThinkTimeMax);
            yield return new WaitForSeconds(delay);

            // Draw enemy card into logical and visual hand
            if (_gameState.EnemyHand.Count < _config.MaxHandSize)
            {
                CardSO enemyDrawn = _gameState.EnemyDeck.Draw();
                if (enemyDrawn != null)
                {
                    _gameState.EnemyHand.AddCard(enemyDrawn, _config.MaxHandSize);
                    _handLayoutManager.AddCardToEnemyHand(enemyDrawn);
                }
            }

            // AI picks card based on captain weights
            CardSO playedCard = _enemyAI.PickCard(
                _gameState.EnemyHand.CardsSO,
                damageCardPlayed: false,
                actionCardPlayed: false);

            if (playedCard == null)
            {
                Debug.Log("Enemy has no valid card to play - skipping turn");
                _stateMachine.TransitionTo(_stateMachine.PlayerTurn);
                yield break;
            }

            _gameState.EnemyHand.RemoveCard(playedCard);
            _gameState.DiscardEnemyCard(playedCard);

            // Animate enemy card to play zone and reveal it
            bool animationDone = false;
            yield return StartCoroutine(
                _handLayoutManager.PlayEnemyCardAnimation(
                    playedCard, _playZone, () => animationDone = true));
            yield return new WaitUntil(() => animationDone);

            // Check if attack is blockable
            bool isKraken        = playedCard.Name == "The Kraken";
            bool playerHasDMT    = _gameState.PlayerHand.CardsSO.Any(c => c.Name == "Dead Man's Turn");
            bool playerHasKraken = _gameState.PlayerHand.CardsSO.Any(c => c.Name == "The Kraken");
            bool isAttackCard    = playedCard.CardType == CardType.Weapon ||
                                   playedCard.CardType == CardType.Combo  ||
                                   playedCard.CardType == CardType.DOT;

            bool canBlock = isAttackCard && !_gameState.SirenSongActive &&
                            ((isKraken && playerHasKraken) || (!isKraken && playerHasDMT));

            if (canBlock)
            {
                string blockCost   = isKraken ? "The Kraken (3 HP + 33% materials)" : "Dead Man's Turn";
                bool   wasNegated  = false;
                bool   playerChose = false;

                _deadMansTurnPrompt.Show(
                    playedCard,
                    playedCard.Damage,
                    blockCost,
                    onNegate:  () => { wasNegated = true;  playerChose = true; },
                    onTakeHit: () => { wasNegated = false; playerChose = true; }
                );

                yield return new WaitUntil(() => playerChose);

                if (wasNegated)
                {
                    string blockCardName = isKraken ? "The Kraken" : "Dead Man's Turn";
                    CardSO blockCard = _gameState.PlayerHand.CardsSO
                        .FirstOrDefault(c => c.Name == blockCardName);

                    if (blockCard != null)
                    {
                        _gameState.PlayerHand.RemoveCard(blockCard);
                        _gameState.NotifyPlayerCardRemoved(blockCard);
                        _gameState.DiscardPlayerCard(blockCard);

                        // TODO - deduct 33% materials when material system is built
                        if (isKraken)
                            _gameState.ApplyDamage(DamageTarget.Player, 3);
                    }
                    Debug.Log($"Attack negated - {playedCard.Name} blocked");
                }
                else
                {
                    _gameState.ApplyDamage(DamageTarget.Player, playedCard.Damage);
                    Debug.Log($"Took hit from {playedCard.Name} - damage: {playedCard.Damage}");
                }
            }
            else if (isAttackCard)
            {
                _gameState.ApplyDamage(DamageTarget.Player, playedCard.Damage);
                Debug.Log($"Enemy played: {playedCard.Name} - damage: {playedCard.Damage}");
            }

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

        private void OnMatchWin()
        {
            _endTurnButton.interactable = false;
            _resultsPanel.ShowWin(_enemyCaptain.LevelReward, _gameState.PlayerHP);
        }

        private void OnMatchLoss()
        {
            _endTurnButton.interactable = false;
            _resultsPanel.ShowLoss(_enemyCaptain.LevelReward, _gameState.PlayerHP);
        }

        private void OnPlayerDeckStateChanged(DeckState state) =>
            Debug.Log($"Player deck: {state}");

        private void OnPlayerHandStateChanged(HandState state) =>
            Debug.Log($"Player hand: {state}");

        private void OnEnemyDeckStateChanged(DeckState state) =>
            Debug.Log($"Enemy deck: {state}");
    }
}