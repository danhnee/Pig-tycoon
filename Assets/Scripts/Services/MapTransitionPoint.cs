using UnityEngine;
using PigTycoon.Presentation;

namespace PigTycoon.Presentation.Services
{
    /// <summary>Keyboard-operated gateway between additive map scenes.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class MapTransitionPoint : MonoBehaviour
    {
        [SerializeField] private string destinationScene;
        [SerializeField] private string destinationSpawnId;
        private bool requested;

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerInteractionController>() != null)
                PlayerInteractionController.Instance?.ShowFeedback("Nhấn E để di chuyển đến khu vực khác.");
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (requested || !Input.GetKeyDown(KeyCode.E) ||
                other.GetComponentInParent<PlayerInteractionController>() == null)
                return;

            MapTravelService service = MapTravelService.Instance;
            if (service == null)
            {
                PlayerInteractionController.Instance?.ShowFeedback("Dịch vụ chuyển map chưa sẵn sàng.");
                return;
            }

            requested = true;
            service.Travel(destinationScene, destinationSpawnId, result =>
            {
                if (!result)
                    requested = false;
            });
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerInteractionController>() != null)
                requested = false;
        }
    }
}
