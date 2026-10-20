using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum PigUtilityBehaviorState
    {
        IdleGrazing = 0,
        SeekingFeeder = 1,
        SeekingWaterOrCooling = 2,
        SeekingShelterRefuge = 3,
        FlockingWithAlphaLeader = 4,
        AvoidingBiohazardCorpse = 5,
        FleeingInPanic = 6,
        QuarantinedInIsolation = 7,
        JackfruitCorner0200Anomaly = 8
    }

    [Serializable]
    public sealed class PigAutonomousAgentDecision
    {
        public string PigId { get; set; } = string.Empty;
        public PigUtilityBehaviorState ChosenState { get; set; }
        public float UtilityScore { get; set; }
        public float OldX { get; set; }
        public float OldY { get; set; }
        public float NewX { get; set; }
        public float NewY { get; set; }
        public string CrossSystemInteractionNote { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class TacticalRaidEngagementSummary
    {
        public int WaveIndex { get; set; }
        public int InitialThreatBudget { get; set; }
        public int EffectiveThreatWithWealthAndCorpseScent { get; set; }
        public int AboveGroundWiresCutBySaboteurs { get; set; }
        public int UndergroundWiresProtectedCount { get; set; }
        public int UfoAbductionAttempts { get; set; }
        public int UfoBeamsSeveredBySkyhook { get; set; }
        public int DomesticatedXichMaoCounterAttackDamage { get; set; }
        public int TowerSalvoDamage { get; set; }
        public int AcousticShockDecibels { get; set; }
        public int ElderCalmAuraMitigation { get; set; }
        public int UnshelteredPregnantSowsMiscarried { get; set; }
        public bool PowerBreakerTrippedDuringPeak { get; set; }
        public bool Victory { get; set; }
        public MarketTheme PostBattleMarketShift { get; set; }
    }

    /// <summary>
    /// Động cơ Mô phỏng Hệ sinh thái &amp; Tương tác Đa Tác Tử Thời Gian Thực (AAA Systemic Ecosystem Simulation):
    /// Kết nối trực tiếp toàn bộ các hệ thống của GameEngine thành một vòng lặp nhân quả tự thân (Emergent Simulation Loop):
    /// 1. Utility AI cho từng cá thể heo (tự tìm máng ăn theo Flow-Field khi đói, tìm nguồn mát có điện khi Nắng Gắt,
    ///    bám theo Thủ Lĩnh Đàn trong bán kính Neo Tinh Thần, tránh xác chết ô nhiễm, và phản ứng dị tượng 02:00 ở góc gốc mít).
    /// 2. Lây nhiễm không gian &amp; ô nhiễm máng ăn/nguồn nước theo khoảng cách thực tế + tương tác Dòng Gen × Thời tiết × Lưới điện
    ///    (Lôi Mạch nạp tụ điện khi Giông Bão, Nấm Phát Quang mở sáng Minimap ban đêm &amp; thu hút Bí Nhân).
    /// 3. Mạng lưới Thiện cảm NPC Thị trấn Bờ Rìa (Tám Vá giảm hao bền điện, Mận tăng kế vị, Bác sĩ Dung tăng mổ xác, Lệ điều chỉnh phí kiểm dịch).
    /// 4. Giao tranh Đợt Quái Đa Chủng Loại (Kẻ Cắt Dây cắt dây nổi vs dây ngầm ×3, UFO chùm kéo vs Skyhook 3A, Chiến trư Xích Mao hộ vệ Thủ Lĩnh).
    /// 5. Đồng hồ Tâm lý Góc Nhìn Kép Khoa (lấy cương) vs An (lấy nhu) tại hai mốc 07:02 sáng và 02:00 đêm.
    /// </summary>
    [Serializable]
    public sealed class SystemicEcosystemSimulation
    {
        public int ContaminatedTroughsCount { get; set; }
        public float LoiMachStormCapacitorBonusUnits { get; set; }
        public float NamPhatQuangNightMinimapBonusMeters { get; set; }
        public float CurrentReflectiveSurfaceLagSeconds { get; set; }
        public int TotalEmergentInteractionsProcessed { get; set; }
        public List<PigAutonomousAgentDecision> LastHourlyAgentDecisions { get; private set; } = new List<PigAutonomousAgentDecision>();
        public TacticalRaidEngagementSummary? LastTacticalRaidSummary { get; private set; }

        /// <summary>
        /// Thực thi Utility AI và tương tác sinh thái không gian theo từng giờ cho toàn bộ đàn heo trên nông trại.
        /// </summary>
        public int TickHourlyEcologyAndAgentAi(GameEngine engine)
        {
            if (engine == null) return 0;

            LastHourlyAgentDecisions.Clear();
            int interactions = 0;

            bool hasShepherdDog = engine.PetsAndNpcs.HasPet(PetId.ChoChanDan);
            bool coolingPowered = !engine.DeepMechanics.PowerGrid.IsBreakerTripped &&
                                  (engine.Defense.HasPoweredBuilding("BomLamMat") || engine.Farm.Infrastructure.WaterTroughs > 0);
            bool floodlightPowered = !engine.DeepMechanics.PowerGrid.IsBreakerTripped &&
                                     engine.Defense.HasPoweredBuilding("DenPhaCongNghiep");

            // Kiểm tra heo Lôi Mạch nạp điện cảm ứng vào Tụ Điện trong Giông Bão & Nấm Phát Quang phát sáng ban đêm
            LoiMachStormCapacitorBonusUnits = 0f;
            NamPhatQuangNightMinimapBonusMeters = 0f;

            Pig? leaderPig = null;
            if (engine.Herd.Leader != null)
            {
                leaderPig = engine.Pigs.Find(p => p.IsAlive && p.Id == engine.Herd.Leader.PigId);
            }

            // Tìm tọa độ các xác heo chưa dọn để heo sống tự động tránh xa vùng ô nhiễm
            var uncollectedCorpses = engine.Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot);

            for (int i = 0; i < engine.Pigs.Count; i++)
            {
                var p = engine.Pigs[i];
                if (p == null || !p.IsAlive) continue;

                float oldX = p.PositionX;
                float oldY = p.PositionY;

                if (p.IsIsolated)
                {
                    LastHourlyAgentDecisions.Add(new PigAutonomousAgentDecision
                    {
                        PigId = p.Id,
                        ChosenState = PigUtilityBehaviorState.QuarantinedInIsolation,
                        UtilityScore = 100f,
                        OldX = oldX,
                        OldY = oldY,
                        NewX = p.PositionX,
                        NewY = p.PositionY,
                        CrossSystemInteractionNote = "Đang trong Khu Cách Ly: chặn 100% lây nhiễm chéo sang máng đàn."
                    });
                    continue;
                }

                // Tương tác Cột Thu Lôi (GDD 15: +20 điện tích tụ điện khi Giông Bão) & Dòng Gen Lôi Mạch × Giông Bão (GDD 5.2: tụt Tinh thần ×1.5 nếu ngoài nơi trú, ăn +10%, nguy cơ Lôi Tâm nếu thiếu Cột Thu Lôi 20m)
                if (p.GeneLine == GeneLineId.LoiMach && engine.CurrentWeather == WeatherType.GiongBao)
                {
                    bool hasLightningRod = engine.Defense.HasPoweredBuilding("CotThuLoi");
                    float chargeAdded = hasLightningRod ? 20.0f : 4.0f;
                    LoiMachStormCapacitorBonusUnits += chargeAdded;
                    engine.DeepMechanics.PowerGrid.CapacitorChargeUnits = Math.Min(
                        engine.DeepMechanics.PowerGrid.GetMaxCapacitorReserve(),
                        engine.DeepMechanics.PowerGrid.CapacitorChargeUnits + chargeAdded);

                    float dxRef = p.PositionX - engine.GridZones.HerdRefugeCenter.x;
                    float dyRef = p.PositionY - engine.GridZones.HerdRefugeCenter.y;
                    bool inRefuge = (dxRef * dxRef + dyRef * dyRef) <= 324f;
                    if (!inRefuge)
                    {
                        p.Mood = Math.Max(0, p.Mood - 2); // Giông Bão: Tinh thần giảm ×1.5 ngoài nơi trú (GDD 5.2)
                    }
                    interactions++;
                }

                // Tương tác Bệnh Nấm Phát Quang × Ban đêm / Sương Mù -> Tăng tầm nhìn Minimap & Áp lực Sự kiện
                if (p.ActiveDisease == DiseaseId.NamPhatQuang &&
                    (engine.Clock.CurrentHour >= 18 || engine.Clock.CurrentHour < 6 || engine.CurrentWeather == WeatherType.SuongMu))
                {
                    NamPhatQuangNightMinimapBonusMeters = Math.Max(NamPhatQuangNightMinimapBonusMeters, 8.0f);
                    interactions++;
                }

                // Đánh giá điểm Utility cho từng nhu cầu sinh học & xã hội
                PigUtilityBehaviorState bestState = PigUtilityBehaviorState.IdleGrazing;
                float bestUtility = 10f;
                string note = "Đi kiếm ăn tự nhiên trong đồng cỏ cho phép.";

                // 1. Dị tượng 02:00 sáng 'Sáu Với Một' tại góc gốc mít Tây Bắc (-18, 18)
                if (engine.Clock.CurrentHour == 2 && !engine.Story.SeventhChannelCut)
                {
                    if (p.Id == "pig_white_foot_2018")
                    {
                        p.PositionX = -18f;
                        p.PositionY = 18f;
                        bestState = PigUtilityBehaviorState.JackfruitCorner0200Anomaly;
                        bestUtility = 99f;
                        note = "Con heo chân trắng tự đi về góc gốc mít Tây Bắc (-18, 18) lúc 02:00 sáng.";
                        interactions++;
                    }
                    else
                    {
                        float dxAnom = p.PositionX - (-18f);
                        float dyAnom = p.PositionY - 18f;
                        if (dxAnom * dxAnom + dyAnom * dyAnom < 4.0f)
                        {
                            p.PositionX += 1.8f;
                            bestState = PigUtilityBehaviorState.JackfruitCorner0200Anomaly;
                            bestUtility = 95f;
                            note = "Đàn heo tự động dạt ra chừa vòng tròn trống 1.2m ở góc Tây Bắc (ảnh năm 2018).";
                            interactions++;
                        }
                    }
                }

                // 2. Tránh xác heo chết chưa thu dọn (bán kính 10m)
                for (int c = 0; c < uncollectedCorpses.Count; c++)
                {
                    var corpse = uncollectedCorpses[c];
                    float dxC = p.PositionX - corpse.PositionX;
                    float dyC = p.PositionY - corpse.PositionY;
                    float distSq = dxC * dxC + dyC * dyC;
                    if (distSq < 100f && bestUtility < 85f)
                    {
                        float stepX = dxC >= 0f ? 2.0f : -2.0f;
                        float stepY = dyC >= 0f ? 2.0f : -2.0f;
                        p.PositionX += stepX;
                        p.PositionY += stepY;
                        p.Comfort = Math.Max(0, p.Comfort - 2);
                        bestState = PigUtilityBehaviorState.AvoidingBiohazardCorpse;
                        bestUtility = 85f;
                        note = $"Tránh xa xác heo [{corpse.Id}] chưa dọn (-2 Comfort, nguy cơ ổ dịch).";
                        interactions++;
                        break;
                    }
                }

                // 3. Hoảng loạn -> Chạy tán loạn hoặc được Chó Chăn Đàn / Thủ Lĩnh neo giữ
                if (p.MoodState == MoodState.HoangLoan && bestUtility < 90f)
                {
                    if (leaderPig != null && !engine.Herd.Leader!.IsUnderAttack)
                    {
                        p.ApplyNeoTinhThan(engine.Herd.Leader.NeoThreshold);
                        bestState = PigUtilityBehaviorState.FlockingWithAlphaLeader;
                        bestUtility = 90f;
                        note = $"Được Thủ Lĩnh [{leaderPig.Name}] neo tinh thần ở ngưỡng {engine.Herd.Leader.NeoThreshold}.";
                    }
                    else
                    {
                        bestState = PigUtilityBehaviorState.FleeingInPanic;
                        bestUtility = 90f;
                        note = "Hoảng loạn: từ chối ăn máng đàn và có nguy cơ húc cổng.";
                    }
                    interactions++;
                }

                // 4. Đói (Hunger >= 30) -> Tự động đi theo Flow-Field về phía Máng Ăn và ăn trực tiếp từ kho cám
                if (p.Hunger >= 30 && p.MoodState != MoodState.HoangLoan && bestUtility < 80f)
                {
                    p.PositionX = MoveTowards(p.PositionX, 0f, 3.5f);
                    p.PositionY = MoveTowards(p.PositionY, 0f, 3.5f);
                    bestState = PigUtilityBehaviorState.SeekingFeeder;
                    bestUtility = 80f;

                    if (engine.Farm.FeedStockKg >= 0.5f && p.PositionX * p.PositionX + p.PositionY * p.PositionY <= 900f)
                    {
                        engine.Farm.FeedStockKg = Math.Max(0f, engine.Farm.FeedStockKg - 0.5f);
                        p.Hunger = Math.Max(0, p.Hunger - 25);
                        p.FedToday = true;

                        // Nếu heo đang mắc bệnh truyền nhiễm Nhóm A ăn tại máng chung -> làm nhiễm bẩn máng ăn!
                        if (p.ActiveDisease.HasValue && DiseaseBoard.GroupOf(p.ActiveDisease.Value) == DiseaseGroup.A)
                        {
                            ContaminatedTroughsCount = Math.Min(10, ContaminatedTroughsCount + 1);
                            note = $"Heo bệnh [{p.ActiveDisease.Value}] ăn tại máng chung làm nhiễm bẩn máng (ContaminatedTroughs={ContaminatedTroughsCount})!";
                        }
                        else if (ContaminatedTroughsCount > 0 && !p.ActiveDisease.HasValue && p.ImmuneDays <= 0)
                        {
                            note = "Ăn tại máng đang bị nhiễm khuẩn từ heo bệnh trước đó!";
                        }
                        else
                        {
                            note = "Tự tìm máng ăn trong tầm 30m và ăn khẩu phần bổ sung.";
                        }
                    }
                    interactions++;
                }

                // 5. Nắng Gắt & Comfort thấp -> Tìm nguồn mát (nếu Bơm Làm Mát đang có điện)
                if (engine.CurrentWeather == WeatherType.NangGat && p.Comfort < 65 && bestUtility < 75f)
                {
                    bestState = PigUtilityBehaviorState.SeekingWaterOrCooling;
                    bestUtility = 75f;
                    if (coolingPowered)
                    {
                        note = "Di chuyển về vùng Bơm Làm Mát đang hoạt động.";
                    }
                    else
                    {
                        note = "Tìm nguồn mát nhưng Lưới Điện đang sập cầu dao -> Bơm Làm Mát ngừng chạy!";
                    }
                    interactions++;
                }

                // 6. Chó Chăn Đàn tự động gom heo đi lạc ngoài tầm 30m về gần chuồng (khi đã bật Thú cưng Chó Chăn Đàn)
                if (hasShepherdDog && (p.PositionX * p.PositionX + p.PositionY * p.PositionY > 900f) && bestUtility < 70f)
                {
                    p.PositionX = MoveTowards(p.PositionX, 0f, 6.0f);
                    p.PositionY = MoveTowards(p.PositionY, 0f, 6.0f);
                    bestState = PigUtilityBehaviorState.FlockingWithAlphaLeader;
                    bestUtility = 70f;
                    note = "Chó Chăn Đàn tự động lùa heo ngoài tầm 30m quay về vùng phục vụ.";
                    interactions++;
                }

                // 7. Ban đêm không có Đèn Pha Công Nghiệp có điện -> heo đứng xa trại bị giảm nhẹ sự thoải mái
                if ((engine.Clock.CurrentHour >= 20 || engine.Clock.CurrentHour < 5) && !floodlightPowered)
                {
                    if (p.PositionX * p.PositionX + p.PositionY * p.PositionY > 400f)
                    {
                        p.Comfort = Math.Max(0, p.Comfort - 1);
                        interactions++;
                    }
                }

                LastHourlyAgentDecisions.Add(new PigAutonomousAgentDecision
                {
                    PigId = p.Id,
                    ChosenState = bestState,
                    UtilityScore = bestUtility,
                    OldX = oldX,
                    OldY = oldY,
                    NewX = p.PositionX,
                    NewY = p.PositionY,
                    CrossSystemInteractionNote = note
                });
            }

            // Đồng hồ tâm lý Góc Nhìn Kép (07:02 sáng & 02:00 đêm)
            if (engine.Clock.CurrentHour == 7 || engine.Clock.CurrentHour == 2)
            {
                float pressureFactor = engine.Farm.EventPressureIndex / 100f;
                CurrentReflectiveSurfaceLagSeconds = engine.Story.SeventhChannelCut
                    ? 0.0f
                    : (float)Math.Round(0.5f + pressureFactor * 1.5f, 2);
                interactions++;
            }

            TotalEmergentInteractionsProcessed += interactions;
            return interactions;
        }

        /// <summary>
        /// Đồng bộ mạng lưới Thiện cảm NPC Thị trấn Bờ Rìa (Story.NpcTrustTier) thành các hệ số cơ học trực tiếp trên Trại, Chợ, Điện và Y tế.
        /// </summary>
        public (int effectiveQuarantineFee, float wireRepairDiscount, int bonusVetCures, int bonusSuccessionPct) ApplyNpcTrustNetworkToMechanics(GameEngine engine)
        {
            if (engine == null) return (60, 0f, 0, 0);

            int leTrust = engine.Story.NpcTrustTier.TryGetValue("Le", out int lt) ? lt : 1;
            int tamVaTrust = engine.Story.NpcTrustTier.TryGetValue("TamVa", out int tv) ? tv : 1;
            int dungTrust = engine.Story.NpcTrustTier.TryGetValue("BacSiDung", out int dt) ? dt : 1;
            int manTrust = engine.Story.NpcTrustTier.TryGetValue("Man", out int mt) ? mt : 1;

            // Lệ (Kiểm dịch): Trust cao giảm phí kiểm dịch (60G -> 48G); nếu bán heo bệnh lậu làm Trust = 0 thì phí tăng lên 90G
            int baseQuarantine = engine.Economy.DayMarket.Theme == MarketTheme.HoangLoanDich ? 90 : 60;
            int effectiveQuarantine = leTrust == 0
                ? 90
                : Math.Max(40, baseQuarantine - (leTrust - 1) * 6);

            // Nếu An mở khóa 'Mặc Cả 90%' (Khóa #6) hoặc 'Mặc Cả Mềm' (Khóa #1) -> giảm thêm 10%
            if (engine.Story.ActiveProtagonist == PlayerId.An && engine.DeepMechanics.UnlockedSpecialtyLocksCount >= 1)
            {
                effectiveQuarantine = (int)Math.Round(effectiveQuarantine * 0.90f);
            }
            engine.Economy.DayMarket.QuarantineFeePerPig = effectiveQuarantine;

            // Tám Vá (Thợ máy): Mỗi bậc Trust > 1 giảm 10% phí sửa chữa dây/trụ điện và giúp cầu dao tự đóng lại nhanh hơn
            float repairDiscount = Math.Min(0.40f, Math.Max(0, tamVaTrust - 1) * 0.10f);
            if (tamVaTrust >= 3 && engine.DeepMechanics.PowerGrid.IsBreakerTripped)
            {
                engine.DeepMechanics.PowerGrid.BreakerTripRemainingSeconds =
                    Math.Max(0f, engine.DeepMechanics.PowerGrid.BreakerTripRemainingSeconds - 1.5f);
            }

            // Bác sĩ Dung: Trust >= 3 giúp ca trực thú y chữa thêm +1 heo/ngày và tẩy trùng máng nhiễm khuẩn
            int bonusCures = dungTrust >= 3 ? 1 : 0;
            if (dungTrust >= 2 && ContaminatedTroughsCount > 0)
            {
                ContaminatedTroughsCount = Math.Max(0, ContaminatedTroughsCount - 1);
            }

            // Mận: Trust >= 3 cộng thêm +1% tiến độ Bồi Dưỡng Kế Vị mỗi ngày
            int bonusSuccession = manTrust >= 3 ? 1 : 0;

            TotalEmergentInteractionsProcessed += 4;
            return (effectiveQuarantine, repairDiscount, bonusCures, bonusSuccession);
        }

        /// <summary>
        /// Mô phỏng chi tiết giao tranh Đợt Quái Đa Chủng Loại (Kẻ Cắt Dây, UFO Chùm Kéo, Đạo Tặc Rìu Sắt, Thú Bão)
        /// tương tác trực tiếp với Dây Điện Nổi/Ngầm, Tháp Skyhook, Chiến Trư Xích Mao, Hào Quang Heo Già và Nơi Trú Đàn.
        /// </summary>
        public TacticalRaidEngagementSummary ExecuteMultiArchetypeTacticalRaid(
            GameEngine engine,
            int waveIndex,
            int baseThreatBudget,
            bool isBossWave,
            bool triggerBachVan)
        {
            var summary = new TacticalRaidEngagementSummary
            {
                WaveIndex = waveIndex,
                InitialThreatBudget = baseThreatBudget
            };

            if (engine == null) return summary;

            int uncollectedCorpses = engine.Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count;
            int wealthAndCorpseBonus = engine.Defense.PendingThreatBudgetBonus + uncollectedCorpses * 15 + (engine.Farm.Gold / 1000) * 5;
            summary.EffectiveThreatWithWealthAndCorpseScent = Math.Max(40, baseThreatBudget + wealthAndCorpseBonus);

            // 1. Kẻ Cắt Dây tấn công các tuyến dây điện nổi (IsUnderground == false)
            for (int i = 0; i < engine.DeepMechanics.PowerGrid.Wires.Count; i++)
            {
                var wire = engine.DeepMechanics.PowerGrid.Wires[i];
                if (wire.IsUnderground)
                {
                    summary.UndergroundWiresProtectedCount++;
                }
                else if (!wire.IsCut && summary.AboveGroundWiresCutBySaboteurs == 0 && summary.EffectiveThreatWithWealthAndCorpseScent >= 80)
                {
                    engine.DeepMechanics.PowerGrid.TryCutWireByEnemy(wire.WireId, engine.Defense.Buildings);
                    summary.AboveGroundWiresCutBySaboteurs++;
                }
            }

            // 2. UFO Trinh Sát chiếu chùm kéo bắt cóc heo Quý/Hiếm ngoài Nơi Trú vs Tháp Phòng Không Skyhook (Nhánh 3A_CatTiaKeo)
            summary.UfoAbductionAttempts = summary.EffectiveThreatWithWealthAndCorpseScent >= 100 ? 1 : 0;
            if (summary.UfoAbductionAttempts > 0 && engine.Defense.HasPoweredBuilding("ThapPhongKhongSkyhook"))
            {
                summary.UfoBeamsSeveredBySkyhook = 1;
            }

            // 3. Chiến trư Xích Mao đã thuần dưỡng (IsXichMaoDomesticated) xuất kích bảo vệ Thủ Lĩnh Đàn & hàng rào
            int domesticatedXichMao = engine.Pigs.FindAll(p =>
                p.IsAlive && !p.IsIsolated && p.GeneLine == GeneLineId.XichMao && p.IsXichMaoDomesticated).Count;
            summary.DomesticatedXichMaoCounterAttackDamage = domesticatedXichMao * 45;

            // 4. Thực thi đợt phòng thủ chính trên GameEngine (đồng bộ lưới điện đỉnh, bắn tháp, âm chấn, báo cáo 7 mục)
            int adjustedThreat = Math.Max(30, summary.EffectiveThreatWithWealthAndCorpseScent - summary.DomesticatedXichMaoCounterAttackDamage);
            var (waveRep, breakerTripped, miscarried, calmAura) = engine.ExecuteLiveCombatWave(
                waveIndex,
                adjustedThreat,
                isBossWave,
                triggerBachVan);

            summary.TowerSalvoDamage = waveRep.EnemiesDefeated * 20;
            summary.AcousticShockDecibels = engine.Defense.LastWaveAcousticShockScore;
            summary.ElderCalmAuraMitigation = calmAura;
            summary.UnshelteredPregnantSowsMiscarried = miscarried;
            summary.PowerBreakerTrippedDuringPeak = breakerTripped;
            summary.Victory = waveRep.Victory;
            summary.PostBattleMarketShift = engine.Economy.DayMarket.Theme;

            LastTacticalRaidSummary = summary;
            TotalEmergentInteractionsProcessed += 8;
            return summary;
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta) return target;
            return current + Math.Sign(target - current) * maxDelta;
        }
    }
}
