using UnityEngine;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Target & Movement")]
        public Transform Target;
        public float SmoothSpeed = 6.0f;
        public Vector2 Offset = Vector2.zero;

        [Header("Stardew Cozy Zoom (Ortho Size)")]
        [Tooltip("KÃ­ch thÆ°á»›c táº§m nhÃ¬n phÃ³ng to gáº§n ngÆ°á»i chÆ¡i (5.2 chuáº©n nhÃ¬n rÃµ nÃ©t nhÃ¢n váº­t vÃ  thÃº nuÃ´i)")]
        public float TargetOrthoSize = 5.2f;
        public float MinOrthoSize = 3.5f;
        public float MaxOrthoSize = 10.0f;
        public float ZoomSpeed = 3.5f;

        [Header("Boundary Clamping")]
        [Tooltip("KhÃ³a camera trong ranh giá»›i báº£n Ä‘á»“ tháº£o nguyÃªn")]
        public bool ClampToFarmBounds = true;
        public Rect? CustomBounds = null;

        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = TargetOrthoSize;
            }
        }

        private void Start()
        {
            FindTargetIfNull();
            if (Target != null)
            {
                Vector3 startPos = Target.position + (Vector3)Offset;
                startPos.z = transform.position.z;
                transform.position = startPos;
            }
        }

        private void LateUpdate()
        {
            FindTargetIfNull();
            if (Target == null) return;

            // 0. Há»— trá»£ con lÄƒn chuá»™t (Mouse ScrollWheel) Ä‘á»ƒ phÃ³ng to / thu nhá» gÃ³c nhÃ¬n tá»± do
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                TargetOrthoSize = Mathf.Clamp(TargetOrthoSize - scroll * 5.0f, MinOrthoSize, MaxOrthoSize);
            }

            // 1. PhÃ³ng to / Thu nhá» mÆ°á»£t mÃ  theo TargetOrthoSize
            if (cam != null && Mathf.Abs(cam.orthographicSize - TargetOrthoSize) > 0.01f)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, TargetOrthoSize, Time.deltaTime * ZoomSpeed);
            }

            // 2. Tá»a Ä‘á»™ mong muá»‘n
            Vector3 desiredPos = new Vector3(Target.position.x + Offset.x, Target.position.y + Offset.y, transform.position.z);

            // 3. Giá»›i háº¡n camera trong ranh giá»›i tháº£o nguyÃªn (MapBounds)
            if (ClampToFarmBounds && cam != null)
            {
                Rect bounds = CustomBounds.HasValue
                    ? CustomBounds.Value
                    : (FarmEnvironment2D.Instance != null ? (Rect)FarmEnvironment2D.Instance.MapBounds : Rect.zero);
                if (bounds.width <= 0.1f) return;
                float vertExtent = cam.orthographicSize;
                float horzExtent = vertExtent * cam.aspect;

                float minX = bounds.xMin + horzExtent;
                float maxX = bounds.xMax - horzExtent;
                float minY = bounds.yMin + vertExtent;
                float maxY = bounds.yMax - vertExtent;

                if (minX < maxX)
                {
                    desiredPos.x = Mathf.Clamp(desiredPos.x, minX, maxX);
                }
                else
                {
                    desiredPos.x = bounds.center.x;
                }

                if (minY < maxY)
                {
                    desiredPos.y = Mathf.Clamp(desiredPos.y, minY, maxY);
                }
                else
                {
                    desiredPos.y = bounds.center.y;
                }
            }

            // 4. Di chuyá»ƒn mÆ°á»£t mÃ  bÃ¡m theo nhÃ¢n váº­t
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * SmoothSpeed);
        }

        private void FindTargetIfNull()
        {
            if (Target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    Target = player.transform;
                }
                else
                {
                    var playerCtrl = FindAnyObjectByType<PlayerMobileController>();
                    if (playerCtrl != null)
                    {
                        Target = playerCtrl.transform;
                    }
                }
            }
        }
    }
}
