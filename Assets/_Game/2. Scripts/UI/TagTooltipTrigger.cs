// Assets/_Game/2. Scripts/UI/TagTooltipTrigger.cs
using UnityEngine;
using UnityEngine.EventSystems;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    // Attach to each tag icon Image in the tags container.
    // Shows tag name and description on pointer enter.
    public class TagTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [HideInInspector] public CardTagSO Tag;

        [SerializeField] private TooltipController _tooltip;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Tag == null || _tooltip == null) return;
            // TODO: extend TooltipController to accept tag name + description
            // For now logs to confirm wiring is correct
            Debug.Log($"Tag: {Tag.TagName} — {Tag.Description}");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _tooltip?.Hide();
        }
    }
}