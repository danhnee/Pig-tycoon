using System;
using UnityEngine;

namespace PigTycoon.Presentation
{
    public class CorpseLot2DView : MonoBehaviour
    {
        [Header("GDD v6.0 Specs: 4 Ô Tập Kết Xác")]
        public const int MaxCorpseSlots = 4;

        [SerializeField]
        private Transform[] slotTransforms = new Transform[MaxCorpseSlots];

        private readonly string[] occupiedPigIds = new string[MaxCorpseSlots];

        public int OccupiedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MaxCorpseSlots; i++)
                {
                    if (!string.IsNullOrEmpty(occupiedPigIds[i])) count++;
                }
                return count;
            }
        }

        public bool HasAvailableSlot => OccupiedCount < MaxCorpseSlots;

        private void Awake()
        {
            // Tự động tìm hoặc sinh 4 ô nếu chưa gán
            if (slotTransforms == null || slotTransforms.Length != MaxCorpseSlots || slotTransforms[0] == null)
            {
                slotTransforms = new Transform[MaxCorpseSlots];
                for (int i = 0; i < MaxCorpseSlots; i++)
                {
                    string childName = $"Slot_{i + 1}";
                    Transform child = transform.Find(childName);
                    if (child == null)
                    {
                        var newChild = new GameObject(childName);
                        newChild.transform.SetParent(transform);
                        float offsetX = (i % 2) * 1.5f - 0.75f;
                        float offsetY = (i / 2) * 1.5f - 0.75f;
                        newChild.transform.localPosition = new Vector3(offsetX, offsetY, 0);
                        child = newChild.transform;
                    }
                    slotTransforms[i] = child;
                }
            }
        }

        public bool TryAssignSlot(string pigId, out Vector2 assignedWorldPos, out int slotIndex)
        {
            for (int i = 0; i < MaxCorpseSlots; i++)
            {
                if (string.IsNullOrEmpty(occupiedPigIds[i]))
                {
                    occupiedPigIds[i] = pigId;
                    slotIndex = i;
                    assignedWorldPos = slotTransforms[i] != null ? (Vector2)slotTransforms[i].position : (Vector2)transform.position;
                    return true;
                }
            }

            assignedWorldPos = Vector2.zero;
            slotIndex = -1;
            return false;
        }

        public void ReleaseSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < MaxCorpseSlots)
            {
                occupiedPigIds[slotIndex] = null;
            }
        }

        public Vector2 GetSlotPosition(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < MaxCorpseSlots && slotTransforms[slotIndex] != null)
            {
                return slotTransforms[slotIndex].position;
            }
            return transform.position;
        }
    }
}
