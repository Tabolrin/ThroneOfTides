// Assets/_Game/2. Scripts/Systems/VFX/LockersReturnVFXController.cs
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.Systems.VFX
{
    // Locker's Return VFX - spawned by CardVFXHandler's legacy PlayCardVFX routing, always at
    // the player's sea-surface-right anchor regardless of which side actually played the card
    // (matching LockerReturnEffectSO's own always-player-deck behavior - RetrieveFromDiscard
    // always returns cards to the player's deck). The card-art tentacle fills from the top while
    // rising, then one card-back flies from the tentacle to the deck per retrieved card,
    // staggered with a delay between each, popping a "+1" over the deck on arrival. The tentacle
    // fades out once every card has landed.
    //
    // Note: the actual card retrieval (LockerReturnEffectSO → RetrieveFromDiscard) still happens
    // instantly the moment the card is played - ActionEffectSO.Execute has no async/staggering
    // capability in this codebase, so this only staggers the VISUAL arrival; the cards are
    // already back in the deck by the time this sequence plays out.
    public class LockersReturnVFXController : MonoBehaviour
    {
        [SerializeField] private Image      _tentacleImage;
        [SerializeField] private GameObject _cardBackPrefab;

        [Header("Fill & Rise")]
        [SerializeField] private float _fillDuration = 0.5f;
        [SerializeField] private Ease  _fillEase     = Ease.OutSine;
        [SerializeField] private float _riseDistance = 1f;
        [SerializeField] private float _riseDuration = 0.5f;
        [SerializeField] private Ease  _riseEase     = Ease.OutSine;

        [Header("Cards")]
        [SerializeField] private int   _cardCount          = 3;
        [SerializeField] private float _delayBetweenCards  = 0.25f;
        [SerializeField] private float _cardFlightDuration = 0.4f;
        [SerializeField] private Ease  _cardFlightEase     = Ease.InOutSine;
        [Tooltip("Uniform scale applied to each spawned card-back - independent of the CardBackVFX prefab's own authored scale.")]
        [SerializeField] private float _cardScale = 1f;

        [Header("Hold & Fade Out")]
        [SerializeField] private float _holdDuration    = 0.2f;
        [SerializeField] private float _fadeOutDuration = 0.4f;

        private Transform     _deckPoint;
        private System.Action _onCardArrived;
        private Sequence      _sequence;

        /// <param name="cardArt">The card's own art sprite - the tentacle fills in as this.</param>
        /// <param name="deckPoint">Always the player's deck point - see class remarks.</param>
        /// <param name="onCardArrived">Invoked once per card-back the instant it reaches the deck (drives the "+1" popup).</param>
        public void Setup(Sprite cardArt, Transform deckPoint, System.Action onCardArrived)
        {
            _deckPoint     = deckPoint;
            _onCardArrived = onCardArrived;

            if (_tentacleImage != null && cardArt != null)
                _tentacleImage.sprite = cardArt;
        }

        private void Awake()
        {
            if (_tentacleImage == null) return;

            _tentacleImage.fillAmount = 0f;
            _tentacleImage.fillMethod = Image.FillMethod.Vertical;
            _tentacleImage.fillOrigin = (int)Image.OriginVertical.Top;
        }

        private void Start() => BuildAndPlaySequence();

        private void BuildAndPlaySequence()
        {
            Vector3 risenPosition = transform.position + Vector3.up * _riseDistance;

            _sequence = DOTween.Sequence();
            _sequence.Append(DOTween.To(() => _tentacleImage.fillAmount, v => _tentacleImage.fillAmount = v, 1f, _fillDuration).SetEase(_fillEase));
            _sequence.Join(transform.DOMove(risenPosition, _riseDuration).SetEase(_riseEase));

            for (int i = 0; i < _cardCount; i++)
            {
                _sequence.AppendCallback(SpawnCardBack);
                if (i < _cardCount - 1) _sequence.AppendInterval(_delayBetweenCards);
            }

            // Give the last card time to actually land before the tentacle starts fading.
            _sequence.AppendInterval(_cardFlightDuration + _holdDuration);
            _sequence.Append(_tentacleImage.DOFade(0f, _fadeOutDuration));
            _sequence.OnComplete(() => Destroy(gameObject));
        }

        private void SpawnCardBack()
        {
            if (_cardBackPrefab == null || _deckPoint == null) return;

            var cardBack = Instantiate(_cardBackPrefab, transform.position, Quaternion.identity);
            cardBack.transform.localScale = Vector3.one * _cardScale;
            cardBack.transform.DOMove(_deckPoint.position, _cardFlightDuration)
                .SetEase(_cardFlightEase)
                .OnComplete(() =>
                {
                    _onCardArrived?.Invoke();
                    Destroy(cardBack);
                });
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
