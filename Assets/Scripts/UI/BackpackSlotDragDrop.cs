using UnityEngine;
using UnityEngine.EventSystems;

namespace PigTycoon.Presentation
{
    /// <summary>Compatibility binding for old scenes; carried objects are selected, never grid-swapped.</summary>
    public class BackpackSlotDragDrop : MonoBehaviour, IPointerClickHandler
    {
        public int SlotIndex;
        public BackpackPopup Popup;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && Popup != null)
                Popup.SelectSlot(SlotIndex);
        }
    }
}
