using UnityEngine;
using UnityEngine.EventSystems;

namespace PigTycoon.Presentation
{
    public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Joystick Settings")]
        public RectTransform BackgroundRect;
        public RectTransform HandleRect;
        public float HandleRange = 100f;
        public float DeadZone = 0.05f;

        public Vector2 InputVector { get; private set; } = Vector2.zero;
        public float Horizontal => InputVector.x;
        public float Vertical => InputVector.y;

        private Vector2 joystickPosition = Vector2.zero;

        private void Start()
        {
            if (BackgroundRect == null) BackgroundRect = GetComponent<RectTransform>();
            if (HandleRect != null) joystickPosition = HandleRect.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (BackgroundRect == null || HandleRect == null) return;

            Vector2 position = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, BackgroundRect.position);
            Vector2 radius = BackgroundRect.sizeDelta / 2f;
            Vector2 direction = (eventData.position - position) / (radius * HandleRange / 100f);

            InputVector = direction.magnitude > 1f ? direction.normalized : direction;

            if (InputVector.magnitude < DeadZone)
            {
                InputVector = Vector2.zero;
            }

            HandleRect.anchoredPosition = (InputVector * radius * (HandleRange / 100f));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            InputVector = Vector2.zero;
            if (HandleRect != null)
            {
                HandleRect.anchoredPosition = joystickPosition;
            }
        }
    }
}
