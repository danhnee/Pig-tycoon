using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>Switches between the house exterior and its room layout using the existing action button.</summary>
    public sealed class KhoaHouseDoor2DView : MonoBehaviour
    {
        private Transform houseRoot;
        private GameObject interior;
        private GameObject exterior;
        private KhoaHouseDoor2DView stateOwner;
        private Vector3 interiorSpawnLocal;
        private Vector3 exteriorSpawnLocal;
        private bool isInside;
        private bool cameraClampWasSaved;
        private bool cameraClampBeforeEntering;
        private bool cameraZoomWasSaved;
        private float cameraZoomBeforeEntering;

        public bool IsInside => stateOwner != null ? stateOwner.isInside : isInside;
        public Transform HouseRoot => stateOwner != null && stateOwner.houseRoot != null ? stateOwner.houseRoot : houseRoot;

        public void Initialize(Transform root, GameObject interiorRoot, GameObject exteriorRoot)
        {
            houseRoot = root;
            interior = interiorRoot;
            exterior = exteriorRoot;
            stateOwner = this;
            // Central living room spawn stays 1.5+ world units from the exit threshold.
            interiorSpawnLocal = new Vector3(5f, -1.6f, 0f);
            exteriorSpawnLocal = new Vector3(2.0f, -2.2f, 0f);
        }

        public void InitializeInteriorExit(KhoaHouseDoor2DView entryDoor)
        {
            stateOwner = entryDoor;
        }

        public void Interact(Transform player)
        {
            var owner = stateOwner;
            if (player == null || owner == null || owner.houseRoot == null || owner.interior == null || owner.exterior == null)
                return;

            var cameraFollow = FindAnyObjectByType<CameraFollow2D>();
            bool entering = !owner.isInside;
            if (entering && cameraFollow != null)
            {
                owner.cameraClampBeforeEntering = cameraFollow.ClampToFarmBounds;
                owner.cameraClampWasSaved = true;
                owner.cameraZoomBeforeEntering = cameraFollow.TargetOrthoSize;
                owner.cameraZoomWasSaved = true;
            }

            owner.isInside = entering;
            owner.interior.SetActive(owner.isInside);
            owner.exterior.SetActive(!owner.isInside);

            // Enter the isolated interior map or return to the exterior threshold.
            player.position = owner.isInside
                ? owner.interior.transform.TransformPoint(owner.interiorSpawnLocal)
                : (owner.exterior != null ? owner.exterior.transform.TransformPoint(owner.exteriorSpawnLocal) : owner.houseRoot.TransformPoint(owner.exteriorSpawnLocal));

            if (cameraFollow != null)
            {
                cameraFollow.Target = player;
                cameraFollow.ClampToFarmBounds = owner.isInside
                    ? false
                    : owner.cameraClampWasSaved && owner.cameraClampBeforeEntering;
                if (owner.isInside)
                    cameraFollow.TargetOrthoSize = Mathf.Min(cameraFollow.MaxOrthoSize, 10f);
                else if (owner.cameraZoomWasSaved)
                    cameraFollow.TargetOrthoSize = owner.cameraZoomBeforeEntering;
            }

            Camera activeCamera = cameraFollow != null ? cameraFollow.GetComponent<Camera>() : Camera.main;
            if (activeCamera != null)
            {
                Vector2 offset = cameraFollow != null ? cameraFollow.Offset : Vector2.zero;
                activeCamera.transform.position = new Vector3(player.position.x + offset.x, player.position.y + offset.y, activeCamera.transform.position.z);
            }

            if (!owner.isInside)
            {
                owner.cameraClampWasSaved = false;
                owner.cameraZoomWasSaved = false;
            }

            var dayNight = DayNight2DController.Instance != null ? DayNight2DController.Instance : FindAnyObjectByType<DayNight2DController>();
            if (dayNight != null)
            {
                dayNight.ForceUpdateAmbient(true);
            }
        }
    }
}
