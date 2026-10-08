using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Rào chắn vô hình tại cổng chuồng chăn thả (Pasture Gate):
    /// Cho phép người chơi (An trên Farm_Main) đi qua tự do để khám phá thảo nguyên,
    /// nhưng chặn cứng đàn Heo không cho chạy thoát ra ngoài.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PigGateBarrier : MonoBehaviour
    {
        private Collider2D gateCollider;

        private void Awake()
        {
            gateCollider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            IgnorePlayerCollision();
        }

        private void OnEnable()
        {
            IgnorePlayerCollision();
        }

        public void IgnorePlayerCollision()
        {
            if (gateCollider == null) gateCollider = GetComponent<Collider2D>();

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var playerCols = player.GetComponentsInChildren<Collider2D>();
                foreach (var pCol in playerCols)
                {
                    if (pCol != null && gateCollider != null)
                    {
                        Physics2D.IgnoreCollision(pCol, gateCollider, true);
                    }
                }
            }
            else
            {
                var pCtrl = FindAnyObjectByType<PlayerMobileController>();
                if (pCtrl != null)
                {
                    var pCol = pCtrl.GetComponent<Collider2D>();
                    if (pCol != null && gateCollider != null)
                    {
                        Physics2D.IgnoreCollision(pCol, gateCollider, true);
                    }
                }
            }
        }
    }
}
