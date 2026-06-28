using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class UILightningEffect : MonoBehaviour
{
    [Header("Lightning Settings")]
    [Tooltip("Time range between lightning strikes (Seconds)")]
    [SerializeField] private Vector2 timeBetweenStrikes = new Vector2(5f, 15f);
    
    [Header("Animation Timings")]
    [SerializeField] private float fillDuration = 0.1f;
    [SerializeField] private float holdDuration = 0.2f;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Audio Integration")]
    [Tooltip("Hook your MMF_Player here to play the Thunder SFX via FEEL")]
    public UnityEvent OnLightningStrike;

    private Image _lightningImage;
    private Sequence _lightningSequence;

    private void Awake()
    {
        _lightningImage = GetComponent<Image>();
        ResetVisuals();
    }

    private void OnEnable()
    {
        QueueNextStrike();
    }

    private void OnDisable()
    {
        // Kill sequence to prevent it from running in the background if the menu closes
        _lightningSequence?.Kill();
    }

    private void ResetVisuals()
    {
        // Ensure perfect starting state
        _lightningImage.fillAmount = 0f;
        
        Color c = _lightningImage.color;
        c.a = 1f;
        _lightningImage.color = c;
    }

    private void QueueNextStrike()
    {
        float waitTime = Random.Range(timeBetweenStrikes.x, timeBetweenStrikes.y);

        // We use a DOTween Sequence to orchestrate the exact steps you requested
        _lightningSequence = DOTween.Sequence();

        _lightningSequence.AppendInterval(waitTime)
            .AppendCallback(() => 
            {
                // 1. Trigger the FEEL SFX right as the strike begins
                OnLightningStrike?.Invoke();
            })
            // 2. Fill from 0 to 100% (1.0f)
            .Append(_lightningImage.DOFillAmount(1f, fillDuration).SetEase(Ease.Flash))
            // 3. Hold on 100% for a short while
            .AppendInterval(holdDuration)
            // 4. Fade out Alpha to 0
            .Append(_lightningImage.DOFade(0f, fadeDuration))
            // 5. Reset Fill to 0 and Alpha back to 1 for the next cycle
            .AppendCallback(ResetVisuals)
            // 6. Loop by calling the queue method again
            .OnComplete(QueueNextStrike);
    }
}