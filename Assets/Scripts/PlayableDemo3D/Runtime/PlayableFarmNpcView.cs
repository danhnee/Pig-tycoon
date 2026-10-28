using UnityEngine;

namespace PigTycoon.PlayableDemo3D
{
    /// <summary>Attach to the authored Mận/black-umbrella model. A resident is not a free employee.</summary>
    [DefaultExecutionOrder(-300)]
    public sealed class PlayableFarmNpcView : MonoBehaviour
    {
        public string NpcId = "Man";
        public PlayableFarmController Farm;
        public PlayableFarmStation Interaction;
        public Animator Animator;
        public string TalkTrigger = "Talk";
        public string EmploymentStatus => Farm != null && Farm.Session != null
            ? PlayableNpcConversation.EmploymentStatus(Farm.Session.Engine, NpcId) : "Chưa tuyển vào trại";

        private void Awake()
        {
            if (Interaction == null) Interaction = GetComponent<PlayableFarmStation>();
            if (Interaction == null) Interaction = gameObject.AddComponent<PlayableFarmStation>();
            Interaction.Kind = FarmInteractionKind.Dialogue;
            Interaction.StationId = "npc." + NpcId.ToLowerInvariant();
            Interaction.DisplayName = NpcId == "Man" ? "Mận" : NpcId;
            if (Animator == null) Animator = GetComponentInChildren<Animator>();
        }
        private void Start()
        {
            if (Farm == null) Farm = FindAnyObjectByType<PlayableFarmController>();
            if (Farm != null) Farm.RegisterStation(Interaction);
        }
        public FarmActionResult Talk(PlayableFarmSession session)
        {
            if (Farm != null && Farm.IsPaused) return new FarmActionResult(false, "Đang tạm nghỉ.");
            var result = PlayableNpcConversation.Talk(session, Interaction.Point, NpcId);
            if (!result.Success) return result;
            Vector3 facing = new Vector3(session.PlayerX - transform.position.x, 0f, session.PlayerY - transform.position.z);
            if (facing.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(facing);
            if (Animator != null && Animator.runtimeAnimatorController != null)
                foreach (var parameter in Animator.parameters)
                    if (parameter.name == TalkTrigger && parameter.type == AnimatorControllerParameterType.Trigger)
                    { Animator.SetTrigger(TalkTrigger); break; }
            return result;
        }
    }
}
