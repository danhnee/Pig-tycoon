using UnityEngine;

namespace PigTycoon.Presentation.Services
{
    /// <summary>Named arrival point used by additive map transitions.</summary>
    public sealed class MapSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId;

        public string SpawnId => spawnId;
    }
}
