using UnityEngine;

namespace PigTycoon
{
    /// <summary>
    /// Attached to the Edit-Mode design showcase canvas in PixelMyth_25D_Design_Master.unity.
    /// Keeps the UI visible in Edit Mode (Scene View / Game View / Inspector for lecturer demonstration)
    /// and hides the static preview copy in Play Mode when the live interactive MythHudController spawns.
    /// </summary>
    public sealed class EditModeDesignCanvasAutoHider : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.isPlaying)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
