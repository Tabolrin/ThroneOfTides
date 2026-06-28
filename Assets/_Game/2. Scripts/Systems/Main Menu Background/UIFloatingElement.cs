using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class UIFloatingElement : MonoBehaviour
{
    [Header("Position Amplitudes (Min / Max)")]
    [Tooltip("X = Minimum Offset, Y = Maximum Offset")]
    // [MinMaxSlider(-50f, 50f)] // Uncomment if using NaughtyAttributes/Odin
    [SerializeField] private Vector2 xOffsetRange = new Vector2(-20f, 20f);
    
    // [MinMaxSlider(-50f, 50f)] // Uncomment if using NaughtyAttributes/Odin
    [SerializeField] private Vector2 yOffsetRange = new Vector2(-15f, 15f);

    [Header("Animation Durations (Min / Max)")]
    [Tooltip("X = Minimum Duration, Y = Maximum Duration")]
    // [MinMaxSlider(1f, 10f)] // Uncomment if using NaughtyAttributes/Odin
    [SerializeField] private Vector2 xDurationRange = new Vector2(3f, 5f);
    
    // [MinMaxSlider(1f, 10f)] // Uncomment if using NaughtyAttributes/Odin
    [SerializeField] private Vector2 yDurationRange = new Vector2(2f, 4f);

    [Header("Settings")]
    [SerializeField] private Ease easingType = Ease.InOutSine;

    private RectTransform _rectTransform;
    private Vector2 _startPosition;
    
    // We separate the tweens so they can run asynchronously 
    private Tween _xTween;
    private Tween _yTween;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _startPosition = _rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        // Fire off both loops independently
        AnimateX();
        AnimateY();
    }

    private void OnDisable()
    {
        // Always kill tweens when disabled to prevent memory leaks and UI ghosting
        _xTween?.Kill();
        _yTween?.Kill();
    }

    private void AnimateX()
    {
        // Pick a random destination on the X axis relative to the start point
        float targetX = _startPosition.x + Random.Range(xOffsetRange.x, xOffsetRange.y);
        
        // Pick a random time it takes to get there
        float duration = Random.Range(xDurationRange.x, xDurationRange.y);

        // Tween just the X axis, then loop recursively
        _xTween = _rectTransform.DOAnchorPosX(targetX, duration)
            .SetEase(easingType)
            .OnComplete(AnimateX);
    }

    private void AnimateY()
    {
        // Pick a random destination on the Y axis relative to the start point
        float targetY = _startPosition.y + Random.Range(yOffsetRange.x, yOffsetRange.y);
        
        // Pick a random time it takes to get there
        float duration = Random.Range(yDurationRange.x, yDurationRange.y);

        // Tween just the Y axis, then loop recursively
        _yTween = _rectTransform.DOAnchorPosY(targetY, duration)
            .SetEase(easingType)
            .OnComplete(AnimateY);
    }
}