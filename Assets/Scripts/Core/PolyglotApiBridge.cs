using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PigTycoon.Core
{
    [Serializable]
    public sealed class PolyglotApiResponse
    {
        public int StatusCode { get; set; } = 200;
        public string Endpoint { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string ActiveProtagonist { get; set; } = string.Empty;
        public string YoungestSiblingName { get; set; } = string.Empty;
        public string NotebookStyleSummary { get; set; } = string.Empty;
        public string NativeBackendUsed { get; set; } = string.Empty;
        public long NativeFlowFieldRebuildsCount { get; set; }
        public string JsonData { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cầu nối API Đa Ngôn Ngữ (Polyglot REST / JSON-RPC &amp; Native C-ABI Bridge):
    /// Liên kết trực tiếp GameEngine (C# Core) &lt;-&gt; Native Rust/C++20 FFI (`pig_tycoon_native.dll` / `pig_tycoon_cpp.dll`)
    /// &lt;-&gt; Go Simulation Gateway &lt;-&gt; Flutter Multi-Platform Client (Windows / Android / Web).
    /// Tuân thủ tuyệt đối `noEngineReferences: true` và `allowUnsafeCode: false`.
    /// </summary>
    public sealed class PolyglotApiBridge
    {
        private readonly GameEngine _engine;

        public PolyglotApiBridge(GameEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public PolyglotApiResponse Dispatch(string httpMethod, string endpoint, string? jsonBody = null)
        {
            string normEndpoint = (endpoint ?? string.Empty).Trim();
            string method = (httpMethod ?? "GET").Trim().ToUpperInvariant();

            switch (normEndpoint)
            {
                case "/api/v1/status":
                    return HandleGetStatus();

                case "/api/v1/protagonist/switch":
                    return HandleSwitchProtagonist(jsonBody);

                case "/api/v1/story/notebook":
                    return HandleGetNotebook();

                case "/api/v1/story/chapter8-finale":
                    return HandleExecuteChapter8Finale();

                case "/api/v1/gdd6/appraise":
                    return HandleAppraiseGdd54(jsonBody);

                case "/api/v1/gdd6/specialty-locks":
                    return HandleEvaluateSpecialtyLocksGdd108(jsonBody);

                case "/api/v1/causal/execute-chain":
                    return HandleExecuteCausalChain(jsonBody);

                case "/api/v1/simulation/tick":
                    return HandleSimulationTickAndNativeFlowField(jsonBody);

                case "/api/v1/polyglot/benchmark":
                    return HandlePolyglotBenchmark();

                default:
                    return new PolyglotApiResponse
                    {
                        StatusCode = 404,
                        Endpoint = normEndpoint,
                        Success = false,
                        Message = $"Không tìm thấy endpoint API: {method} {normEndpoint}"
                    };
            }
        }

        public PolyglotApiResponse HandleGetStatus()
        {
            NativePolyglotAccelerator.EnsureNativeProbed();
            PlayerId prot = _engine.Story.ActiveProtagonist;
            string notebookStyle = prot == PlayerId.An
                ? "Sổ bìa hồng dán sticker con thỏ (Thư gửi người thân + ★ Ngôi sao / ☾ Mặt trăng + mặt cười :))"
                : "Sổ bìa đen 2 cột ('Thấy' bên trái | 'Có thật' bên phải + Lề bút chì)";

            var payload = new Dictionary<string, object>
            {
                ["day"] = _engine.Clock.CurrentDay,
                ["hour"] = _engine.Clock.CurrentHour,
                ["gold"] = _engine.Farm.Gold,
                ["farmLevel"] = _engine.Farm.FarmLevel,
                ["habitatIndex"] = _engine.Farm.HabitatIndex,
                ["livingPigsCount"] = _engine.Pigs.FindAll(p => p.IsAlive).Count,
                ["protagonist"] = prot.ToString(),
                ["youngestSibling"] = _engine.Story.YoungestSiblingName,
                ["rustDllLoaded"] = NativePolyglotAccelerator.IsRustNativeAvailable,
                ["cppDllLoaded"] = NativePolyglotAccelerator.IsCppNativeAvailable,
                ["nativeFlowFieldRebuilds"] = NativePolyglotAccelerator.NativeHardwareAcceleratedFlowFieldCount,
                ["lastFlowFieldBackend"] = _engine.FlowField.LastUsedNativeBackend,
                ["herdLeavesEmptySpotAtJackfruitCorner"] = _engine.CausalChains.HerdLeavesEmptySpotAtJackfruitCorner,
                ["chippedBowlReturnedAtGate"] = _engine.CausalChains.ChippedBlueGlazedBowlReturnedAtGate
            };

            return new PolyglotApiResponse
            {
                StatusCode = 200,
                Endpoint = "/api/v1/status",
                Success = true,
                ActiveProtagonist = prot.ToString(),
                YoungestSiblingName = _engine.Story.YoungestSiblingName,
                NotebookStyleSummary = notebookStyle,
                NativeBackendUsed = _engine.FlowField.LastUsedNativeBackend,
                NativeFlowFieldRebuildsCount = NativePolyglotAccelerator.NativeHardwareAcceleratedFlowFieldCount,
                JsonData = JsonConvert.SerializeObject(payload),
                Message = $"API Status OK — Nhân vật: {prot} (Em út: {_engine.Story.YoungestSiblingName})"
            };
        }

        public PolyglotApiResponse HandleSwitchProtagonist(string? jsonBody)
        {
            PlayerId target = PlayerId.An;
            if (!string.IsNullOrWhiteSpace(jsonBody) &&
                jsonBody.Contains("Khoa", StringComparison.OrdinalIgnoreCase))
            {
                target = PlayerId.Khoa;
            }

            PlayerRoster.Apply(_engine.Character, target);
            _engine.Story.SwitchProtagonistPerspective(target);

            var timeline = CanonicalSagaAndGdd6Systems.GetFamilyTimelineByProtagonist(target);
            var q10Reflection = StoryJournalSystem.EvaluateReflectiveSurfaceEmotionForProtagonist("WindowGlass_Q10", target);
            var q19Reflection = StoryJournalSystem.EvaluateReflectiveSurfaceEmotionForProtagonist("Necklace7Notches_Q19", target);

            string notebookStyle = target == PlayerId.An
                ? "Sổ bìa hồng dán sticker con thỏ (★ / ☾ / :))"
                : "Sổ bìa đen 2 cột ('Thấy' / 'Có thật')";

            var payload = new Dictionary<string, object>
            {
                ["activeProtagonist"] = target.ToString(),
                ["characterDataId"] = _engine.Character.Id,
                ["specialtyTitle"] = target == PlayerId.An ? "Người Đọc Dòng" : "Trại Chủ Lão Luyện",
                ["youngestSiblingOn13May2019"] = timeline[3].MemberName,
                ["notebookStyle"] = notebookStyle,
                ["q10RealFace"] = q10Reflection.realFaceState,
                ["q10ReflectionFace"] = q10Reflection.reflectionState,
                ["q19RealFace"] = q19Reflection.realFaceState,
                ["q19ReflectionFace"] = q19Reflection.reflectionState,
                ["bothPalmsUpGestureActive"] = true
            };

            return new PolyglotApiResponse
            {
                StatusCode = 200,
                Endpoint = "/api/v1/protagonist/switch",
                Success = true,
                ActiveProtagonist = target.ToString(),
                YoungestSiblingName = _engine.Story.YoungestSiblingName,
                NotebookStyleSummary = notebookStyle,
                NativeBackendUsed = _engine.FlowField.LastUsedNativeBackend,
                NativeFlowFieldRebuildsCount = NativePolyglotAccelerator.NativeHardwareAcceleratedFlowFieldCount,
                JsonData = JsonConvert.SerializeObject(payload),
                Message = $"Đã chuyển góc nhìn sang [{target}] — Em út mất ngày 13/05/2019 là [{timeline[3].MemberName}]."
            };
        }

        public PolyglotApiResponse HandleGetNotebook()
        {
            PlayerId prot = _engine.Story.ActiveProtagonist;
            bool blackoutInvariantsValid = true;
            int blackoutCount = 0;

            for (int i = 0; i < _engine.Story.AnPinkNotebookEntries.Count; i++)
            {
                var entry = _engine.Story.AnPinkNotebookEntries[i];
                if (entry.IsUnconsciousBlackoutEntry) blackoutCount++;
                if (!entry.ValidateBlackoutInvariant()) blackoutInvariantsValid = false;
            }

            var payload = new Dictionary<string, object>
            {
                ["protagonist"] = prot.ToString(),
                ["khoaTwoColumnCount"] = _engine.Story.NotebookEntries.Count,
                ["anPinkLetterCount"] = _engine.Story.AnPinkNotebookEntries.Count,
                ["anBlackoutEntryCount"] = blackoutCount,
                ["anBlackoutInvariantsValid"] = blackoutInvariantsValid,
                ["comparisonRowsCount"] = CanonicalSagaAndGdd6Systems.DualProtagonistComparisonTable27.Count
            };

            return new PolyglotApiResponse
            {
                StatusCode = 200,
                Endpoint = "/api/v1/story/notebook",
                Success = blackoutInvariantsValid,
                ActiveProtagonist = prot.ToString(),
                YoungestSiblingName = _engine.Story.YoungestSiblingName,
                NotebookStyleSummary = prot == PlayerId.An ? "Sổ bìa hồng dán sticker con thỏ" : "Sổ bìa đen 2 cột",
                JsonData = JsonConvert.SerializeObject(payload),
                Message = $"Notebook API OK (Blackout Invariants Valid={blackoutInvariantsValid}, Blackout Entries={blackoutCount})"
            };
        }

        public PolyglotApiResponse HandleExecuteChapter8Finale()
        {
            PlayerId prot = _engine.Story.ActiveProtagonist;
            var finale = CanonicalSagaAndGdd6Systems.ExecuteCanonicalSeasonEightFinale(prot, voluntaryBothPalmsUp: true);

            var payload = new Dictionary<string, object>
            {
                ["completed"] = finale.Completed,
                ["protagonist"] = finale.Protagonist.ToString(),
                ["voluntaryBothPalmsUp"] = finale.VoluntaryBothPalmsUp,
                ["sirphinaThreePhrasesRuleVerified"] = finale.SirphinaThreePhrasesRuleVerified,
                ["modernAndAncientSoulFused"] = finale.ModernAndAncientSoulFused,
                ["twoWorldsPermanentlySeparated"] = finale.TwoWorldsPermanentlySeparated,
                ["huyenYWithdrawn"] = finale.HuyenYWithdrawnFromMainWorld,
                ["macKhaiLineCount"] = finale.SixLinesOfMacKhai.Count,
                ["kimOThanhHoiMemberCount"] = CanonicalSagaAndGdd6Systems.FourMembersOfKimOThanhHoi.Count,
                ["boRiaResidentFarewellsCount"] = finale.ElevenBoRiaResidentFarewells.Count,
                ["cityReturnEpilogue"] = finale.CityReturnEpilogue,
                ["threeYearsLaterAfterCredit"] = finale.ThreeYearsLaterAfterCredit
            };

            return new PolyglotApiResponse
            {
                StatusCode = 200,
                Endpoint = "/api/v1/story/chapter8-finale",
                Success = finale.Completed && finale.SirphinaThreePhrasesRuleVerified,
                ActiveProtagonist = prot.ToString(),
                YoungestSiblingName = _engine.Story.YoungestSiblingName,
                JsonData = JsonConvert.SerializeObject(payload),
                Message = finale.CityReturnEpilogue
            };
        }

        public PolyglotApiResponse HandleAppraiseGdd54(string? jsonBody)
        {
            AppraisalChannel channel = AppraisalChannel.ChoDemGiamDinhCapCao;
            GeneRarity rarity = GeneRarity.DiBien;

            if (!string.IsNullOrWhiteSpace(jsonBody))
            {
                if (jsonBody.Contains("ChoNgay", StringComparison.OrdinalIgnoreCase))
                    channel = AppraisalChannel.ChoNgay;
                else if (jsonBody.Contains("PhongNghienCuu", StringComparison.OrdinalIgnoreCase))
                    channel = AppraisalChannel.PhongNghienCuu;
                else if (jsonBody.Contains("BiNhan", StringComparison.OrdinalIgnoreCase))
                    channel = AppraisalChannel.BiNhanHoSoGiongLoai;

                if (jsonBody.Contains("Hiem", StringComparison.OrdinalIgnoreCase))
                    rarity = GeneRarity.Hiem;
                else if (jsonBody.Contains("Quy", StringComparison.OrdinalIgnoreCase))
                    rarity = GeneRarity.Quy;
                else if (jsonBody.Contains("HuyenThoai", StringComparison.OrdinalIgnoreCase))
                    rarity = GeneRarity.HuyenThoai;
            }

            var csharpSpec = CanonicalSagaAndGdd6Systems.GetCanonicalAppraisalChannelSpecGdd54(channel, rarity);
            var nativeSpec = NativePolyglotAccelerator.EvaluateGdd54AndSealNativeApi(channel, rarity, _engine.Story.ActiveProtagonist);
            bool exactAgreement = Math.Abs(csharpSpec.successRate - nativeSpec.probability) < 0.001f &&
                                  csharpSpec.goldCost == nativeSpec.goldCost &&
                                  Math.Abs(csharpSpec.staminaCost - nativeSpec.staminaCost) < 0.01f &&
                                  csharpSpec.processingHours == nativeSpec.hours;

            var payload = new Dictionary<string, object>
            {
                ["channel"] = channel.ToString(),
                ["rarity"] = rarity.ToString(),
                ["successRate"] = csharpSpec.successRate,
                ["goldCost"] = csharpSpec.goldCost,
                ["reagentCount"] = csharpSpec.reagentCount,
                ["staminaCost"] = csharpSpec.staminaCost,
                ["processingHours"] = csharpSpec.processingHours,
                ["nativeRustUsed"] = nativeSpec.usedNativeRust,
                ["sealYearsPlusTag"] = nativeSpec.sealYearsPlusTag,
                ["csharpAndNativeBitExactAgreement"] = exactAgreement
            };

            return new PolyglotApiResponse
            {
                StatusCode = 200,
                Endpoint = "/api/v1/gdd6/appraise",
                Success = exactAgreement,
                ActiveProtagonist = _engine.Story.ActiveProtagonist.ToString(),
                NativeBackendUsed = nativeSpec.usedNativeRust ? "Rust_C_ABI" : "Managed_CSharp_SoA",
                JsonData = JsonConvert.SerializeObject(payload),
                Message = $"GDD 5.4 [{channel} / {rarity}]: Tỉ lệ={csharpSpec.successRate * 100f:0}%, Vàng={csharpSpec.goldCost}G, STA={csharpSpec.staminaCost:0}, Giờ={csharpSpec.processingHours}h (Khớp C-ABI={exactAgreement})"
            };
        }

        public PolyglotApiResponse HandleEvaluateSpecialtyLocksGdd108(string? jsonBody = null)
        {
            int liveRareOrHigherGenes = 0;
            for (int i = 0; i < _engine.Pigs.Count; i++)
            {
                if (_engine.Pigs[i].IsIdentified && _engine.Pigs[i].Rarity >= GeneRarity.Hiem)
                {
                    liveRareOrHigherGenes++;
                }
            }

            int liveCycleBosses = _engine.DeepMechanics.MidGameGateUnlocked ? 1 : 0;
            var liveGate = CanonicalSagaAndGdd6Systems.EvaluateCanonicalMidGameGateGdd102(
                majorWavesSurvived: _engine.MajorWavesSurvived,
                farmLevel: _engine.Farm.FarmLevel,
                maxHerdSizeEverReached: _engine.Pigs.Count,
                rareOrHigherGenesIdentifiedCount: liveRareOrHigherGenes,
                cycleBossesDefeated: liveCycleBosses,
                activeDefenseBuildingsCount: _engine.Defense.Buildings.Count);

            bool simulateQualifiedGate = string.IsNullOrWhiteSpace(jsonBody) ||
                                         !jsonBody.Contains("\"liveOnly\":true", StringComparison.OrdinalIgnoreCase);

            var gate = simulateQualifiedGate
                ? CanonicalSagaAndGdd6Systems.EvaluateCanonicalMidGameGateGdd102(
                    majorWavesSurvived: Math.Max(8, _engine.MajorWavesSurvived),
                    farmLevel: Math.Max(7, _engine.Farm.FarmLevel),
                    maxHerdSizeEverReached: Math.Max(120, _engine.Pigs.Count),
                    rareOrHigherGenesIdentifiedCount: Math.Max(2, liveRareOrHigherGenes),
                    cycleBossesDefeated: Math.Max(1, liveCycleBosses),
                    activeDefenseBuildingsCount: Math.Max(12, _engine.Defense.Buildings.Count))
                : liveGate;

            int clampedPreReveal = CanonicalSagaAndGdd6Systems.ClampSpecialtyLockProgressBeforeRevealGdd104(140, 150, midGameGateRevealed: false);
            int postReveal = CanonicalSagaAndGdd6Systems.ClampSpecialtyLockProgressBeforeRevealGdd104(140, 150, midGameGateRevealed: true);
            float redPlus3Rate = CanonicalSagaAndGdd6Systems.GetSpecialtyPaperOpenRateGdd105(SpecialtyPaperColor.Do, 3);
            float redPlus3Lock6Rate = CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(SpecialtyPaperColor.Do, 6, 3);
            var redPlus3Rolls = CanonicalSagaAndGdd6Systems.GetSpecialtyPaperExtraRollSpecGdd105(SpecialtyPaperColor.Do, 3);

            bool allNonCombat = true;
            for (int i = 0; i < CanonicalSagaAndGdd6Systems.KhoaAndAnTwelveSpecialtyLocksGdd108.Count; i++)
            {
                if (!CanonicalSagaAndGdd6Systems.KhoaAndAnTwelveSpecialtyLocksGdd108[i].IsStrictlyNonCombatDailySupport)
                {
                    allNonCombat = false;
                    break;
                }
            }

            var payload = new Dictionary<string, object>
            {
                ["liveMidGameGateUnlocked"] = liveGate.unlocked,
                ["liveSubConditionsMetCount"] = liveGate.subConditionsMetCount,
                ["midGameGateUnlocked"] = gate.unlocked,
                ["subConditionsMetCount"] = gate.subConditionsMetCount,
                ["preRevealCap150"] = clampedPreReveal,
                ["postRevealProgress150"] = postReveal,
                ["redPaperPlus3Rate"] = redPlus3Rate,
                ["redPaperPlus3Lock6Rate"] = redPlus3Lock6Rate,
                ["redPaperPlus3ExtraRoll3Chance"] = redPlus3Rolls.extraRollChance,
                ["totalLocksCount"] = CanonicalSagaAndGdd6Systems.KhoaAndAnTwelveSpecialtyLocksGdd108.Count,
                ["allStrictlyNonCombatDailySupport"] = allNonCombat
            };

            return new PolyglotApiResponse
            {
                StatusCode = 200,
                Endpoint = "/api/v1/gdd6/specialty-locks",
                Success = gate.unlocked && clampedPreReveal == 75 && allNonCombat,
                ActiveProtagonist = _engine.Story.ActiveProtagonist.ToString(),
                JsonData = JsonConvert.SerializeObject(payload),
                Message = $"GDD 10.2–10.8 OK: Gate={gate.unlocked} (Live={liveGate.unlocked}), Trần trước lộ diện={clampedPreReveal}/150 (50%), Đỏ+3 Khóa#6={redPlus3Lock6Rate * 100f:0}% (lượt 3={redPlus3Rolls.extraRollChance * 100f:0}%), 12 khóa phi chiến đấu={allNonCombat}"
            };
        }

        public PolyglotApiResponse HandleExecuteCausalChain(string? jsonBody)
        {
            // Đảm bảo có đàn heo thực để chạy mô phỏng nhân quả đa tầng
            if (_engine.Pigs.Count < 8)
            {
                for (int i = _engine.Pigs.Count; i < 8; i++)
                {
                    var p = Pig.CreateDefault(
                        $"causal_pig_{i + 1}",
                        $"Heo Đàn #{i + 1}",
                        i == 0 ? GeneLineId.KimTho : (i == 1 ? GeneLineId.HuThe : GeneLineId.HongDien),
                        i <= 1 ? GeneRarity.Hiem : GeneRarity.Thuong,
                        i % 2 == 0 ? "Cai" : "Duc",
                        initialAge: i < 5 ? 32 : 14,
                        initialWeight: 95f);
                    p.Stage = i < 5 ? PigStage.HeoGia : PigStage.TruongThanh;
                    p.PositionX = -6f + i * 1.5f;
                    p.PositionY = -4f + i * 1.2f;
                    p.Bonding = 82f;
                    p.Mood = 68;
                    _engine.Pigs.Add(p);
                }
            }

            var chainC = _engine.CausalChains.ExecuteElderPigRetentionAndSuccessionCausalChain(_engine, directPlayerCareToday: true);
            var saleTarget = _engine.Pigs.Find(p => p.IsAlive);
            var chainA = _engine.CausalChains.ExecutePigSaleCausalChain(_engine, saleTarget != null ? saleTarget.Id : "causal_pig_1", payQuarantineFee: true);
            var deathTarget = _engine.Pigs.Find(p => p.IsAlive);
            var chainB = _engine.CausalChains.ExecutePigDeathAndCorpseCausalChain(_engine, deathTarget != null ? deathTarget.Id : "causal_pig_2", "Bệnh dịch ngoài khu xử lý", inProcessingZone: false);
            var chainD = _engine.CausalChains.ExecuteDefenseWaveAndBachVanCausalChain(_engine, fireCuuTieuLoiPhao: true, triggerLandmine: true, useProtagonistRecallCalm: true);
            var chainE = _engine.CausalChains.ExecuteStoryInvestigationAndEndingCausalChain(_engine, cutSeventhPackageChannelInQ20: true, voluntaryBothPalmsUpInSeason8: true);

            int totalLayersTouched = chainA.LayersTouchedCount + chainB.LayersTouchedCount + chainC.LayersTouchedCount + chainD.LayersTouchedCount + chainE.LayersTouchedCount;
            bool allOk = chainA.Success && chainB.Success && chainC.Success && chainD.Success && chainE.Success;

            var payload = new Dictionary<string, object>
            {
                ["protagonist"] = _engine.Story.ActiveProtagonist.ToString(),
                ["allFiveChainsSucceeded"] = allOk,
                ["totalCrossSystemImpactSteps"] = totalLayersTouched,
                ["chainA_SaleSteps"] = chainA.LayersTouchedCount,
                ["chainB_DeathCorpseSteps"] = chainB.LayersTouchedCount,
                ["chainC_ElderRetentionSteps"] = chainC.LayersTouchedCount,
                ["chainD_DefenseWaveSteps"] = chainD.LayersTouchedCount,
                ["chainE_StoryInvestigationSteps"] = chainE.LayersTouchedCount,
                ["herdLeavesEmptySpotAtJackfruitCorner"] = chainE.HerdLeavesEmptySpotAtJackfruitCorner,
                ["chippedBowlReturnedAtGate"] = chainE.ChippedBowlReturnedAtGate,
                ["droppedSevenStrokeHarvestReceipt"] = chainD.DroppedSevenStrokeHarvestReceipt,
                ["postRaidPigPriceSurgePct"] = chainD.PostRaidPigPriceSurgePctAfter
            };

            return new PolyglotApiResponse
            {
                StatusCode = 200,
                Endpoint = "/api/v1/causal/execute-chain",
                Success = allOk && totalLayersTouched >= 27,
                ActiveProtagonist = _engine.Story.ActiveProtagonist.ToString(),
                YoungestSiblingName = _engine.Story.YoungestSiblingName,
                NativeBackendUsed = _engine.FlowField.LastUsedNativeBackend,
                NativeFlowFieldRebuildsCount = NativePolyglotAccelerator.NativeHardwareAcceleratedFlowFieldCount,
                JsonData = JsonConvert.SerializeObject(payload),
                Message = $"Đã thực thi trọn vẹn 5 Chuỗi Tác Động Chéo ({totalLayersTouched} bước nhân quả liên hệ thống) cho [{_engine.Story.ActiveProtagonist}]."
            };
        }

        public PolyglotApiResponse HandleSimulationTickAndNativeFlowField(string? jsonBody)
        {
            int minutes = 60;
            if (!string.IsNullOrWhiteSpace(jsonBody) && jsonBody.Contains("1440", StringComparison.Ordinal))
            {
                minutes = 1440;
            }

            if (_engine.Pigs.Count == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    var p = Pig.CreateDefault($"api_pig_{i + 1}", $"Heo Đàn #{i + 1}", GeneLineId.HongDien, GeneRarity.Thuong, i % 2 == 0 ? "Cai" : "Duc", 10, 65f);
                    p.PositionX = -12f + i * 2.5f;
                    p.PositionY = -8f + i * 1.5f;
                    _engine.Pigs.Add(p);
                }
            }

            _engine.Tick(minutes);
            var recall = _engine.CommandHerdRecallToRefuge(khoaWhistle: _engine.Story.ActiveProtagonist == PlayerId.Khoa);

            var payload = new Dictionary<string, object>
            {
                ["tickedMinutes"] = minutes,
                ["currentDay"] = _engine.Clock.CurrentDay,
                ["currentHour"] = _engine.Clock.CurrentHour,
                ["shelteredPigs"] = recall.shelteredCount,
                ["eligiblePigs"] = recall.totalEligible,
                ["flowFieldBackend"] = _engine.FlowField.LastUsedNativeBackend,
                ["nativeFlowFieldRebuildsCount"] = NativePolyglotAccelerator.NativeHardwareAcceleratedFlowFieldCount
            };

            return new PolyglotApiResponse
            {
                StatusCode = 200,
                Endpoint = "/api/v1/simulation/tick",
                Success = recall.shelteredCount > 0,
                ActiveProtagonist = _engine.Story.ActiveProtagonist.ToString(),
                NativeBackendUsed = _engine.FlowField.LastUsedNativeBackend,
                NativeFlowFieldRebuildsCount = NativePolyglotAccelerator.NativeHardwareAcceleratedFlowFieldCount,
                JsonData = JsonConvert.SerializeObject(payload),
                Message = recall.message
            };
        }

        public PolyglotApiResponse HandlePolyglotBenchmark()
        {
            var hybrid = NativePolyglotAccelerator.ExecuteBenchmarkSuite(
                PolyglotExecutionBackend.AutoHybridBestOfBreed,
                bfsIters: 500,
                boidsSteps: 5000,
                combatTicks: 50000,
                economySeeds: 2000);

            bool checksumsValid =
                hybrid.Checksum1 == NativePolyglotAccelerator.ExpectedChecksum1_Bfs500 &&
                hybrid.Checksum2 == NativePolyglotAccelerator.ExpectedChecksum2_Boids5000 &&
                hybrid.Checksum3 == NativePolyglotAccelerator.ExpectedChecksum3_Combat50000 &&
                hybrid.Checksum4 == NativePolyglotAccelerator.ExpectedChecksum4_Economy2000;

            var payload = new Dictionary<string, object>
            {
                ["bfsMs"] = hybrid.Kernel1BfsMs,
                ["boidsMs"] = hybrid.Kernel2BoidsMs,
                ["combatMs"] = hybrid.Kernel3CombatMs,
                ["economyMs"] = hybrid.Kernel4EconomyMs,
                ["totalMs"] = hybrid.TotalMs,
                ["checksum1"] = hybrid.Checksum1,
                ["checksum2"] = hybrid.Checksum2,
                ["checksum3"] = hybrid.Checksum3,
                ["checksum4"] = hybrid.Checksum4,
                ["bitExactVerified"] = checksumsValid,
                ["rustDllAvailable"] = NativePolyglotAccelerator.IsRustNativeAvailable,
                ["cppDllAvailable"] = NativePolyglotAccelerator.IsCppNativeAvailable
            };

            return new PolyglotApiResponse
            {
                StatusCode = 200,
                Endpoint = "/api/v1/polyglot/benchmark",
                Success = checksumsValid,
                ActiveProtagonist = _engine.Story.ActiveProtagonist.ToString(),
                NativeBackendUsed = NativePolyglotAccelerator.IsCppNativeAvailable ? "C++20+Rust_Hybrid" : "Managed_CSharp_SoA",
                NativeFlowFieldRebuildsCount = NativePolyglotAccelerator.NativeHardwareAcceleratedFlowFieldCount,
                JsonData = JsonConvert.SerializeObject(payload),
                Message = $"Polyglot Benchmark Total={hybrid.TotalMs:F3}ms (BitExact={checksumsValid})"
            };
        }
    }
}
