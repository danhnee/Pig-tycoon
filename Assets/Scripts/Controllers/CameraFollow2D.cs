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
        [Tooltip("Kích thước tầm nhìn phóng to gần người chơi (5.2 chuẩn nhìn rõ nét nhân vật và thú nuôi)")]
        public float TargetOrthoSize = 5.2f;
        public float MinOrthoSize = 3.5f;
        public float MaxOrthoSize = 10.0f;
        public float ZoomSpeed = 3.5f;

        [Header("Boundary Clamping")]
        [Tooltip("Khóa camera trong ranh giới bản đồ thảo nguyên")]
        public bool ClampToFarmBounds = true;

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

            // 0. Hỗ trợ con lăn chuột (Mouse ScrollWheel) để phóng to / thu nhỏ góc nhìn tự do
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                TargetOrthoSize = Mathf.Clamp(TargetOrthoSize - scroll * 5.0f, MinOrthoSize, MaxOrthoSize);
            }

            // 1. Phóng to / Thu nhỏ mượt mà theo TargetOrthoSize
            if (cam != null && Mathf.Abs(cam.orthographicSize - TargetOrthoSize) > 0.01f)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, TargetOrthoSize, Time.deltaTime * ZoomSpeed);
            }

            // 2. Tọa độ mong muốn
            Vector3 desiredPos = new Vector3(Target.position.x + Offset.x, Target.position.y + Offset.y, transform.position.z);

            // 3. Giới hạn camera trong ranh giới thảo nguyên (MapBounds)
            if (ClampToFarmBounds && FarmEnvironment2D.Instance != null && cam != null)
            {
                Rect bounds = FarmEnvironment2D.Instance.MapBounds;
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

            // 4. Di chuyển mượt mà bám theo nhân vật
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
