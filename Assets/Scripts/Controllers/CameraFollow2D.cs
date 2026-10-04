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
        [Tooltip("Kích thước tầm nhìn phóng to gần người chơi (6.0 - 6.5 chuẩn Stardew Valley)")]
        public float TargetOrthoSize = 6.5f;
        public float ZoomSpeed = 3.0f;

        [Header("Boundary Clamping")]
        [Tooltip("Khóa camera không bị lọt ra ngoài hàng rào trang trại")]
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
                // Đặt camera ngay lập tức vào người chơi khi bắt đầu
                Vector3 startPos = Target.position + (Vector3)Offset;
                startPos.z = transform.position.z;
                transform.position = startPos;
            }
        }

        private void LateUpdate()
        {
            FindTargetIfNull();
            if (Target == null) return;

            // 1. Phóng to / Thu nhỏ mượt mà theo TargetOrthoSize
            if (cam != null && Mathf.Abs(cam.orthographicSize - TargetOrthoSize) > 0.01f)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, TargetOrthoSize, Time.deltaTime * ZoomSpeed);
            }

            // 2. Tọa độ mong muốn
            Vector3 desiredPos = new Vector3(Target.position.x + Offset.x, Target.position.y + Offset.y, transform.position.z);

            // 3. Giới hạn camera trong khuôn viên trang trại (Không thấy khoảng trống đen ngoài rào)
            if (ClampToFarmBounds && FarmEnvironment2D.Instance != null && cam != null)
            {
                Rect bounds = FarmEnvironment2D.Instance.FarmBounds;
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
