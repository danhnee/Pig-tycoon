using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>Selects one of the two existing actors before their gameplay components awaken.</summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class PixelWorldPlayerRoster : MonoBehaviour
    {
        public PlayerMobileController An;
        public PlayerMobileController Khoa;
        public MobileJoystick Joystick;

        void Awake()
        {
            bool useKhoa = MobileGameController.Instance?.Engine?.Character.Identity == PlayerId.Khoa;
            PlayerMobileController selected = useKhoa ? Khoa : An;
            PlayerMobileController other = useKhoa ? An : Khoa;
            if (selected == null || Joystick == null)
            {
                Debug.LogError("The source player rig is missing an actor or its mobile controls.");
                return;
            }
            if (other != null) other.gameObject.SetActive(false);
            selected.Joystick = Joystick;
            // The source farm actor owns the serialized context-button bindings.
            var sourceInput = An != null ? An.GetComponent<PlayerInteractionController>() : null;
            var selectedInput = selected.GetComponent<PlayerInteractionController>();
            if (sourceInput != null && selectedInput != null && sourceInput != selectedInput)
            {
                selectedInput.ActionButtonRoot = sourceInput.ActionButtonRoot;
                selectedInput.ActionButton = sourceInput.ActionButton;
                selectedInput.ActionButtonText = sourceInput.ActionButtonText;
                selectedInput.FeedbackFloatingText = sourceInput.FeedbackFloatingText;
            }
            foreach (var hud in FindObjectsByType<StardewHUDController>(FindObjectsSortMode.None))
                hud.PlayerController = selected;
            foreach (var camera in FindObjectsByType<CameraFollow2D>(FindObjectsSortMode.None))
                camera.Target = selected.transform;
            selected.gameObject.SetActive(true);
        }

        /// <summary>
        /// Start-screen choice made after this rig awoke (HANDBOOK P1.02): both actors share one controller, so the
        /// active actor keeps its bindings and only swaps to the chosen character's sprite library (the other actor's,
        /// which stays inactive). Later scene loads pick the matching actor in <see cref="Awake"/>.
        /// </summary>
        public void ApplyChosenIdentity(PlayerId id)
        {
            PlayerMobileController chosen = id == PlayerId.Khoa ? Khoa : An;
            PlayerMobileController active = An != null && An.gameObject.activeInHierarchy ? An
                : Khoa != null && Khoa.gameObject.activeInHierarchy ? Khoa : null;
            if (chosen == null || active == null || chosen == active) return;
            var source = chosen.GetComponent<CharacterSpriteAnimator>();
            var target = active.GetComponent<CharacterSpriteAnimator>();
            if (source != null && target != null && source.MythLibrary != null)
                target.SetMythLibrary(source.MythLibrary);
            else
                Debug.LogWarning($"[Roster] cannot swap the player sprite library to {id}: missing animator or library.");
        }
    }
}
