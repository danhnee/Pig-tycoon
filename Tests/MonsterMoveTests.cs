using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>Đòn cơ bản né được / đòn đặc biệt chỉ phản kích (gốc 5%, cộng theo kỹ năng thuần); Chống Chịu &lt; 30% phạt Thể lực.</summary>
internal static class MonsterMoveTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static void Run()
    {
        // mỗi loại quái có ít nhất một đòn cơ bản; rút sức là thuộc tính của đòn, không theo hạng quái
        foreach (EnemyTypeId t in Enum.GetValues(typeof(EnemyTypeId)))
            Check(MonsterMovesets.For(t).Any(m => m.Kind == MonsterMoveKind.Basic), $"{t} cần đòn cơ bản");
        Check(MonsterMovesets.For(EnemyTypeId.BayNhay).Any(m => m.StaminaDrainRatio > 0f), "Bầy Nhảy (quái thường) có đòn rút sức");
        Check(MonsterMovesets.For(EnemyTypeId.DauLinhTrom).All(m => m.StaminaDrainRatio == 0f), "Đầu Lĩnh Trộm (tinh anh) không rút sức");

        // đặc biệt chỉ tung khi hết hồi chiêu
        double ready = 0;
        var m1 = MonsterMovesets.Pick(EnemyTypeId.KeBatHeo, 10, ref ready, 0.1);
        Check(m1.Kind == MonsterMoveKind.Special && ready > 10, "hết hồi chiêu + roll thấp → đặc biệt");
        var m2 = MonsterMovesets.Pick(EnemyTypeId.KeBatHeo, 11, ref ready, 0.1);
        Check(m2.Kind == MonsterMoveKind.Basic, "đang hồi chiêu → cơ bản");

        // phản kích: gốc 5%, bấm sẵn hoặc bấm loạn không được cộng, căn đúng nhịp mới cao
        Check(Math.Abs(CounterResolver.Chance(0.5f, false, 1, 0, false) - 0.05f) < 1e-4, "bấm trước khi quái lấy đà: chỉ 5%");
        Check(Math.Abs(CounterResolver.Chance(0.05f, true, 3, 0, false) - 0.05f) < 1e-4, "bấm loạn: chỉ 5%");
        float sloppy = CounterResolver.Chance(0.4f, true, 1, 0, false);
        float tight = CounterResolver.Chance(0.05f, true, 1, 0, false);
        Check(sloppy < tight && Math.Abs(tight - 0.60f) < 1e-4, "căn đúng khung đòn chạm: 5 + 45 + 10 = 60%");
        Check(CounterResolver.Chance(0.05f, true, 1, 3, true) <= CounterResolver.MaxChance, "trần 90%");

        // Người dùng: "bình 180 giảm 25% còn 135; 140/180 sau giảm là 105/135", rồi trở lại khi Chống Chịu hồi
        var tk = new CharacterData(PlayerId.An);
        tk.Stamina.CurrentStamina = 140f;
        float cap = tk.GetEffectiveMaxStamina();
        tk.CurrentGuard = tk.MaxGuard * 0.2f;
        tk.SyncLowGuardStamina();
        Check(Math.Abs(tk.Stamina.CurrentStamina - 140f * 0.75f) < 0.01f && Math.Abs(tk.GetEffectiveMaxStamina() - cap * 0.75f) < 0.01f, "140/180 → 105/135");
        tk.CurrentGuard = tk.MaxGuard;
        tk.SyncLowGuardStamina();
        Check(Math.Abs(tk.Stamina.CurrentStamina - 140f) < 0.01f, "Chống Chịu hồi trên 30% → trở lại 140/180");

        // Chống Chịu < 30%: tổng Thể lực −25%, tốc độ hồi còn 6/10 (người dùng chốt)
        var c = new CharacterData(PlayerId.An);
        c.CurrentGuard = c.MaxGuard;
        float full = c.GetEffectiveMaxStamina();
        c.CurrentGuard = c.MaxGuard * 0.29f;
        Check(Math.Abs(c.GetEffectiveMaxStamina() - full * 0.75f) < 0.01f, "Chống Chịu < 30%: Thể lực tối đa −25%");
        c.CurrentGuard = c.MaxGuard * 0.05f; // còn xa ngưỡng 30% sau 1 giây hồi Chống Chịu
        c.SyncLowGuardStamina();
        c.Stamina.CurrentStamina = 10f;
        c.TickCombatRegen(1f, false, 5f);
        Check(Math.Abs(c.Stamina.CurrentStamina - 16f) < 0.01f, "hồi 10/s × 0,6 = 6/s");
    }
}
