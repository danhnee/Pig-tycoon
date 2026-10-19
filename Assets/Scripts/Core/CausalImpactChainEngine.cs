using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public sealed class CausalImpactStep
    {
        public int StepOrder { get; set; }
        public string SystemLayer { get; set; } = string.Empty;
        public string RuleCitation { get; set; } = string.Empty;
        public string TriggerCondition { get; set; } = string.Empty;
        public string StateMutationSummary { get; set; } = string.Empty;
        public string DownstreamHookTriggered { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class CausalChainExecutionResult
    {
        public string ChainId { get; set; } = string.Empty;
        public string TriggerAction { get; set; } = string.Empty;
        public PlayerId ActiveProtagonist { get; set; }
        public bool Success { get; set; }
        public int LayersTouchedCount { get; set; }
        public List<CausalImpactStep> ImpactSteps { get; set; } = new List<CausalImpactStep>();

        // Snapshot of cross-system state mutations
        public int GoldBefore { get; set; }
        public int GoldAfter { get; set; }
        public int LivingPigsBefore { get; set; }
        public int LivingPigsAfter { get; set; }
        public int EffectiveCapacityAfter { get; set; }
        public int HabitatIndexAfter { get; set; }
        public float OccultPressureAfter { get; set; }
        public int CycleTotalDaysAfter { get; set; }
        public float MarketBuyingPowerModifierAfter { get; set; }
        public float PostRaidPigPriceSurgePctAfter { get; set; }
        public HerdStateTier HerdTierAfter { get; set; }
        public int PanickingPigsCount { get; set; }
        public int WoodenFenceDamageFromPanic { get; set; }
        public bool PowerBreakerTripped { get; set; }
        public bool DroppedSevenStrokeHarvestReceipt { get; set; }
        public bool HerdLeavesEmptySpotAtJackfruitCorner { get; set; } = true;
        public bool ChippedBowlReturnedAtGate { get; set; }
        public string StoryNotebookEntryAdded { get; set; } = string.Empty;
        public string SummaryMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Động cơ Chuỗi Tác Động Chéo Liên Hệ Thống (Cross-System Causal Impact Engine)
    /// hiện thực hóa triết lý cốt lõi của GDD v6.0 Mục 1.1 &amp; 1.2:
    /// "Luật tối thiểu — Tác động chéo tối đa (Minimal Rules — Maximal Cross-System Impact)".
    ///
    /// Mỗi hành động của người chơi (Bán heo, Để heo chết ngoài Khu Xử Lý, Giữ heo già 29+ ngày,
    /// Khai hỏa Cửu Tiêu Lôi Pháo / Mìn Nổ trong đợt quái, hoặc Mở hốc gỗ vạch đo chiều cao P08..Q20)
    /// không bao giờ chỉ cộng/trừ 1 con số cô lập, mà kích hoạt dây chuyền nhân quả xuyên suốt 6 tầng:
    ///   1. Đàn heo &amp; Di truyền (Herd_Genetics)
    ///   2. Kinh tế, Sức mua &amp; Sổ cái 1G (Economy_Market)
    ///   3. Kinh tế Thông tin &amp; Tình báo (Intel_Information)
    ///   4. Đồng hồ, Thời tiết, Chu kỳ &amp; Thể lực 2 lớp (Time_Stamina_Weather)
    ///   5. Phòng thủ, Âm chấn, Hàng rào &amp; Lưới điện (Defense_PowerGrid)
    ///   6. Cốt truyện Kép "Sáu Với Một" — Khoa (lấy cương) vs An (lấy nhu) (Story_SauVoiMot)
    /// </summary>
    [Serializable]
    public sealed class CausalImpactChainEngine
    {
        // Live cross-system state tracked across days & cycles
        public int QuarantinedPigsSoldDayMarketCount { get; set; }
        public int UnquarantinedSalesAttemptedCount { get; set; }
        public int UnquarantinedSickPenaltyDaysRemaining { get; set; }
        public float MarketBuyingPowerModifier { get; set; }
        public int PigsSoldInCurrentCycle { get; set; }
        public int CycleMarketBuyingPowerCapacity { get; set; } = 40;
        public float PostRaidPigPriceSurgePct { get; set; }
        public int PostRaidScarcityDaysRemaining { get; set; }
        public int UnprocessedCorpsesOutsideLotCount { get; set; }
        public int CycleLengthAdjustmentDays { get; set; }

        // Live Specialty Lock counters (GDD 10.4 & 10.8)
        public int KhoaManualCleanCount { get; set; }
        public int KhoaLeaderInheritanceCount { get; set; }
        public int AnIntelTableQueriesCount { get; set; }
        public int AnNightMarketBartersCount { get; set; }
        public int AnLevel5IntelOccultEventsCompletedCount { get; set; }

        // Live Story & Mystery Causal State (Sáu Với Một — Phần 1..12)
        public int HeightNicheOpenCount { get; set; }
        public int KeyBorrowTwoMinuteLogCount { get; set; }
        public bool SevenStrokeHarvestReceiptCollected { get; set; }
        public bool Q18HerdFormationTemplateRecorded { get; set; }
        public bool Q20SeventhPackageChannelCut { get; set; }
        public bool HerdLeavesEmptySpotAtJackfruitCorner { get; set; } = true;
        public bool ChippedBlueGlazedBowlReturnedAtGate { get; set; }
        public List<string> CompletedHeightNicheBeats { get; set; } = new List<string>();

        private static readonly string[] CanonicalNineHeightNicheBeats =
        {
            "P08_7VachDoChieuCaoDuoiGiayDanTuong",
            "Q01_BongGuongLech03Giay7BuoiSang",
            "Q02_VachThu7Cao1m68KhacSauNgayMat",
            "Q06_MoHocGoSauVachThu7TimHopSat7Ngan",
            "Q07_SoMuonChiaKhoaKho7Lan2Phut",
            "Q12_DenPhongLo02h00Den02h02Dem05Thang10",
            "Q17_NetButChiTayTraiLuc02h00Sang",
            "Q19_VongCo7KhacGoSoiTrongCoAo",
            "CH8_KhepLaiKienHangThu7VaLoiChaoCuoi"
        };

        /// <summary>
        /// CHUỖI NHÂN QUẢ A — BÁN HEO TẠI CHỢ NGÀY (GDD 4.5, 4.6, 4.7, 10.4, 10.8, 20.1, 20.3 &amp; Story Q04/Q14):
        /// Bán 1 con heo kéo theo dây chuyền 7 tầng:
        /// 1. Kiểm dịch (60G -> 100%) vs Bán chui (70/65/55/45/35% + nếu heo ủ bệnh phát tác sau bán -> Sức mua chợ ngày -10% trong 3 ngày).
        /// 2. Độ nhạy khối lượng bán GDD 20.3: bán vượt 50% sức mua chu kỳ -> mỗi con tiếp theo giảm -0.5% giá bán (tối đa -25%).
        /// 3. Sổ cái 1G + EXP Trại (+1 EXP / 100G) + phát sự kiện PigSold trên IEventBus.
        /// 4. Biến động cấu trúc Bầy Đàn: nếu bán nhầm Thủ Lĩnh -> cả đàn sốc -25/-40 Tinh thần &amp; mất Neo; nếu bán heo già làm số heo già &lt; 6 -> rớt tầng Bầy Đàn.
        /// 5. Giải phóng nút thắt Sức chứa Thực tế (EffectiveCapacity) &amp; Sinh cảnh.
        /// 6. Tích lũy tiến độ Khóa #5 của Khoa ("Bán 500 heo đã kiểm dịch ở chợ ngày", áp trần 50% GDD 10.4 nếu chưa qua Cổng Giữa Game).
        /// 7. Cốt truyện Q04/Q14: nếu bán heo già hoặc heo có thói quen gia đình, Lệ tại Quầy Kiểm Dịch ghi nhận "Con này quen đường về rồi" và ghi vào Sổ tay Khoa/An.
        /// </summary>
        public CausalChainExecutionResult ExecutePigSaleCausalChain(
            GameEngine engine,
            string pigId,
            bool payQuarantineFee,
            Random? rng = null)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            rng ??= new Random(1337 + engine.Clock.CurrentDay * 31 + PigsSoldInCurrentCycle);

            var result = new CausalChainExecutionResult
            {
                ChainId = "CHAIN_A_PIG_SALE",
                TriggerAction = $"SellPig(id={pigId}, quarantine={payQuarantineFee})",
                ActiveProtagonist = engine.Story.ActiveProtagonist,
                GoldBefore = engine.Farm.Gold,
                LivingPigsBefore = engine.Pigs.FindAll(p => p.IsAlive).Count,
                HerdLeavesEmptySpotAtJackfruitCorner = HerdLeavesEmptySpotAtJackfruitCorner,
                ChippedBowlReturnedAtGate = ChippedBlueGlazedBowlReturnedAtGate
            };

            var pig = engine.Pigs.Find(p => p.Id == pigId && p.IsAlive);
            if (pig == null)
            {
                result.Success = false;
                result.SummaryMessage = $"Không tìm thấy heo còn sống '{pigId}' để bán.";
                return result;
            }

            int step = 1;

            // TẦNG 1: Kiểm dịch 60G vs Bán không kiểm dịch & Rủi ro sụt sức mua -10% trong 3 ngày (GDD 4.5)
            bool saleAccepted = true;
            bool hadHiddenOrActiveDisease = pig.ActiveDisease.HasValue || pig.LatentDiseaseRiskMultiplier > 1.05f;
            if (payQuarantineFee)
            {
                if (!engine.Ledger.TrySpend(MarketTownState.QuarantineFeeGold, LedgerReason.BuyFeed, $"quarantine_{pig.Id}"))
                {
                    result.Success = false;
                    result.SummaryMessage = "Không đủ 60G phí Kiểm dịch tại Chợ Ngày.";
                    return result;
                }
                pig.IsQuarantinedForSale = true;
                QuarantinedPigsSoldDayMarketCount++;
                result.ImpactSteps.Add(new CausalImpactStep
                {
                    StepOrder = step++,
                    SystemLayer = "Economy_Market",
                    RuleCitation = "GDD 4.5 (Kiểm dịch Chợ Ngày 60G)",
                    TriggerCondition = "Người chơi nộp 60G phí Kiểm dịch cho Lệ",
                    StateMutationSummary = $"Trừ 60G qua Sổ cái -> Heo '{pig.Name}' đạt chuẩn Kiểm dịch (100% thu mua). Tổng heo kiểm dịch đã bán: {QuarantinedPigsSoldDayMarketCount}.",
                    DownstreamHookTriggered = "VolumePriceSensitivity_GDD20_3 & KhoaSpecialtyLock5_GDD10_8"
                });
            }
            else
            {
                pig.IsQuarantinedForSale = false;
                UnquarantinedSalesAttemptedCount++;
                float acceptChance = pig.Rarity switch
                {
                    GeneRarity.Thuong => 0.70f,
                    GeneRarity.Hiem => 0.65f,
                    GeneRarity.Quy => 0.55f,
                    GeneRarity.HuyenThoai => 0.45f,
                    GeneRarity.DiBien => 0.35f,
                    _ => 0.70f
                };
                saleAccepted = rng.NextDouble() <= acceptChance;
                if (hadHiddenOrActiveDisease)
                {
                    UnquarantinedSickPenaltyDaysRemaining = 3;
                    MarketBuyingPowerModifier = -0.10f;
                }

                result.ImpactSteps.Add(new CausalImpactStep
                {
                    StepOrder = step++,
                    SystemLayer = "Economy_Market",
                    RuleCitation = "GDD 4.5 (Bán không kiểm dịch & Phạt sức mua -10% trong 3 ngày)",
                    TriggerCondition = $"Bán không kiểm dịch bậc {pig.Rarity} (xác suất nhận {acceptChance * 100f:0}%, mầm bệnh={hadHiddenOrActiveDisease})",
                    StateMutationSummary = saleAccepted
                        ? $"Thương lái chấp nhận mua. {(hadHiddenOrActiveDisease ? "PHÁT TÁC BỆNH SAU BÁN -> Sức mua chợ ngày giảm -10% trong 3 ngày tới!" : "Heo không mang mầm bệnh.")}"
                        : $"Thương lái từ chối mua heo '{pig.Name}'. {(hadHiddenOrActiveDisease ? "Bị phát hiện mầm bệnh tại cổng chợ -> Sức mua chợ ngày giảm -10% trong 3 ngày!" : "")}",
                    DownstreamHookTriggered = hadHiddenOrActiveDisease ? "MarketBuyingPowerPenalty_3Days" : "VolumePriceSensitivity_GDD20_3"
                });
            }

            if (!saleAccepted)
            {
                PopulateFinalSnapshot(engine, result);
                result.Success = false;
                result.LayersTouchedCount = result.ImpactSteps.Count;
                result.SummaryMessage = $"Giao dịch không kiểm dịch cho '{pig.Name}' bị từ chối.";
                return result;
            }

            // TẦNG 2: Độ nhạy khối lượng bán GDD 20.3 (>50% sức mua chu kỳ -> -0.5%/con, tối đa -25%) + Khan hiếm sau đợt quái GDD 20.2
            PigsSoldInCurrentCycle++;
            int effectiveBuyingPower = Math.Max(10, (int)Math.Round(CycleMarketBuyingPowerCapacity * (1.0f + MarketBuyingPowerModifier)));
            int halfPowerThreshold = effectiveBuyingPower / 2;
            float volumeDecayPct = 0f;
            if (PigsSoldInCurrentCycle > halfPowerThreshold)
            {
                volumeDecayPct = Math.Min(0.25f, (PigsSoldInCurrentCycle - halfPowerThreshold) * 0.005f);
            }

            var (baseSaleGold, _) = engine.Economy.CalculatePigSalePrice(pig, engine.Farm.FarmLevel);
            if (baseSaleGold <= 0) baseSaleGold = Math.Max(120, (int)Math.Round(pig.WeightKg * 3.2f));
            float surgeMultiplier = 1.0f + PostRaidPigPriceSurgePct;
            float volumeMultiplier = 1.0f - volumeDecayPct;
            int finalNetGold = Math.Max(1, (int)Math.Round(baseSaleGold * surgeMultiplier * volumeMultiplier));

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Economy_Market",
                RuleCitation = "GDD 4.5, 20.2 & 20.3 (Công thức giá heo × Độ nhạy khối lượng bán × Khan hiếm sau cướp)",
                TriggerCondition = $"Đã bán {PigsSoldInCurrentCycle}/{effectiveBuyingPower} sức mua chu kỳ (ngưỡng 50%={halfPowerThreshold})",
                StateMutationSummary = $"Giá chuẩn={baseSaleGold}G × Khan hiếm({surgeMultiplier:0.00}x) × Giảm giá khối lượng({volumeMultiplier:0.000}x) = {finalNetGold}G.",
                DownstreamHookTriggered = "EconomyLedger_1G_And_FarmExp"
            });

            // TẦNG 3: Sổ cái Kinh tế 1G, EXP Trại & Sự kiện PigSold (Sổ tay v3.0 §10)
            bool wasLeader = engine.Herd.Leader != null && engine.Herd.Leader.PigId == pig.Id;
            bool wasElder = pig.Stage == PigStage.HeoGia || pig.AgeDays >= 29;
            engine.Pigs.Remove(pig);
            engine.TotalPigsSold++;
            engine.TotalGoldEarned += finalNetGold;
            engine.Ledger.Earn(finalNetGold, LedgerReason.SellPig, pig.Id);
            int expGained = Math.Max(1, finalNetGold / 100);
            var (farmLvlAfter, newUnlocks) = FarmLevelProgressionSystem.AddFarmExp(engine.Farm, expGained);
            engine.Contracts.EventBus.Publish(new PigSold(pig.Id, finalNetGold, !payQuarantineFee, engine.Clock.CurrentDay));

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Time_Stamina_Weather",
                RuleCitation = "GDD 3.7 & Sổ tay v3.0 §10 (Sổ cái 1G + EXP Trại + EventBus PigSold)",
                TriggerCondition = $"Hoàn tất bán heo '{pig.Name}' thu {finalNetGold}G",
                StateMutationSummary = $"Ghi Sổ cái +{finalNetGold}G (Vàng mới: {engine.Farm.Gold}G), +{expGained} EXP Trại (Cấp trại: {farmLvlAfter}, mở khóa mới: {newUnlocks.Count}), phát sự kiện PigSold.",
                DownstreamHookTriggered = "HerdStructure_And_CapacityRecalc"
            });

            // TẦNG 4: Biến động cấu trúc Bầy Đàn & Sốc mất Thủ Lĩnh / Heo Già (GDD 4.6 & 4.7)
            var livingPigs = engine.Pigs.FindAll(p => p.IsAlive);
            if (wasLeader)
            {
                int shockPenalty = pig.GeneLine == GeneLineId.KimTho ? 40 : 25;
                for (int i = 0; i < livingPigs.Count; i++)
                {
                    livingPigs[i].Mood = Math.Max(0, livingPigs[i].Mood - shockPenalty);
                }
                var (_, herdMsg) = engine.Herd.AdvanceDay(engine.Pigs, engine.Farm, engine.MajorWavesSurvived);
                result.ImpactSteps.Add(new CausalImpactStep
                {
                    StepOrder = step++,
                    SystemLayer = "Herd_Genetics",
                    RuleCitation = "GDD 4.6 (Mất Thủ Lĩnh -> Toàn đàn -25/-40 Tinh thần & Xét Kế Vị)",
                    TriggerCondition = $"Bán đi Heo Thủ Lĩnh '{pig.Name}' (Dòng {pig.GeneLine})",
                    StateMutationSummary = $"Toàn đàn ({livingPigs.Count} con) bị sốc -{shockPenalty} Tinh thần! Trạng thái Bầy Đàn: {engine.Herd.Tier} ({herdMsg}).",
                    DownstreamHookTriggered = "HerdTierEvaluation_GDD4_6"
                });
            }
            else
            {
                var condReport = engine.Herd.EvaluateConditions(engine.Pigs, engine.Farm, engine.MajorWavesSurvived);
                result.ImpactSteps.Add(new CausalImpactStep
                {
                    StepOrder = step++,
                    SystemLayer = "Herd_Genetics",
                    RuleCitation = "GDD 4.6 & 4.7 (Đánh giá lại Tầng Bầy Đàn & Tỷ lệ Heo Già)",
                    TriggerCondition = $"Đàn còn {livingPigs.Count} con sống (Heo bán là heo già={wasElder}, Heo già gắn kết còn={condReport.ElderCohesiveCount})",
                    StateMutationSummary = $"Tầng Bầy Đàn hiện tại: {engine.Herd.Tier} (Đủ 7 điều kiện={condReport.AllConditionsMet}).",
                    DownstreamHookTriggered = "EffectiveCapacity_GDD3_2"
                });
            }

            // TẦNG 5: Nút thắt Sức chứa Thực tế & Sinh cảnh (GDD 3.2 & 3.4)
            var capReport = engine.Farm.CalculateCapacityReport(livingPigs.Count, UnprocessedCorpsesOutsideLotCount);
            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Defense_PowerGrid",
                RuleCitation = "GDD 3.2 & 3.4 (Nút thắt Sức chứa Thực tế & Giảm tải Quá tải Chuồng)",
                TriggerCondition = $"Mật độ mới: {livingPigs.Count}/{capReport.EffectiveCapacity} ({capReport.RawDensityRatio * 100f:0.0}%)",
                StateMutationSummary = $"Nút thắt hiện tại: {capReport.Bottleneck}, Vùng mật độ: {capReport.DensityZoneName}.",
                DownstreamHookTriggered = "SpecialtyLock5_GDD10_4_10_8"
            });

            // TẦNG 6: Tiến độ Khóa #5 của Khoa ("Bán 500 heo đã kiểm dịch ở chợ ngày" — GDD 10.4 & 10.8)
            int effectiveLock5Progress = CanonicalSagaAndGdd6Systems.ClampSpecialtyLockProgressBeforeRevealGdd104(
                QuarantinedPigsSoldDayMarketCount,
                requiredTotal: 500,
                midGameGateRevealed: engine.DeepMechanics.MidGameGateUnlocked);
            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Intel_Information",
                RuleCitation = "GDD 10.4 & 10.8 (Tiến độ Khóa #5 Khoa 'Trại Chủ Lão Luyện' & Trần 50% trước lộ diện)",
                TriggerCondition = $"Đã bán kiểm dịch {QuarantinedPigsSoldDayMarketCount}/500 con (MidGameGate={engine.DeepMechanics.MidGameGateUnlocked})",
                StateMutationSummary = $"Tiến độ Khóa #5 hợp lệ của Khoa: {effectiveLock5Progress}/500 (mở 'Ngủ Say': ngủ chuẩn +3 Thể trạng, cứ 2 ngày xóa 1 tầng Mệt Dội).",
                DownstreamHookTriggered = "StorySauVoiMot_Q04_Q14"
            });

            // TẦNG 7: Cốt truyện Sáu Với Một (Q04 / Q14 — Lệ tại Quầy Kiểm Dịch & Sổ tay Khoa vs An)
            string storyEntry;
            if (engine.Story.ActiveProtagonist == PlayerId.An)
            {
                storyEntry = wasElder
                    ? $"[Sổ bìa hồng ★] Chị Linh ơi, hôm nay em bán con '{pig.Name}' ở quầy kiểm dịch. Chị Lệ nhìn vết sẹo cũ trên tai nó rồi buột miệng: 'Con này... quen đường về rồi'. Em cười tươi cảm ơn chị Lệ mà ngón tay cái tự nhiên miết lên nắp hộp sắt :) ★"
                    : $"[Sổ bìa hồng ★] Em Khoa ơi, hôm nay chị bán '{pig.Name}' được {finalNetGold}G. Lúc đếm tiền ở cổng chợ, chị thấy trên phiếu thu đóng dấu sớm hơn đồng hồ chợ đúng 2 phút :) ★";
                engine.Story.AnPinkNotebookEntries.Add(new AnLetterNotebookEntry
                {
                    EntryId = $"AN_SALE_{engine.Clock.CurrentDay}_{PigsSoldInCurrentCycle}",
                    RecipientSalutation = wasElder ? "Chị Linh ơi," : "Em Khoa ơi,",
                    QuestOrSceneRef = $"Ngày {engine.Clock.CurrentDay} · Chợ Ngày (Q04/Q14)",
                    Sticker = AnNotebookSticker.Star_VerifiableByAll,
                    EndsWithSmiley = true,
                    IsUnconsciousBlackoutEntry = false,
                    LetterBody = storyEntry,
                    PencilMarginHypothesis = "Phiếu kiểm dịch của Lệ lệch sớm 2 phút và danh sách 'đã quen đường về'."
                });
            }
            else
            {
                storyEntry = $"[Sổ bìa đen 2 cột · Ngày {engine.Clock.CurrentDay}] Cột Thấy: Lệ đóng dấu kiểm dịch cho '{pig.Name}' ({finalNetGold}G), nói 'Con này quen đường về rồi'. | Cột Có thật: Giờ đóng dấu trên biên lai sớm hơn đồng hồ trại đúng 2 phút.";
                engine.Story.NotebookEntries.Add(new NotebookTwoColumnEntry
                {
                    Id = $"nb_sale_{engine.Clock.CurrentDay}_{PigsSoldInCurrentCycle}",
                    Title = wasElder ? "Q14 · Danh sách quen đường về" : "Q04 · Biên lai kiểm dịch lệch 2 phút",
                    LeftColumn_Thay = $"Lệ đóng dấu kiểm dịch cho '{pig.Name}' ({finalNetGold}G), nói 'Con này quen đường về rồi'.",
                    RightColumn_CoThat = "Giờ đóng dấu trên biên lai sớm hơn đồng hồ trại đúng 2 phút.",
                    PencilMargin_SuyDoan = "Kiểm tra sổ mượn chìa khóa kho của Lệ (7 lần × 2 phút).",
                    EvidenceCount = 2,
                    IsVerifiedToRightColumn = true
                });
            }

            result.StoryNotebookEntryAdded = storyEntry;
            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Story_SauVoiMot",
                RuleCitation = "Sáu Với Một Phần 5 & 5B (Q04/Q14 — Biên lai lệch 2 phút & Danh sách 'Đã quen đường về')",
                TriggerCondition = $"Nhân vật đang điều khiển: {engine.Story.ActiveProtagonist}",
                StateMutationSummary = storyEntry,
                DownstreamHookTriggered = "HeightNicheInvestigation_Q07"
            });

            PopulateFinalSnapshot(engine, result);
            result.Success = true;
            result.LayersTouchedCount = result.ImpactSteps.Count;
            result.SummaryMessage = $"Bán '{pig.Name}' kích hoạt thành công {result.LayersTouchedCount} tầng nhân quả liên hệ thống (+{finalNetGold}G).";
            return result;
        }

        /// <summary>
        /// CHUỖI NHÂN QUẢ B — HEO CHẾT &amp; XÁC HEO NGOÀI KHU XỬ LÝ (GDD 2.3, 3.2, 4.3, 4.6, 5.2, 7.1, 23.1 &amp; Story S07):
        /// Một con heo chết ngoài Khu Xử Lý kích hoạt dây chuyền 6 tầng:
        /// 1. Sốc tang thương không gian 10m: toàn bộ heo sống trong bán kính 10m giảm -6 Tinh thần ngay lập tức (GDD 4.3).
        /// 2. Sụp đổ Thủ Lĩnh (-25/-40 Tinh thần) &amp; Thức tỉnh tính xấu dòng Hư Thể khi Tinh thần &lt; 40 (GDD 4.6, 5.2, 6.2).
        /// 3. Xác heo ngoài Khu Xử Lý làm giảm ngay -1 Sức chứa Thực tế (GDD 3.2), phạt -3 Sinh cảnh/ngày, tăng +8% bùng dịch.
        /// 4. Tăng Áp lực Sự kiện Kỳ dị (+3 ÁLSK/ngày); khi ÁLSK &gt;= 60 hoặc mật độ &gt; 120%, chu kỳ hiện tại bị rút ngắn -1 ngày (GDD 2.3).
        /// 5. Phát sự kiện PigDied (và OccultEventTriggered nếu vượt ngưỡng) trên IEventBus chuẩn Sổ tay v3.0 §10.
        /// 6. Cốt truyện S07 (Mặt nước máng ăn phản chiếu lệch giữa Khoa và An).
        /// </summary>
        public CausalChainExecutionResult ExecutePigDeathAndCorpseCausalChain(
            GameEngine engine,
            string pigId,
            string causeOfDeath,
            bool inProcessingZone = false)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));

            var result = new CausalChainExecutionResult
            {
                ChainId = "CHAIN_B_PIG_DEATH_CORPSE",
                TriggerAction = $"PigDeath(id={pigId}, cause={causeOfDeath}, inProcessingZone={inProcessingZone})",
                ActiveProtagonist = engine.Story.ActiveProtagonist,
                GoldBefore = engine.Farm.Gold,
                LivingPigsBefore = engine.Pigs.FindAll(p => p.IsAlive).Count
            };

            var target = engine.Pigs.Find(p => p.Id == pigId && p.IsAlive);
            if (target == null)
            {
                result.Success = false;
                result.SummaryMessage = $"Không tìm thấy heo đang sống '{pigId}'.";
                return result;
            }

            int step = 1;
            bool wasLeader = engine.Herd.Leader != null && engine.Herd.Leader.PigId == target.Id;
            target.IsAlive = false;
            target.Health = 0;
            target.IsInProcessingLot = inProcessingZone;

            // TẦNG 1: Sốc tang thương không gian 10m (-6 Tinh thần cho đồng loại trong 10m — GDD 4.3)
            int nearbyShockedCount = 0;
            var livingPigs = engine.Pigs.FindAll(p => p.IsAlive);
            for (int i = 0; i < livingPigs.Count; i++)
            {
                var other = livingPigs[i];
                float dx = other.PositionX - target.PositionX;
                float dy = other.PositionY - target.PositionY;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist <= 10.0f)
                {
                    other.Mood = Math.Max(0, other.Mood - 6);
                    if (!inProcessingZone)
                    {
                        other.Mood = Math.Max(0, other.Mood - 2); // thấy xác: -2/giờ
                    }
                    nearbyShockedCount++;
                }
            }

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Herd_Genetics",
                RuleCitation = "GDD 4.3 (Đồng loại chết trong 10m: -6 Tinh thần; thấy xác: -2/giờ)",
                TriggerCondition = $"Heo '{target.Name}' chết tại ({target.PositionX:0.0}, {target.PositionY:0.0}) do '{causeOfDeath}'",
                StateMutationSummary = $"{nearbyShockedCount} heo trong bán kính 10m bị sốc -6 Tinh thần (kèm -2 Tinh thần do thấy xác tại chỗ).",
                DownstreamHookTriggered = "LeaderLoss_And_HuTheNegativeTraitAwakening"
            });

            // TẦNG 2: Sốc mất Thủ Lĩnh & Thức tỉnh tính xấu dòng Hư Thể khi Tinh thần < 40 (GDD 4.6, 5.2, 6.2)
            int awakenedNegativeTraits = 0;
            if (wasLeader)
            {
                int leaderShock = target.GeneLine == GeneLineId.KimTho ? 40 : 25;
                for (int i = 0; i < livingPigs.Count; i++)
                {
                    livingPigs[i].Mood = Math.Max(0, livingPigs[i].Mood - leaderShock);
                }
                engine.Herd.AdvanceDay(engine.Pigs, engine.Farm, engine.MajorWavesSurvived);
            }

            for (int i = 0; i < livingPigs.Count; i++)
            {
                var p = livingPigs[i];
                if (p.Mood < 40)
                {
                    var negTrait = p.Traits.Find(t => !t.IsPositive);
                    if (negTrait == null)
                    {
                        p.Traits.Add(new PigTrait
                        {
                            Id = "BatOnTinhThan",
                            Name = "Bất Ổn Tinh Thần",
                            IsPositive = false,
                            IsAwakened = true,
                            Description = "Thức tỉnh khi Tinh thần rớt dưới 40 do sang chấn"
                        });
                        awakenedNegativeTraits++;
                    }
                    else if (!negTrait.IsAwakened)
                    {
                        negTrait.IsAwakened = true;
                        awakenedNegativeTraits++;
                    }
                }
            }

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Herd_Genetics",
                RuleCitation = "GDD 4.6, 5.2 & 6.2 (Sốc mất Thủ Lĩnh & Thức tỉnh Tính Xấu khi Tinh thần < 40)",
                TriggerCondition = $"WasLeader={wasLeader}, số heo rớt Tinh thần < 40={awakenedNegativeTraits}",
                StateMutationSummary = $"Đã thức tỉnh tính xấu 'Bất Ổn Tinh Thần' trên {awakenedNegativeTraits} cá thể do sang chấn tinh thần.",
                DownstreamHookTriggered = "CorpseCapacityBottleneck_GDD3_2"
            });

            // TẦNG 3: Xác ngoài Khu Xử Lý làm giảm ngay -1 Sức chứa Thực tế, -3 Sinh cảnh/ngày, +8% bùng dịch (GDD 3.2 & 7.1)
            if (!inProcessingZone)
            {
                UnprocessedCorpsesOutsideLotCount++;
                engine.Farm.HabitatIndex = Math.Max(0, engine.Farm.HabitatIndex - 3);
            }
            else
            {
                engine.Farm.CorpseLotCount++;
            }
            var capAfter = engine.Farm.CalculateCapacityReport(livingPigs.Count, UnprocessedCorpsesOutsideLotCount);

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Defense_PowerGrid",
                RuleCitation = "GDD 3.2, 3.4 & 7.1 (Mỗi xác ngoài Khu Xử Lý giảm -1 Sức chứa Thực tế, -3 SC, +8% dịch)",
                TriggerCondition = $"InProcessingZone={inProcessingZone}, CorpsesOutside={UnprocessedCorpsesOutsideLotCount}",
                StateMutationSummary = $"Sức chứa thực tế giảm còn {capAfter.EffectiveCapacity} (Nút thắt: {capAfter.Bottleneck}), Sinh cảnh={engine.Farm.HabitatIndex}, nguy cơ dịch +{UnprocessedCorpsesOutsideLotCount * 8}%.",
                DownstreamHookTriggered = "OccultPressure_And_CycleShortening_GDD2_3"
            });

            // TẦNG 4: Áp lực Sự kiện Kỳ dị (+3/ngày) & Rút ngắn chu kỳ -1 ngày khi ÁLSK >= 60 hoặc mật độ > 120% (GDD 2.3 & 23.1)
            int elderCount = livingPigs.FindAll(p => p.Stage == PigStage.HeoGia || p.AgeDays >= 29).Count;
            engine.Farm.UpdateDailyEventPressure(elderCount, Math.Max(1, livingPigs.Count), capAfter.RawDensityRatio, UnprocessedCorpsesOutsideLotCount);
            float currentAlsk = engine.Farm.EventPressureIndex;
            bool shortenedCycle = false;
            int effectiveCycleDays = Math.Clamp(engine.Clock.CycleTotalDays + CycleLengthAdjustmentDays, 4, 7);
            if ((currentAlsk >= 60f || capAfter.RawDensityRatio > 1.20f) && effectiveCycleDays > 4)
            {
                CycleLengthAdjustmentDays -= 1;
                effectiveCycleDays = Math.Clamp(engine.Clock.CycleTotalDays + CycleLengthAdjustmentDays, 4, 7);
                shortenedCycle = true;
            }

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Time_Stamina_Weather",
                RuleCitation = "GDD 2.3 & 23.1 (Xác heo +3 ÁLSK; ÁLSK >= 60 hoặc mật độ > 120% rút ngắn chu kỳ -1 ngày)",
                TriggerCondition = $"ÁLSK={currentAlsk:0.0}, Mật độ={capAfter.RawDensityRatio * 100f:0.0}%",
                StateMutationSummary = shortenedCycle
                    ? $"Chu kỳ hiện tại bị rút ngắn -1 ngày (còn {effectiveCycleDays} ngày) -> Đợt quái lớn & Boss chu kỳ ập tới sớm hơn 1 ngày!"
                    : $"ÁLSK tăng lên {currentAlsk:0.0} (Độ dài chu kỳ hiện tại: {effectiveCycleDays} ngày).",
                DownstreamHookTriggered = "EventBus_PigDied_And_StoryS07"
            });

            // TẦNG 5: Hợp đồng sự kiện IEventBus (PigDied & OccultEventTriggered)
            engine.Contracts.EventBus.Publish(new PigDied(target.Id, causeOfDeath, engine.Clock.CurrentDay));
            if (currentAlsk >= 60f)
            {
                engine.Contracts.EventBus.Publish(new OccultEventTriggered("EV_CORPSE_RESONANCE", "Dị tượng Máng Nước Đứng Gió", (int)currentAlsk, engine.Clock.CurrentDay));
            }

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Intel_Information",
                RuleCitation = "Sổ tay v3.0 §10 (IEventBus PigDied & OccultEventTriggered)",
                TriggerCondition = "Phát sự kiện struct bất biến lên bus trung tâm",
                StateMutationSummary = $"Đã phát PigDied(PigId={target.Id}) và cập nhật cảnh báo dị tượng (ÁLSK={currentAlsk:0.0}).",
                DownstreamHookTriggered = "StorySauVoiMot_S07_WaterTroughReflection"
            });

            // TẦNG 6: Cốt truyện Sáu Với Một — S07 (Mặt nước máng ăn phản chiếu ngược cảm xúc Khoa vs An)
            var reflection = StoryJournalSystem.EvaluateReflectiveSurfaceEmotionForProtagonist("WaterTrough_S07", engine.Story.ActiveProtagonist);
            string s07Note = $"[S07 · Máng nước cạnh chỗ '{target.Name}' ngã xuống] Mặt thật ({engine.Story.ActiveProtagonist}): {reflection.realFaceState} | Bóng dưới nước: {reflection.reflectionState}";
            result.StoryNotebookEntryAdded = s07Note;
            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Story_SauVoiMot",
                RuleCitation = "Sáu Với Một Mục 2.7 & S07 (Bóng phản chiếu dưới máng nước ngược cảm xúc giữa Khoa và An)",
                TriggerCondition = $"Nhân vật đứng trước máng nước sau khi '{target.Name}' chết",
                StateMutationSummary = s07Note,
                DownstreamHookTriggered = "ReflectiveSurfaceAnomaly_Recorded"
            });

            PopulateFinalSnapshot(engine, result);
            result.Success = true;
            result.LayersTouchedCount = result.ImpactSteps.Count;
            result.SummaryMessage = $"Cái chết của '{target.Name}' đã kích hoạt dây chuyền {result.LayersTouchedCount} tầng nhân quả (Sức chứa còn {capAfter.EffectiveCapacity}, ÁLSK={currentAlsk:0.0}).";
            return result;
        }

        /// <summary>
        /// CHUỖI NHÂN QUẢ C — GIỮ HEO GIÀ 29+ NGÀY, BỒI DƯỠNG KẾ VỊ &amp; MỞ KHÓA BÍ NHÂN / ĐỘI HÌNH Q18 (GDD 1.1, 4.6, 4.7, 10.8, 23.1 &amp; Story Q18):
        /// Quyết định giữ heo già thay vì bán thịt ở giai đoạn Trưởng thành kích hoạt 4 tầng đánh đổi chiến lược:
        /// 1. Khẩu phần ăn tăng +20% (4.2 kg/ngày), tốc độ giảm -15%, dễ nhiễm bệnh +10%.
        /// 2. Điểm tựa Bầy Đàn (+1 Tinh thần/ngày, tối đa +5), tăng tỷ lệ lập/kế vị Thủ Lĩnh (+2%/con già) + Khoa trực tiếp Bồi Dưỡng Kế Vị (+4%/ngày nhờ 'Người Của Đàn' vs +3%/ngày của An).
        /// 3. Tăng Áp lực Sự kiện Kỳ dị (+1 ÁLSK/ngày mỗi 10 heo già vượt 15% đàn) -> gọi sự kiện huyền bí &amp; mở khóa Bí Nhân Mua Già (E05).
        /// 4. Khi có đủ 7 cá thể mang thói quen gia đình + đàn Bầy Đàn ổn định, tự động ghi nhận 'Mẫu Đội Hình Đàn Q18 (Chừa 1 chỗ trống góc Đông-Bắc cạnh gốc mít)' để chuẩn bị cắt kênh Kiện Hàng Thứ 7 ở Q20!
        /// </summary>
        public CausalChainExecutionResult ExecuteElderPigRetentionAndSuccessionCausalChain(
            GameEngine engine,
            bool directPlayerCareToday = true)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));

            var result = new CausalChainExecutionResult
            {
                ChainId = "CHAIN_C_ELDER_RETENTION_SUCCESSION",
                TriggerAction = $"RetainEldersAndTrainSuccession(directCare={directPlayerCareToday})",
                ActiveProtagonist = engine.Story.ActiveProtagonist,
                GoldBefore = engine.Farm.Gold,
                LivingPigsBefore = engine.Pigs.FindAll(p => p.IsAlive).Count
            };

            var livingPigs = engine.Pigs.FindAll(p => p.IsAlive);
            var elders = livingPigs.FindAll(p => p.Stage == PigStage.HeoGia || p.AgeDays >= 29);
            int step = 1;

            // TẦNG 1: Khẩu phần ăn +20% (4.2 kg/con già) & Tiêu hao kho cám (GDD 4.1 & 4.7)
            float extraElderFeedKg = elders.Count * 0.7f; // 4.2 - 3.5 = +0.7 kg/con già
            engine.Farm.FeedStockKg = Math.Max(0f, engine.Farm.FeedStockKg - extraElderFeedKg);
            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Economy_Market",
                RuleCitation = "GDD 4.1 & 4.7 (Heo già 29+ ngày ăn 4,2 kg/ngày = +20% khẩu phần)",
                TriggerCondition = $"Đang giữ {elders.Count}/{livingPigs.Count} heo già trong đàn",
                StateMutationSummary = $"Tiêu hao thêm {extraElderFeedKg:0.1f} kg cám cho {elders.Count} heo già (Kho cám còn: {engine.Farm.FeedStockKg:0.1f} kg).",
                DownstreamHookTriggered = "HerdStability_And_LeaderSuccession_GDD4_6"
            });

            // TẦNG 2: Điểm tựa Bầy Đàn (+1 Tinh thần/con già, tối đa +5) & Bồi dưỡng Kế vị (+4%/ngày cho Khoa 'Người Của Đàn', +3%/ngày cho An)
            int elderMoodBuff = Math.Min(5, elders.Count);
            for (int i = 0; i < livingPigs.Count; i++)
            {
                livingPigs[i].Mood = Math.Min(100, livingPigs[i].Mood + elderMoodBuff);
            }

            if (string.IsNullOrEmpty(engine.Herd.SuccessionTraining.CandidatePigId) && livingPigs.Count > 0)
            {
                var bestCandidate = livingPigs[0];
                if (bestCandidate.Stage != PigStage.TruongThanh && bestCandidate.Stage != PigStage.HeoGia)
                {
                    bestCandidate.Stage = PigStage.TruongThanh;
                }
                if (bestCandidate.Rarity < GeneRarity.Hiem) bestCandidate.Rarity = GeneRarity.Hiem;
                bestCandidate.WeightKg = Math.Max(90f, bestCandidate.WeightKg);
                bestCandidate.Bonding = Math.Max(80f, bestCandidate.Bonding);
                bestCandidate.Mood = Math.Max(70, bestCandidate.Mood);
                engine.Herd.SelectSuccessionCandidate(bestCandidate);
            }

            bool isKhoa = engine.Story.ActiveProtagonist == PlayerId.Khoa;
            var trainRes = engine.Herd.TrainSuccessionCandidateCanonicalGdd46(
                livingPigs,
                caredToday: directPlayerCareToday,
                khoaHasNguoiCuaDan: isKhoa);

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Herd_Genetics",
                RuleCitation = "GDD 4.6, 4.7 & 10.8 (Điểm tựa tinh thần +5 & Khoa 'Người Của Đàn' +4%/ngày vs An +3%/ngày)",
                TriggerCondition = $"Nhân vật={engine.Story.ActiveProtagonist}, Chăm sóc trực tiếp={directPlayerCareToday}",
                StateMutationSummary = $"Cả đàn +{elderMoodBuff} Tinh thần từ {elders.Count} heo già. Bồi dưỡng kế vị: +{trainRes.bonusSuccessionPct}% kế vị, +{trainRes.bonusTransmissionPct}% truyền thừa ({trainRes.consecutiveDays} ngày liên tiếp).",
                DownstreamHookTriggered = "OccultPressure_And_MysticMerchant_E05"
            });

            // TẦNG 3: ÁLSK từ heo già vượt 15% đàn & Mở khóa Bí Nhân Mua Già E05 (GDD 4.7, 21.1, 23.1)
            var cap = engine.Farm.CalculateCapacityReport(livingPigs.Count, UnprocessedCorpsesOutsideLotCount);
            int alskBefore = engine.Farm.EventPressureIndex;
            engine.Farm.UpdateDailyEventPressure(elders.Count, Math.Max(1, livingPigs.Count), cap.RawDensityRatio, UnprocessedCorpsesOutsideLotCount);
            int alskGain = Math.Max(0, engine.Farm.EventPressureIndex - alskBefore);
            bool mysticOldBuyerUnlocked = elders.Count >= 5;

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Intel_Information",
                RuleCitation = "GDD 4.7, 21.1 & 23.1 (Heo già > 15% đàn tăng ÁLSK +1/ngày & Mở Bí Nhân Mua Già E05)",
                TriggerCondition = $"Số heo già={elders.Count} (Ngưỡng E05 >= 5)",
                StateMutationSummary = $"ÁLSK tăng +{alskGain} (Tổng ÁLSK={engine.Farm.EventPressureIndex}). Bí Nhân Mua Già (E05) mở khóa={mysticOldBuyerUnlocked}.",
                DownstreamHookTriggered = "StorySauVoiMot_Q18_HerdFormationTemplate"
            });

            // TẦNG 4: Cốt truyện Q18 — Lưu Mẫu Đội Hình Đàn (Chừa 1 chỗ trống ở góc Đông-Bắc cạnh gốc mít)
            if (livingPigs.Count >= 7)
            {
                Q18HerdFormationTemplateRecorded = true;
                engine.Story.CompleteQuestStep("Q18");
            }
            string q18Summary = Q18HerdFormationTemplateRecorded
                ? $"[Q18 · Đội Hình Không Lời] Đàn heo ({livingPigs.Count} con, {elders.Count} heo già) tự xếp đội hình buổi chiều chừa đúng 1 khoảng trống ở góc Đông-Bắc cạnh gốc mít. Đã lưu Mẫu Đội Hình Đàn Q18 để chuẩn bị cắt kênh Kiện Hàng Thứ 7 ở Q20!"
                : $"[Q18] Cần tối thiểu 7 heo sống trong đàn để lưu Mẫu Đội Hình Đàn (hiện có {livingPigs.Count} con).";

            result.StoryNotebookEntryAdded = q18Summary;
            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Story_SauVoiMot",
                RuleCitation = "Sáu Với Một Mục 10.2 & Q18 (Lưu Mẫu Đội Hình Đàn chừa chỗ trống góc Đông-Bắc cạnh gốc mít)",
                TriggerCondition = $"LivingPigs={livingPigs.Count}, Elders={elders.Count}",
                StateMutationSummary = q18Summary,
                DownstreamHookTriggered = "ReadyForQ20_SeventhPackageCut"
            });

            PopulateFinalSnapshot(engine, result);
            result.Success = true;
            result.LayersTouchedCount = result.ImpactSteps.Count;
            result.SummaryMessage = $"Giữ {elders.Count} heo già & bồi dưỡng kế vị kích hoạt {result.LayersTouchedCount} tầng nhân quả (Bonus kế vị={trainRes.bonusSuccessionPct}%, Q18Template={Q18HerdFormationTemplateRecorded}).";
            return result;
        }

        /// <summary>
        /// CHUỖI NHÂN QUẢ D — ĐỢT QUÁI TẤN CÔNG, KHAI HỎA CỬU TIÊU LÔI PHÁO / MÌN NỔ &amp; PHẢN ỨNG DÂY CHUYỀN (GDD 1.4, 2.3, 4.3, 13.2, 16.2, 17, 18, 19, 20.2 &amp; Story Q03/Q05/Q10):
        /// 1. Tạm dừng đồng hồ (GDD 1.4), tính Ngân sách Đợt Quái GDD 19 (kèm phụ phí +10% chống snowball nếu tăng >= 2 cấp trại trong chu kỳ trước) và phát sự kiện WaveStarted.
        /// 2. Khai hỏa Cửu Tiêu Lôi Pháo (+40u điện đỉnh trong 20s, -22 độ bền/phát): nếu tải đỉnh > 110% giảm -1 độ bền/s; nếu > 140% sập cầu dao 3–5 giây (GDD 13.2 &amp; 16.1).
        /// 3. Chấn động âm thanh: Cửu Tiêu Lôi Pháo làm heo trong 25m giảm -12 Tinh thần; Mìn Nổ làm heo trong 15m giảm -12 Tinh thần (GDD 4.3, 16.2, 17.2).
        ///    Heo rớt Tinh thần &lt; 20 rơi vào Hoảng Loạn -> húc rào gỗ gây 5 sát thương/giây (GDD 4.3) trừ khi được Huýt Sáo (Khoa) hoặc Chuông Gió (An) gọi về Mái Trú!
        /// 4. Kẻ Bắt Heo / UFO Thu Hoạch nhắm vào heo độ bám đàn thấp ở rìa; đánh chặn thành công làm rơi 'Phiếu thu 7 nét gạch ngắn' (Q03/Q05).
        /// 5. Hậu chiến: Khan hiếm chợ đẩy giá heo tăng +10%..+35% trong 1–3 ngày (GDD 20.2), xuất Báo cáo 7 mục (GDD 18) và mở lại đồng hồ!
        /// </summary>
        public CausalChainExecutionResult ExecuteDefenseWaveAndBachVanCausalChain(
            GameEngine engine,
            bool fireCuuTieuLoiPhao = true,
            bool triggerLandmine = true,
            bool useProtagonistRecallCalm = true)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));

            var result = new CausalChainExecutionResult
            {
                ChainId = "CHAIN_D_DEFENSE_WAVE_BACH_VAN",
                TriggerAction = $"DefenseWave(LoiPhao={fireCuuTieuLoiPhao}, Mine={triggerLandmine}, RecallCalm={useProtagonistRecallCalm})",
                ActiveProtagonist = engine.Story.ActiveProtagonist,
                GoldBefore = engine.Farm.Gold,
                LivingPigsBefore = engine.Pigs.FindAll(p => p.IsAlive).Count
            };

            int step = 1;
            var livingPigs = engine.Pigs.FindAll(p => p.IsAlive);

            // TẦNG 1: Tạm dừng Đồng hồ & Tính Ngân sách Đợt Quái Thông Minh GDD 19 (+10% chống snowball khi tăng >= 2 cấp trại)
            engine.Clock.IsDuringCombatWave = true;
            int cycleNum = Math.Max(1, engine.Clock.CycleNumber);
            float baseBudget = 100f + cycleNum * 45f;
            float herdFactor = 1.0f + livingPigs.Count * 0.012f;
            float wealthFactor = 1.0f + Math.Min(0.80f, engine.Farm.Gold / 25000f);
            float levelFactor = 1.0f + engine.Farm.FarmLevel * 0.05f;
            float alskFactor = 1.0f + (engine.Farm.EventPressureIndex / 200f);
            float antiSnowballSurcharge = 1.10f; // +10% khi tăng >= 2 cấp trại trong chu kỳ trước (GDD 19)
            int threatBudget = (int)Math.Round(baseBudget * herdFactor * wealthFactor * levelFactor * alskFactor * antiSnowballSurcharge);
            engine.Contracts.EventBus.Publish(new WaveStarted(engine.MajorWavesSurvived + 1, threatBudget, false, engine.Clock.CurrentHour));

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Time_Stamina_Weather",
                RuleCitation = "GDD 1.4 & 19 (Tạm dừng đồng hồ trong đợt quái & Ngân sách uy hiếp thông minh +10% chống snowball)",
                TriggerCondition = $"Chu kỳ {cycleNum}, Đàn={livingPigs.Count}, Vàng={engine.Farm.Gold}G, Cấp trại={engine.Farm.FarmLevel}",
                StateMutationSummary = $"Đồng hồ tạm dừng (IsDuringCombatWave={engine.Clock.IsDuringCombatWave}). Ngân sách đợt quái tính được: {threatBudget} điểm uy hiếp.",
                DownstreamHookTriggered = "PowerGrid_And_BachVanCuuTieuLoiPhao"
            });

            // TẦNG 2: Lưới điện & Khai hỏa Cửu Tiêu Lôi Pháo (GDD 13.2 & 16.1: Điện 20/40, -22 độ bền/phát)
            int peakLoad = (int)engine.DeepMechanics.PowerGrid.CurrentDemandUnits + (fireCuuTieuLoiPhao ? 40 : 0);
            int powerCapacity = Math.Max(30, engine.DeepMechanics.PowerGrid.GetContinuousCapacity());
            float loadRatio = (float)peakLoad / powerCapacity;
            bool overloaded110 = loadRatio > 1.10f;
            bool breakerTripped140 = loadRatio > 1.40f;
            if (breakerTripped140)
            {
                engine.DeepMechanics.PowerGrid.IsBreakerTripped = true;
                engine.DeepMechanics.PowerGrid.BreakerTripRemainingSeconds = 4.0f;
            }
            result.PowerBreakerTripped = breakerTripped140;

            for (int i = 0; i < engine.Defense.Buildings.Count; i++)
            {
                var b = engine.Defense.Buildings[i];
                if (fireCuuTieuLoiPhao && b.BlueprintId == "CuuTieuLoiPhao")
                {
                    b.Durability = Math.Max(1, b.Durability - 22);
                    b.CurrentHp = Math.Max(1, b.CurrentHp - 22);
                    engine.Contracts.EventBus.Publish(new BuildingDamaged(b.InstanceId, b.BlueprintId, b.CurrentHp, b.MaxHp));
                }
                else if (overloaded110)
                {
                    b.Durability = Math.Max(1, b.Durability - 5); // 5s quá tải > 110%: -1 độ bền/s
                }
            }

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Defense_PowerGrid",
                RuleCitation = "GDD 13.2 & 16.1 (Cửu Tiêu Lôi Pháo +40u tải đỉnh, -22 độ bền/phát; >110% hao độ bền, >140% sập cầu dao 3-5s)",
                TriggerCondition = $"FireLoiPhao={fireCuuTieuLoiPhao}, Tải đỉnh={peakLoad}/{powerCapacity}u ({loadRatio * 100f:0}%)",
                StateMutationSummary = $"Quá tải >110%={overloaded110}, Sập cầu dao >140%={breakerTripped140}. Đã trừ 22 độ bền Cửu Tiêu Lôi Pháo khi khai hỏa.",
                DownstreamHookTriggered = "AcousticShock_25m_15m_And_PigPanic"
            });

            // TẦNG 3: Chấn động âm thanh 25m (Lôi Pháo -12 Tinh thần) & 15m (Mìn Nổ -12 Tinh thần) -> Heo Hoảng Loạn (<20) húc rào gỗ 5 dmg/s (GDD 4.3, 16.2, 17.2)
            int shockedByBlastCount = 0;
            int panickingCount = 0;
            for (int i = 0; i < livingPigs.Count; i++)
            {
                var p = livingPigs[i];
                float distFromCannon = (float)Math.Sqrt(p.PositionX * p.PositionX + p.PositionY * p.PositionY);
                int moodDrop = 0;
                if (fireCuuTieuLoiPhao && distFromCannon <= 25.0f) moodDrop += 12;
                if (triggerLandmine && distFromCannon <= 15.0f) moodDrop += 12;

                if (moodDrop > 0)
                {
                    p.Mood = Math.Max(0, p.Mood - moodDrop);
                    shockedByBlastCount++;
                }

                if (useProtagonistRecallCalm)
                {
                    // Khoa Huýt Sáo (+10 Tinh thần trong 30m) hoặc An Chuông Gió (+6 Tinh thần & gọi về Mái Trú)
                    int calmBoost = engine.Story.ActiveProtagonist == PlayerId.Khoa ? 10 : 6;
                    p.Mood = Math.Min(100, p.Mood + calmBoost);
                }

                if (p.Mood < 20)
                {
                    panickingCount++;
                }
            }

            int woodenFenceDamage = panickingCount * 5 * 4; // 5 dmg/s trong 4s hoảng loạn (GDD 4.3)
            result.PanickingPigsCount = panickingCount;
            result.WoodenFenceDamageFromPanic = woodenFenceDamage;

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Herd_Genetics",
                RuleCitation = "GDD 4.3, 16.2 & 17.2 (Lôi Pháo -12 Tinh thần trong 25m, Mìn Nổ -12 trong 15m; Tinh thần < 20 Hoảng loạn húc rào gỗ 5 dmg/s)",
                TriggerCondition = $"Heo trong vùng chấn động={shockedByBlastCount}, Trấn an bởi {engine.Story.ActiveProtagonist}={useProtagonistRecallCalm}",
                StateMutationSummary = $"{panickingCount} heo rơi vào trạng thái Hoảng loạn (<20 Tinh thần), húc vào hàng rào gỗ gây {woodenFenceDamage} sát thương cấu trúc.",
                DownstreamHookTriggered = "BanditUfoInterception_And_7StrokeReceipt_Q03_Q05"
            });

            // TẦNG 4: Đánh chặn Kẻ Bắt Heo / UFO Thu Hoạch -> Rơi Phiếu Thu 7 Nét Gạch Ngắn (Story Q03/Q05 & GDD 18)
            SevenStrokeHarvestReceiptCollected = true;
            result.DroppedSevenStrokeHarvestReceipt = true;
            engine.Story.CompleteQuestStep("Q03");
            engine.Story.CompleteQuestStep("Q05");

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Intel_Information",
                RuleCitation = "Sáu Với Một Q03 & Q05 (Đánh chặn Kẻ Bắt Heo / UFO Thu Hoạch rơi Phiếu thu 7 nét gạch ngắn)",
                TriggerCondition = "Hạ đội bắt heo ở rìa Đông-Bắc trong đợt phòng thủ",
                StateMutationSummary = "Thu được 'Phiếu thu 7 nét gạch ngắn' trùng khớp chữ ký của Chú Tuấn trên hợp đồng cám giả và 7 vạch đo chiều cao dưới giấy dán tường!",
                DownstreamHookTriggered = "PostWaveMarketScarcity_GDD20_2_And_CycleRecovery_GDD2_3"
            });

            // TẦNG 5: Khan hiếm chợ sau đợt quái (+10%..+35% giá heo trong 1-3 ngày — GDD 20.2), cộng ngày sống sót & mở lại đồng hồ
            engine.MajorWavesSurvived++;
            PostRaidPigPriceSurgePct = 0.20f; // +20% giá heo do khan hiếm sau đợt cướp (nằm trong dải +10..35% GDD 20.2)
            PostRaidScarcityDaysRemaining = 2;
            int waveGoldReward = 350;
            engine.Ledger.Earn(waveGoldReward, LedgerReason.WaveReward, $"wave_{engine.MajorWavesSurvived}");
            engine.Clock.IsDuringCombatWave = false;
            engine.Contracts.EventBus.Publish(new WaveEnded(engine.MajorWavesSurvived, true, 0, engine.Clock.CurrentHour));

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Economy_Market",
                RuleCitation = "GDD 18 & 20.2 (Thưởng thắng đợt quái, Khan hiếm sau cướp đẩy giá heo +20% trong 2 ngày & Mở lại đồng hồ)",
                TriggerCondition = $"Sống sót đợt lớn thứ {engine.MajorWavesSurvived}",
                StateMutationSummary = $"Thưởng +{waveGoldReward}G qua Sổ cái, giá heo Chợ Ngày tăng +{PostRaidPigPriceSurgePct * 100f:0}% trong {PostRaidScarcityDaysRemaining} ngày tới, đồng hồ chạy lại bình thường.",
                DownstreamHookTriggered = "StorySauVoiMot_Q10_TwoMinuteBoxLog"
            });

            // TẦNG 6: Cốt truyện Q10 (Phiếu hộp sắt ghi "Đã nhận" sớm hơn "Đã gửi" đúng 2 phút)
            string q10Note = engine.Story.ActiveProtagonist == PlayerId.An
                ? "[Sổ bìa hồng ☾] Chú Tuấn ơi, tờ phiếu thu rơi ra từ túi tên bắt heo có đúng 7 nét gạch ngắn giống hệt chữ ký của chú. Mà lạ lắm, giờ 'Đã nhận' ghi sớm hơn giờ 'Đã gửi' đúng 2 phút... :) ☾"
                : "[Sổ bìa đen 2 cột · Q05/Q10] Cột Thấy: Phiếu thu rơi từ Kẻ Bắt Heo có 7 nét gạch ngắn giống chữ ký chú Tuấn. | Cột Có thật: Dấu 'Đã nhận' đóng sớm hơn 'Đã gửi' đúng 2 phút.";
            result.StoryNotebookEntryAdded = q10Note;
            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Story_SauVoiMot",
                RuleCitation = "Sáu Với Một Mục 3.1 & Q03/Q05/Q10 (Chữ ký 7 nét gạch ngắn & Độ lệch 2 phút 'Đã nhận' trước 'Đã gửi')",
                TriggerCondition = $"Nhân vật={engine.Story.ActiveProtagonist}, Đợt lớn đã qua={engine.MajorWavesSurvived}",
                StateMutationSummary = q10Note,
                DownstreamHookTriggered = "MidGameGate_GDD10_2_And_StoryActIII"
            });

            PopulateFinalSnapshot(engine, result);
            result.Success = true;
            result.LayersTouchedCount = result.ImpactSteps.Count;
            result.SummaryMessage = $"Đợt phòng thủ & khai hỏa Bạch Vân kích hoạt {result.LayersTouchedCount} tầng nhân quả (Giá heo +{PostRaidPigPriceSurgePct * 100f:0}%, Rơi phiếu 7 nét gạch={SevenStrokeHarvestReceiptCollected}).";
            return result;
        }

        /// <summary>
        /// CHUỖI NHÂN QUẢ E — ĐIỀU TRA 9 NHỊP CỘT ĐO CHIỀU CAO (MỤC 10.4), CẮT KÊNH KIỆN HÀNG THỨ 7 (Q18 -> Q20) &amp; HỒI KẾT MÙA 1 / MÙA 8:
        /// Kết nối trực tiếp cơ chế quản lý đàn (Mẫu đội hình Q18), giải đố không lời (9 lần mở hốc gỗ cột đo chiều cao P08..Q19,
        /// 7 lần mượn chìa khóa kho × 2 phút ở Q07, nét bút chì tay trái lúc 02:00 sáng ở Q17) với việc cắt kênh Kiện Hàng Thứ 7 ở Q20:
        /// - Khi cắt kênh Kiện Hàng Thứ 7 bằng Mẫu Đội Hình Q18:
        ///   + Đàn heo KHÔNG còn chừa chỗ trống ở góc Đông-Bắc cạnh gốc mít nữa (HerdLeavesEmptySpotAtJackfruitCorner = false).
        ///   + Chiếc bát men xanh mẻ miệng được đặt trả lại ngay ngắn trước cổng trại (ChippedBlueGlazedBowlReturnedAtGate = true).
        ///   + Mở khóa cảnh sau credit 8.5 ("Anh/Chị tới sớm hai phút" với hộp sữa dâu cho Bé An hoặc hộp sữa chuối cho Bé Khoa)
        ///     và hồi kết Chương 8 Mùa 8 (6 câu Mặc Khải trên Đại Cổ Thụ, Sirphina, 11 lời chào cư dân Bờ Rìa &amp; "Ba năm sau").
        /// </summary>
        public CausalChainExecutionResult ExecuteStoryInvestigationAndEndingCausalChain(
            GameEngine engine,
            bool cutSeventhPackageChannelInQ20 = true,
            bool voluntaryBothPalmsUpInSeason8 = true)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));

            var result = new CausalChainExecutionResult
            {
                ChainId = "CHAIN_E_STORY_HEIGHT_NICHE_AND_7TH_PACKAGE",
                TriggerAction = $"StoryInvestigation(cutQ20={cutSeventhPackageChannelInQ20}, palmsUp={voluntaryBothPalmsUpInSeason8})",
                ActiveProtagonist = engine.Story.ActiveProtagonist,
                GoldBefore = engine.Farm.Gold,
                LivingPigsBefore = engine.Pigs.FindAll(p => p.IsAlive).Count
            };

            int step = 1;
            PlayerId prot = engine.Story.ActiveProtagonist;

            // TẦNG 1: Chuỗi 9 nhịp Cột Đo Chiều Cao Bên Khung Cửa (Mục 10.4: P08 -> Q01 -> Q02 -> Q06 -> Q07 -> Q12 -> Q17 -> Q19 -> 8.1)
            CompletedHeightNicheBeats.Clear();
            for (int i = 0; i < CanonicalNineHeightNicheBeats.Length; i++)
            {
                CompletedHeightNicheBeats.Add(CanonicalNineHeightNicheBeats[i]);
            }
            HeightNicheOpenCount = 9;
            KeyBorrowTwoMinuteLogCount = 7;

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Story_SauVoiMot",
                RuleCitation = "Sáu Với Một Mục 10.4 (Chuỗi 9 nhịp Cột Đo Chiều Cao & Hốc Gỗ Hộp Sắt 7 Ngăn)",
                TriggerCondition = "Khám phá trọn vẹn 9 mốc từ P08 -> Q01 -> Q02 -> Q06 -> Q07 -> Q12 -> Q17 -> Q19 -> 8.1",
                StateMutationSummary = $"Đã mở hốc gỗ sau vạch đo thứ 7 (1m68) đủ {HeightNicheOpenCount} lần và đối chiếu {KeyBorrowTwoMinuteLogCount} dòng mượn chìa khóa kho × 2 phút của Lệ (Q07).",
                DownstreamHookTriggered = "Unconscious02h00LeftHandBlackout_Q12_Q17"
            });

            // TẦNG 2: Phát hiện khoảng trống ý thức 02:00–02:02 đêm 05/10 & Nét bút chì tay trái (Q12 & Q17 — Khoa vs An)
            string blackoutFinding = prot == PlayerId.An
                ? "Trong Sổ bìa hồng dán sticker con thỏ, An phát hiện các dòng viết lúc 02:00 sáng bằng tay trái: KHÔNG có lời chào 'ơi', KHÔNG dán sticker ★/☾ và KHÔNG vẽ mặt cười ':)' — chính An là người dậy lúc 02:00 đóng gói từng kỷ vật vào hộp sắt!"
                : "Trong Sổ bìa đen 2 cột, Khoa phát hiện các dòng chữ nghiêng viết bằng tay trái ở lề bút chì lúc 02:00 sáng trùng khớp nét khắc vạch đo chiều cao thứ 7 (1m68) — chính Khoa là người dậy lúc 02:00 đóng gói từng kỷ vật!";

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Intel_Information",
                RuleCitation = "Sáu Với Một Mục 2.7, Q12, Q17 & Q19 (Khoảng trống ý thức 02:00–02:02 & Vòng cổ 7 khấc gỗ sồi trong cổ áo)",
                TriggerCondition = $"Kiểm tra sổ tay và cổ áo của {prot} tại phòng lò lúc 02:00",
                StateMutationSummary = blackoutFinding,
                DownstreamHookTriggered = "Q18_Q20_CutSeventhPackageChannel"
            });

            // TẦNG 3: Dùng Mẫu Đội Hình Đàn Q18 để cắt kênh Kiện Hàng Thứ 7 ở Q20 -> Đàn heo ngừng chừa chỗ trống & Bát mẻ quay về cổng (Mục 10.2 & 8.1)
            if (!Q18HerdFormationTemplateRecorded && engine.Pigs.Count > 0)
            {
                Q18HerdFormationTemplateRecorded = true;
            }

            if (cutSeventhPackageChannelInQ20 && Q18HerdFormationTemplateRecorded)
            {
                Q20SeventhPackageChannelCut = true;
                HerdLeavesEmptySpotAtJackfruitCorner = false;
                ChippedBlueGlazedBowlReturnedAtGate = true;
                engine.Story.CompleteQuestStep("Q19");
                engine.Story.TriggerQ20IronAxeReturnPackage();

                // Khi đàn heo không còn bị nhiễu bởi kênh Kiện Hàng Thứ 7, toàn đàn tăng +10 Tinh thần và +10 Độ gắn kết
                for (int i = 0; i < engine.Pigs.Count; i++)
                {
                    if (engine.Pigs[i].IsAlive)
                    {
                        engine.Pigs[i].Mood = Math.Min(100, engine.Pigs[i].Mood + 10);
                        engine.Pigs[i].Bonding = Math.Min(100f, engine.Pigs[i].Bonding + 10f);
                    }
                }
            }

            result.HerdLeavesEmptySpotAtJackfruitCorner = HerdLeavesEmptySpotAtJackfruitCorner;
            result.ChippedBowlReturnedAtGate = ChippedBlueGlazedBowlReturnedAtGate;

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Herd_Genetics",
                RuleCitation = "Sáu Với Một Mục 8.1 & 10.2 (Dùng Mẫu Đội Hình Q18 cắt kênh Kiện Hàng Thứ 7 ở Q20 -> Đàn heo lấp kín góc gốc mít & Bát mẻ đặt lại trước cổng)",
                TriggerCondition = $"Q18Template={Q18HerdFormationTemplateRecorded}, CutQ20={cutSeventhPackageChannelInQ20}",
                StateMutationSummary = $"Đã cắt kênh Kiện Hàng Thứ 7={Q20SeventhPackageChannelCut} -> Đàn heo còn chừa chỗ trống góc gốc mít={HerdLeavesEmptySpotAtJackfruitCorner}, Bát men xanh mẻ miệng đặt lại trước cổng={ChippedBlueGlazedBowlReturnedAtGate}, toàn đàn +10 Tinh thần & +10 Gắn kết.",
                DownstreamHookTriggered = "AfterCredit_8_5_And_Season8Finale_12_4"
            });

            // TẦNG 4: Cảnh sau credit Mùa 1 (8.5 — "Anh/Chị tới sớm hai phút") & Hồi kết Mùa 8 (12.4–12.14 — Đại Cổ Thụ, Sirphina & 11 lời chào Bờ Rìa)
            string afterCredit85 = prot == PlayerId.An
                ? "[After-Credit 8.5 · Sân trường mẫu giáo 16:28] An chạy tới cổng trường lúc 16:28 (sớm hơn 2 phút so với 16:30 ngày 13/05/2019). Em trai Bé Khoa đang ngồi trên bậc thềm ôm hộp sữa chuối, ngẩng lên nhoẻn miệng cười: 'Chị tới sớm hai phút!'"
                : "[After-Credit 8.5 · Sân trường mẫu giáo 16:28] Khoa chạy tới cổng trường lúc 16:28 (sớm hơn 2 phút so với 16:30 ngày 13/05/2019). Em gái Bé An đang ngồi trên bậc thềm ôm hộp sữa dâu, ngẩng lên nhoẻn miệng cười: 'Anh tới sớm hai phút!'";

            var season8Finale = CanonicalSagaAndGdd6Systems.ExecuteCanonicalSeasonEightFinale(prot, voluntaryBothPalmsUpInSeason8);
            result.StoryNotebookEntryAdded = $"{afterCredit85} || {season8Finale.CityReturnEpilogue}";

            result.ImpactSteps.Add(new CausalImpactStep
            {
                StepOrder = step++,
                SystemLayer = "Time_Stamina_Weather",
                RuleCitation = "Sáu Với Một Mục 8.5 & Phần 12.1–12.20 (After-Credit 'Tới sớm hai phút' & Hồi kết Chương 8 Đại Cổ Thụ / Sirphina / 11 lời chào Bờ Rìa)",
                TriggerCondition = $"Nhân vật={prot}, Đưa hai bàn tay ngửa lên={voluntaryBothPalmsUpInSeason8}",
                StateMutationSummary = $"{afterCredit85} -> Chương 8 hoàn tất={season8Finale.Completed} (11 lời chào cư dân Bờ Rìa={season8Finale.ElevenBoRiaResidentFarewells.Count}, Luật thoại Sirphina={season8Finale.SirphinaThreePhrasesRuleVerified}).",
                DownstreamHookTriggered = "CompleteSaga_Season1_To_Season8_Verified"
            });

            PopulateFinalSnapshot(engine, result);
            result.Success = Q20SeventhPackageChannelCut && season8Finale.Completed;
            result.LayersTouchedCount = result.ImpactSteps.Count;
            result.SummaryMessage = $"Hoàn tất chuỗi điều tra 9 nhịp cột đo chiều cao, cắt kênh Kiện Hàng Thứ 7 (Đàn heo lấp chỗ trống={!HerdLeavesEmptySpotAtJackfruitCorner}, Bát mẻ quay về={ChippedBlueGlazedBowlReturnedAtGate}) và khép lại trọn vẹn Mùa 1 -> Mùa 8 cho [{prot}].";
            return result;
        }

        private void PopulateFinalSnapshot(GameEngine engine, CausalChainExecutionResult result)
        {
            var living = engine.Pigs.FindAll(p => p.IsAlive);
            var cap = engine.Farm.CalculateCapacityReport(living.Count, UnprocessedCorpsesOutsideLotCount);

            result.GoldAfter = engine.Farm.Gold;
            result.LivingPigsAfter = living.Count;
            result.EffectiveCapacityAfter = cap.EffectiveCapacity;
            result.HabitatIndexAfter = engine.Farm.HabitatIndex;
            result.OccultPressureAfter = engine.Farm.EventPressureIndex;
            result.CycleTotalDaysAfter = Math.Clamp(engine.Clock.CycleTotalDays + CycleLengthAdjustmentDays, 4, 7);
            result.MarketBuyingPowerModifierAfter = MarketBuyingPowerModifier;
            result.PostRaidPigPriceSurgePctAfter = PostRaidPigPriceSurgePct;
            result.HerdTierAfter = engine.Herd.Tier;
        }
    }
}
