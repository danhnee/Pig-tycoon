using UnityEngine;
using UnityEngine.EventSystems;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Xử lý sự kiện kéo thả (Drag & Drop) và Click cho từng ô trong Túi đồ Balo
    /// </summary>
    public class BackpackSlotDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public int SlotIndex;
        public BackpackPopup Popup;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Popup != null)
            {
                Popup.SelectSlot(SlotIndex);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Popup != null)
            {
                Popup.OnBeginDragSlot(SlotIndex, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Popup != null)
            {
                Popup.OnDragSlot(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Popup != null)
            {
                Popup.OnEndDragSlot(SlotIndex, eventData);
            }
        }
    }
}
