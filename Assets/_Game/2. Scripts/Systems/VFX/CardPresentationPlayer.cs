// Assets/_Game/2. Scripts/Systems/VFX/CardPresentationPlayer.cs
using UnityEngine;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Spawns each played card's authored CardPresentationEntry list — sprite-only prefabs or
    /// paired UI-sprite + world-particle prefabs, at the anchor each entry specifies, with its
    /// SFX cue. Reacts to both player and enemy plays, closing the gap where enemy-played cards
    /// previously got no per-card VFX at all.
    ///
    /// Deliberately separate from CardVFXHandler, which owns generic system-wide combat feedback
    /// (hit impact, HP/mana bars, win/loss) — a different responsibility from per-card unique
    /// presentation. Migrate a card here by populating its PresentationEntries, then delete that
    /// card's now-redundant case from CardVFXHandler.PlayCardVFX so it doesn't double-fire.
    /// </summary>
    public class CardPresentationPlayer : MonoBehaviour
    {
        [Header("Ship Anchors")]
        [Tooltip("The ShipVfxAnchors on the player's ship — resolves anchor points when the player is the caster or the opponent.")]
        [SerializeField] private ShipVfxAnchors _playerShipAnchors;

        [Tooltip("The ShipVfxAnchors on the enemy's ship — resolves anchor points when the enemy is the caster or the opponent.")]
        [SerializeField] private ShipVfxAnchors _enemyShipAnchors;

        [Header("UI")]
        [Tooltip("The Match scene's main HUD/gameplay Canvas RectTransform, used to convert world spawn points into canvas-local positions for Ui Sprite With World Particle entries.")]
        [SerializeField] private RectTransform _gameCanvasRect;

        [Tooltip("The camera used to convert world spawn points into screen/canvas space. Should match the camera CardVFXHandler uses.")]
        [SerializeField] private Camera _gameCamera;

        [Tooltip("Parent RectTransform that spawned UI-mode sprite prefabs are instantiated under (e.g. a full-stretch child of the gameplay Canvas).")]
        [SerializeField] private Transform _uiEffectParent;

        [Header("World")]
        [Tooltip("Parent Transform that spawned world-space prefabs (world sprites and particles) are instantiated under, purely to keep the scene hierarchy tidy — does not affect their spawn position.")]
        [SerializeField] private Transform _worldEffectParent;

        private void OnEnable()
        {
            GameEventBus.OnCardPlayAccepted += HandlePlayerCardPlayed;
            GameEventBus.OnEnemyCardPlayed += HandleEnemyCardPlayed;
        }

        private void OnDisable()
        {
            GameEventBus.OnCardPlayAccepted -= HandlePlayerCardPlayed;
            GameEventBus.OnEnemyCardPlayed -= HandleEnemyCardPlayed;
        }

        private void HandlePlayerCardPlayed(ICard card) => Play(card, CardCasterFilter.Player);
        private void HandleEnemyCardPlayed(ICard card) => Play(card, CardCasterFilter.Enemy);

        private void Play(ICard card, CardCasterFilter caster)
        {
            if (!(card is CardSO cardSO)) return;

            var (casterAnchors, opponentAnchors) = ResolveShipAnchors(caster);

            foreach (var entry in cardSO.PresentationEntries)
            {
                if (!entry.MatchesCaster(caster)) continue;

                PlayEntry(entry, card, caster, casterAnchors, opponentAnchors);
            }
        }

        private void PlayEntry(
            CardPresentationEntry entry,
            ICard card,
            CardCasterFilter caster,
            ShipVfxAnchors casterAnchors,
            ShipVfxAnchors opponentAnchors)
        {
            if (entry.SpritePrefab == null)
            {
                Debug.LogWarning($"{card.Name}: a presentation entry has no Sprite Prefab assigned — skipping.");
                return;
            }

            // Resolve the same PositionType on both ships, not just the entry's own side, so a
            // self-driving effect can animate between the caster's and opponent's matching
            // points (e.g. a mana-pull arc between both ships' hit points).
            var casterPoint = casterAnchors.Get(entry.PositionType);
            var opponentPoint = opponentAnchors.Get(entry.PositionType);
            var spawnTransform = entry.AnchorSide == CardPresentationSide.Caster ? casterPoint : opponentPoint;

            GameObject spriteInstance = entry.PresentationMode == CardPresentationMode.UiSpriteWithWorldParticle
                ? SpawnUiSprite(entry, spawnTransform)
                : SpawnWorldSprite(entry, spawnTransform);

            if (entry.PresentationMode == CardPresentationMode.UiSpriteWithWorldParticle && entry.WorldParticlePrefab != null)
            {
                var particleInstance = Instantiate(
                    entry.WorldParticlePrefab, spawnTransform.position, spawnTransform.rotation, _worldEffectParent);
                Destroy(particleInstance, entry.Lifetime);
            }

            if (spriteInstance.TryGetComponent<ICardPlayEffect>(out var playEffect))
            {
                var context = new CardEffectSpawnContext(
                    casterPoint,
                    opponentPoint,
                    _gameCanvasRect,
                    _gameCamera,
                    card,
                    caster);

                playEffect.Initialize(context);
                playEffect.Completed += () => Destroy(spriteInstance);
            }
            else
            {
                Destroy(spriteInstance, entry.Lifetime);
            }

            CardSfxPlayer.Play(entry.Sfx, spawnTransform.position);
        }

        private GameObject SpawnWorldSprite(CardPresentationEntry entry, Transform spawnTransform)
        {
            return Instantiate(entry.SpritePrefab, spawnTransform.position, spawnTransform.rotation, _worldEffectParent);
        }

        private GameObject SpawnUiSprite(CardPresentationEntry entry, Transform spawnTransform)
        {
            var instance = Instantiate(entry.SpritePrefab, _uiEffectParent);

            if (instance.TryGetComponent<RectTransform>(out var rect))
            {
                rect.anchoredPosition = WorldToCanvasLocalPoint(spawnTransform.position);
            }

            return instance;
        }

        private Vector2 WorldToCanvasLocalPoint(Vector3 worldPosition)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_gameCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _gameCanvasRect, screenPoint, _gameCamera, out var localPoint);
            return localPoint;
        }

        private (ShipVfxAnchors caster, ShipVfxAnchors opponent) ResolveShipAnchors(CardCasterFilter caster)
        {
            return caster == CardCasterFilter.Player
                ? (_playerShipAnchors, _enemyShipAnchors)
                : (_enemyShipAnchors, _playerShipAnchors);
        }
    }
}
