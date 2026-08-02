// Assets/_Game/2. Scripts/Systems/VFX/FloatingCombatText.cs
using System.Collections;
using TMPro;
using UnityEngine;

namespace ThroneOfTides.Systems.VFX
{
    // A short-lived world-space number that rises and fades out, then destroys itself.
    // Used for damage (red), heal (green), and mana gain (light blue) popups.
    public class FloatingCombatText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _label;
        [SerializeField] private float       _riseDistance = 1.2f;
        [SerializeField] private float       _duration     = 1f;

        public void Setup(string text, Color color)
        {
            _label.text  = text;
            _label.color = color;
            StartCoroutine(AnimateAndDestroy());
        }

        private IEnumerator AnimateAndDestroy()
        {
            Vector3 start = transform.position;
            Vector3 end   = start + Vector3.up * _riseDistance;
            Color   color = _label.color;
            float   elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _duration;
                transform.position = Vector3.Lerp(start, end, t);
                _label.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t));
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
