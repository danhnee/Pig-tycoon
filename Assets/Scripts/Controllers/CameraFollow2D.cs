using UnityEngine;
using UnityEngine.Rendering.Universal;

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
        public bool UseWorldBounds;
        public Rect WorldBounds;

        [Header("Pixel framing (with PixelPerfectCamera)")]
        [Tooltip("Show at least this many metres above and below the player. The integer pixel scale is the largest " +
                 "that still shows this much: 1080p → ×2 (8.44 m, about Stardew's framing), 1440p → ×3 (7.5 m), 2160p → ×4.")]
        public float MinHalfHeightM = 7.0f;
        public bool AutoPixelFraming = true;
        private PixelPerfectCamera ppc;
        private int framedW, framedH;

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
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            FindTargetIfNull();
            if (Target != null)
            {
                Vector3 startPos = Target.position + (Vector3)Offset;
                startPos.z = transform.position.z;
                transform.position = ClampPosition(startPos);
            }
            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null) cam.orthographicSize = TargetOrthoSize;
        }

        /// <summary>
        /// Wider view with crisp pixels: pick the integer pixel scale from the real screen height, then hand the
        /// PixelPerfectCamera a reference resolution equal to the screen divided by that scale (whole pixels, no
        /// fractional stretching, which made the old 5.2 m / 640×360 view look rough).
        /// </summary>
        private void ApplyPixelFraming()
        {
            if (!AutoPixelFraming || cam == null) return;
            if (ppc == null) ppc = GetComponent<PixelPerfectCamera>();
            if (ppc == null) return;
            int w = cam.pixelWidth, h = cam.pixelHeight;
            if (w == framedW && h == framedH) return;
            framedW = w; framedH = h;
            int ppu = Mathf.Max(1, ppc.assetsPPU);
            int zoom = Mathf.Max(1, Mathf.FloorToInt(h / (2f * ppu * MinHalfHeightM)));
            ppc.refResolutionX = Mathf.Max(2, (w / zoom) & ~1);
            ppc.refResolutionY = Mathf.Max(2, (h / zoom) & ~1);
            TargetOrthoSize = MinOrthoSize = MaxOrthoSize = ppc.refResolutionY / (2f * ppu);
        }

        private void LateUpdate()
        {
            FindTargetIfNull();
            ApplyPixelFraming();
            if (Target == null) return;

            // 0. Mouse wheel zoom only without a PixelPerfectCamera (it would fight the whole-pixel framing)
            float scroll = ppc == null ? Input.GetAxis("Mouse ScrollWheel") : 0f;
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

            desiredPos = ClampPosition(desiredPos);

            // 4. Di chuyển mượt mà bám theo nhân vật
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * SmoothSpeed);
        }

        private Vector3 ClampPosition(Vector3 desiredPos)
        {
            if (ClampToFarmBounds && cam != null && (UseWorldBounds || FarmEnvironment2D.Instance != null))
            {
                Rect bounds = UseWorldBounds ? WorldBounds : FarmEnvironment2D.Instance.MapBounds;
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

            return desiredPos;
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
