using UnityEngine;


using global::System.Collections;

public class Screen_shace : MonoBehaviour
{
    public static Screen_shace Instance;

    [Header("Shake Settings")]
    [Tooltip("How long the shake lasts (seconds)")]
    public float shakeDuration = 0.2f;

    [Tooltip("How strong the shake is")]
    public float shakeMagnitude = 0.15f;

    [Tooltip("How fast the shake dampens over time (1 = smooth fade out)")]
    public float dampingSpeed = 1.0f;

    private Vector3 initialPosition;
    private Coroutine shakeCoroutine;

    void Start()
    {
        Instance = this;
        initialPosition = transform.localPosition;
    }

    void Update()
    {

    }

   
    public void Shake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = initialPosition;
        }

        shakeCoroutine = StartCoroutine(DoShake(shakeDuration, shakeMagnitude));
    }

 
    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = initialPosition;
        }

        shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float damper = 1f - (elapsed / duration) * dampingSpeed;
            damper = Mathf.Clamp01(damper);

            float offsetX = Random.Range(-1f, 1f) * magnitude * damper;
            float offsetY = Random.Range(-1f, 1f) * magnitude * damper;

            transform.localPosition = initialPosition + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = initialPosition;
    }
}

public class uusing
{
}