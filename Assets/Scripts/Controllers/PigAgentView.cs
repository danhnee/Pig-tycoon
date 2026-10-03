using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class PigAgentView : MonoBehaviour
    {
        public Pig PigModel { get; private set; }

        [Header("Movement & Wander")]
        public float WalkSpeed = 1.8f;
        public float PanicSpeed = 4.0f;
        public float WanderRadius = 15f;

        private Vector3 targetPosition;
        private float wanderTimer = 0f;
        private Camera mainCamera;

        public void Bind(Pig model)
        {
            PigModel = model;
            targetPosition = transform.position;
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (PigModel == null || !PigModel.IsAlive) return;

            // Mobile Optimization: LOD check khoảng cách đến Camera
            if (mainCamera != null)
            {
                float distSqr = (transform.position - mainCamera.transform.position).sqrMagnitude;
                if (distSqr > 1600f) // > 40m
                {
                    // Heo ở xa: chỉ cập nhật đơn giản, không raycast hay anim phức tạp
                    return;
                }
            }

            // Xử lý di chuyển theo trạng thái Tinh Thần
            if (PigModel.MoodState == MoodState.HoangLoan || PigModel.MoodState == MoodState.HoangSo)
            {
                // Chạy hoảng sợ
                HandlePanicMovement();
            }
            else
            {
                // Đi dạo bình thường quanh đàn
                HandlePeacefulWander();
            }
        }

        private void HandlePeacefulWander()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(3f, 7f);
                Vector2 randomCircle = Random.insideUnitCircle * WanderRadius;
                targetPosition = new Vector3(randomCircle.x, transform.position.y, randomCircle.y);
            }

            MoveTowards(targetPosition, WalkSpeed);
        }

        private void HandlePanicMovement()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(0.8f, 2f);
                Vector2 randomCircle = Random.insideUnitCircle * (WanderRadius * 1.5f);
                targetPosition = new Vector3(randomCircle.x, transform.position.y, randomCircle.y);
            }

            MoveTowards(targetPosition, PanicSpeed);
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            Vector3 dir = (target - transform.position);
            dir.y = 0;
            if (dir.magnitude > 0.3f)
            {
                transform.forward = Vector3.Slerp(transform.forward, dir.normalized, Time.deltaTime * 8f);
                transform.position += dir.normalized * (speed * Time.deltaTime);
            }
        }
    }
}
