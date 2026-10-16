using UnityEngine;

namespace PigTycoon.Presentation
{
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce);
        bool IsAlive { get; }
        Vector2 Position { get; }
    }
}
