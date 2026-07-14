// Assets/_Game/2. Scripts/Systems/VFX/CardPresentationPlayer.cs
using UnityEngine;
using UnityEngine.UI;
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
        [Tooltip("The ShipVfxAnchors on the player's ship — resolves anchor points when the player is the caster, opponent, or explicitly chosen target.")]
        [SerializeField] private ShipVfxAnchors _playerShipAnchors;

        [Tooltip("The ShipVfxAnchors on the enemy's ship — resolves anchor points when the enemy is the caster, opponent, or explicitly chosen target.")]
        [SerializeField] private ShipVfxAnchors _enemyShipAnchors;

        [Header("UI")]
        [Tooltip("The Match scene's main HUD/gameplay Canvas RectTransform, used to convert world spawn points into canvas-local positions for Ui Sprite With World Particle entries.")]
        [SerializeField] private RectTransform _gameCanvasRect;

        [Tooltip("The camera used to convert world spawn points into screen/canvas space. Should match the camera CardVFXHandler uses.")]
        [SerializeField] private Camera _gameCamera;

        [Tooltip("Persistent scene-level ParticleSystem reused by Siren Song's music notes — sized once in the editor and repositioned each use, never destroyed.")]
        [SerializeField] private ParticleSystem _musicNoteParticles;

        [Tooltip("Persistent scene-level ParticleSystem reused by Lightning's strike burst.")]
        [SerializeField] private ParticleSystem _lightningStrikeParticles;

        [Tooltip("Persistent full-screen scene Image used for Lightning's whiteout flash.")]
        [SerializeField] private Image _whiteoutImage;

        [Tooltip("Persistent scene-level ParticleSystem reused by Hail Storm's falling hail.")]
        [SerializeField] private ParticleSystem _hailParticles;

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

        private void HandlePlayerCardPlayed(ICard card, DamageTarget? target) => Play(card, CardCasterFilter.Player, target);
        private void HandleEnemyCardPlayed(ICard card, DamageTarget? target) => Play(card, CardCasterFilter.Enemy, target);

        private void Play(ICard card, CardCasterFilter caster, DamageTarget? explicitTarget)
        {
            if (!(card is CardSO cardSO)) return;

            var (casterAnchors, opponentAnchors) = ResolveShipAnchors(caster);

            foreach (var entry in cardSO.PresentationEntries)
            {
                if (!entry.MatchesCaster(caster)) continue;

                PlayEntry(entry, card, caster, casterAnchors, opponentAnchors, explicitTarget);
            }
        }

        private void PlayEntry(
            CardPresentationEntry entry,
            ICard card,
            CardCasterFilter caster,
            ShipVfxAnchors casterAnchors,
            ShipVfxAnchors opponentAnchors,
            DamageTarget? explicitTarget)
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

            var spawnTransform = entry.AnchorSide switch
            {
                CardPresentationSide.Caster => casterPoint,
                CardPresentationSide.Opponent => opponentPoint,
                CardPresentationSide.ExplicitTarget => ResolveExplicitTargetPoint(entry.PositionType, explicitTarget),
                _ => casterPoint
            };

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
                // Self-driving effects decide when their SFX actually happens (e.g. on impact,
                // not at spawn) — hand them a bound callback instead of firing it here.
                var context = new CardEffectSpawnContext(
                    casterPoint,
                    opponentPoint,
                    _gameCanvasRect,
                    _gameCamera,
                    card,
                    caster,
                    playSfx: pos => CardSfxPlayer.Play(entry.Sfx, pos),
                    musicNoteParticles: _musicNoteParticles,
                    lightningStrikeParticles: _lightningStrikeParticles,
                    whiteoutImage: _whiteoutImage,
                    hailParticles: _hailParticles);

                playEffect.Initialize(context);
                playEffect.Completed += () => Destroy(spriteInstance);
            }
            else
            {
                // Simple spawns have no sequence to sync against — play immediately.
                Destroy(spriteInstance, entry.Lifetime);
                CardSfxPlayer.Play(entry.Sfx, spawnTransform.position);
            }
        }

        private Transform ResolveExplicitTargetPoint(VfxAnchorType positionType, DamageTarget? explicitTarget)
        {
            if (!explicitTarget.HasValue)
            {
                Debug.LogWarning(
                    "A presentation entry uses Explicit Target anchoring but no target was selected for " +
                    "this play (card is missing Requires Target Selection) — falling back to the player's ship.");
                return _playerShipAnchors.Get(positionType);
            }

            var anchors = explicitTarget.Value == DamageTarget.Player ? _playerShipAnchors : _enemyShipAnchors;
            return anchors.Get(positionType);
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
