using System.Collections.Generic;
using UnityEngine;
using ThroneOfTides.Data;
using ThroneOfTides.UI;

namespace ThroneOfTides.Systems
{
    public class HandLayoutManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _playerHandAnchor;
        [SerializeField] private RectTransform _enemyHandAnchor;
        [SerializeField] private CardView      _cardPrefab;
        [SerializeField] private Canvas        _dragCanvas;

        [Header("Arc Settings")]
        [SerializeField] private float _arcRadius  = 800f;
        [SerializeField] private float _totalAngle = 60f;

        [Header("Animation")]
        [SerializeField] private float _lerpSpeed = 8f;

        private readonly List<CardView> _playerCards = new List<CardView>();
        private readonly List<CardView> _enemyCards  = new List<CardView>();

        public void AddCardToPlayerHand(CardSO card)
        {
            CardView view = SpawnCard(_playerHandAnchor);
            view.Setup(card);
            _playerCards.Add(view);
        }

        public void AddCardToEnemyHand(CardSO card)
        {
            _enemyCards.Add(SpawnCard(_enemyHandAnchor));
        }

        private CardView SpawnCard(RectTransform anchor)
        {
            CardView view = Instantiate(_cardPrefab, anchor);
            // Inject drag canvas so the card can re-parent itself on drag
            view.GetComponent<CardDragHandler>()?.SetDragCanvas(_dragCanvas);
            return view;
        }

        public void RemoveCardFromPlayerHand(CardView card)
        {
            if (!_playerCards.Remove(card)) return;
            Destroy(card.gameObject);
        }

        private void Update()
        {
            LerpCardsToArc(_playerCards, mirrorRotation: false);
            LerpCardsToArc(_enemyCards,  mirrorRotation: true);
        }

        private void LerpCardsToArc(List<CardView> cards, bool mirrorRotation)
        {
            int count = cards.Count;
            if (count == 0) return;

            float dt = Time.deltaTime * _lerpSpeed;

            for (int i = 0; i < count; i++)
            {
                ComputeDestination(i, count, mirrorRotation,
                    out Vector2 targetPos, out float targetZ);

                RectTransform rt = cards[i].Rect;

                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos, dt);

                // Normalize current angle to -180..180 so lerp always takes the short path
                float currentZ = rt.localEulerAngles.z;
                if (currentZ > 180f) currentZ -= 360f;

                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(currentZ, targetZ, dt));
            }
        }

        // Computes the anchoredPosition and z-rotation for card [index] in a hand of [total].
        // The imaginary circle centre sits arcRadius below the anchor.
        // At θ=0 (centre card) position is (0, 0); side cards dip naturally along the arc.
        private void ComputeDestination(int index, int total, bool mirrorRotation,
            out Vector2 position, out float zRotation)
        {
            float angle = total == 1
                ? 0f
                : Mathf.Lerp(-_totalAngle * 0.5f, _totalAngle * 0.5f, (float)index / (total - 1));

            float rad = angle * Mathf.Deg2Rad;
            position = new Vector2(
                Mathf.Sin(rad) * _arcRadius,
                _arcRadius * (Mathf.Cos(rad) - 1f)
            );

            // Cards tilt to follow the arc tangent; enemy hand mirrors the rotation
            zRotation = mirrorRotation ? angle : -angle;
        }
    }
}
