using PigTycoon.Core;

namespace PigTycoon.PlayableDemo3D
{
    public static class PlayableNpcConversation
    {
        public const string ManCareTopic = "demo.topic.man.care";

        public static FarmActionResult Talk(PlayableFarmSession session, FarmInteractionPoint point, string npcId)
        {
            if (point == null || point.Kind != FarmInteractionKind.Dialogue || !session.CanInteract(point))
                return new FarmActionResult(false, "Đến gần người đối thoại.");
            if (npcId != "Man" || !session.Engine.Story.NpcTrustTier.ContainsKey(npcId))
                return new FarmActionResult(false, "Không có cư dân phù hợp tại điểm này.");
            session.Engine.ConversationTopics.StartTopic(ManCareTopic, 1);
            session.Engine.PlayerStats.Increment("DemoConversations", 1u);
            session.State.Actions.Add(new FarmActionEvidence { Action = "talk", TargetId = npcId,
                Day = session.Engine.Clock.CurrentDay, Hour = session.Engine.Clock.CurrentHour, Minute = session.Engine.Clock.CurrentMinute });
            string address = session.Engine.Character.Identity == PlayerId.Khoa ? "anh" : "chị";
            // Demo guidance adapted from S Q01/P0405 and the free-talk/paid-job separation S P0349.
            // AskVillagerAboutTopic's generic Man branch exposes later lore; do not use it here.
            return new FarmActionResult(true, $"Mận: {address} rải cám, thay nước, chặn đoạn rào hỏng bằng rào tạm rồi dẫn heo lạc về vùng máng nhé.");
        }

        public static string EmploymentStatus(GameEngine engine, string npcId) => HasActiveFarmContract(engine, npcId)
            ? "Đang có hợp đồng nông công" : "Chưa tuyển vào trại";

        public static bool HasActiveFarmContract(GameEngine engine, string npcId) => npcId == "Man" &&
            engine.PetsAndNpcs.HiredNpcs.Exists(n => n.Id == NpcSpecialistId.NongCong && n.IsActive);
    }
}
