using System;

using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using TMPro;

using PigTycoon.Core;



namespace PigTycoon.Presentation

{

    public class EnemyWaveSpawner : MonoBehaviour

    {

        public static EnemyWaveSpawner Instance { get; private set; }



        [Header("Wave State")]

        public bool IsWaveActive { get; private set; } = false;

        public int ActiveEnemyCount => activeEnemies.Count;



        private readonly List<EnemyAgentView> activeEnemies = new List<EnemyAgentView>();

        private GameObject waveBannerObj;

        private TextMeshProUGUI waveBannerText;

        private Coroutine bannerCoroutine;



        private void Awake()

        {

            if (Instance != null && Instance != this)

            {

                Destroy(gameObject);

                return;

            }

            Instance = this;

        }



        private void Start()

        {

            CreateWaveBannerUI();



            if (MobileGameController.Instance != null)

            {

                MobileGameController.Instance.OnStateUpdated += CheckAutoWaveTrigger;

            }

        }



        private void OnDestroy()

        {

            if (MobileGameController.Instance != null)

            {

                MobileGameController.Instance.OnStateUpdated -= CheckAutoWaveTrigger;

            }

        }



        private void CheckAutoWaveTrigger()

        {

            if (IsWaveActive) return;



            var engine = MobileGameController.Instance?.Engine;

            if (engine == null) return;



            // Kích hoạt khi GameClock báo có đợt quái đột kích (Minor Wave)

            if (engine.Clock.IsMinorWaveTriggered && !IsWaveActive)

            {

                engine.Clock.IsMinorWaveTriggered = false;

                TriggerWave("Đạo Tặc Đột Kích Đêm", 3);

            }

        }



        public void TriggerWave(string waveName = "Đạo Tặc Thảo Nguyên", int count = 3, RaidType raidType = RaidType.BanditRaid)

        {

            if (IsWaveActive && activeEnemies.Count > 0)

            {

                ShowWaveBanner($"Đợt quái hiện tại vẫn đang diễn ra! ({activeEnemies.Count} tên còn lại)");

                return;

            }



            IsWaveActive = true;

            activeEnemies.Clear();



            var engine = MobileGameController.Instance?.Engine;

            if (engine != null)

            {

                engine.Clock.IsDuringCombatWave = true;

            }



            if (raidType == RaidType.AlienRaid)

            {

                ShowWaveBanner("CẢNH BÁO: BẦU TRỜI CHỚP TÍM - NHIỄU SÓNG TỪ TRƯỜNG! ĐỘI QUÂN ALIEN ĐỘT KÍCH...", isWarning: true);

                StartCoroutine(SpawnAlienRaidRoutine(count));

            }

            else if (raidType == RaidType.AlienMothershipInvasion)

            {

                ShowWaveBanner("BÁO ĐỘNG ĐỎ CẤP CAO: TÀU MẸ UFO ĐANG ĐÁP XUỐNG! BOSS XUẤT HIỆN!", isWarning: true);

                StartCoroutine(SpawnMothershipInvasionRoutine());

            }

            else

            {

                ShowWaveBanner("CẢNH BÁO: TIẾNG ĐUỐC VÀ CHÓ SỦA Ở BÌA RỪNG! ĐẠO TẶC TIẾP CẬN...", isWarning: true);

                StartCoroutine(SpawnBanditRaidRoutine(count));

            }

        }



        public void TriggerAlienDrop3(Vector2 centerPos)

        {

            StartCoroutine(SpawnAlienDrop3Routine(centerPos));

        }



        private IEnumerator SpawnAlienDrop3Routine(Vector2 centerPos)

        {

            ShowWaveBanner("UFO THẤT BẠI: KÍCH HOẠT ĐỔ BỘ - 3 LÍNH ALIEN ĐANG ĐƯỢC THẢ XUỐNG!", isWarning: true);

            IsWaveActive = true;



            var engine = MobileGameController.Instance?.Engine;

            if (engine != null)

            {

                engine.Clock.IsDuringCombatWave = true;

            }



            int day = engine?.Clock?.CurrentDay ?? 1;



            Vector2[] offsets = new Vector2[]

            {

                new Vector2(-1.5f, -0.6f),

                new Vector2(1.5f, -0.6f),

                new Vector2(0f, 1.3f)

            };



            List<GameObject> beamObjects = new List<GameObject>();

            List<Vector2> spawnPositions = new List<Vector2>();



            for (int i = 0; i < 3; i++)

            {

                Vector2 spawnPos = centerPos + offsets[i];

                spawnPositions.Add(spawnPos);



                var beamObj = new GameObject($"Alien_Drop_Beam_{i}");

                beamObj.transform.position = new Vector3(spawnPos.x, spawnPos.y + 1.5f, 0f);

                var sr = beamObj.AddComponent<SpriteRenderer>();

                sr.sprite = UISpriteLoader.GetTractorBeamVFX();

                sr.sortingOrder = 280;

                sr.color = new Color(0.8f, 0.2f, 1f, 0.9f);



                var labelObj = new GameObject("Label");

                labelObj.transform.SetParent(beamObj.transform, false);

                labelObj.transform.localPosition = new Vector3(0f, 1.1f, 0f);

                var tm = labelObj.AddComponent<TextMeshPro>();

                tm.text = "<color=#c084fc><b>⚡ ALIEN DROP</b></color>";

                tm.fontSize = 2.8f;

                tm.alignment = TextAlignmentOptions.Center;

                tm.sortingOrder = 750;



                beamObjects.Add(beamObj);

            }



            float elapsed = 0f;

            while (elapsed < 1.6f)

            {

                elapsed += Time.deltaTime;

                float pulse = 0.85f + Mathf.PingPong(elapsed * 6f, 0.35f);

                foreach (var b in beamObjects)

                {

                    if (b != null) b.transform.localScale = new Vector3(pulse, 1f, 1f);

                }

                yield return null;

            }



            for (int i = 0; i < beamObjects.Count; i++)

            {

                if (beamObjects[i] != null) Destroy(beamObjects[i]);

                SpawnEnemyAt(EnemyTypeId.AlienGrunt, spawnPositions[i], day);

            }

        }



        public void TriggerMothershipInvasion()

        {

            if (IsWaveActive && activeEnemies.Count > 0)

            {

                ShowWaveBanner($"Đợt quái hiện tại vẫn đang diễn ra! ({activeEnemies.Count} tên còn lại)");

                return;

            }

            TriggerWave("Đại Xâm Lược Tàu Mẹ", 1, RaidType.AlienMothershipInvasion);

        }



        private IEnumerator SpawnMothershipInvasionRoutine()

        {

            var engine = MobileGameController.Instance?.Engine;

            int day = engine?.Clock?.CurrentDay ?? 1;



            // Tàu Mẹ đáp ở RÌA TRÊN của map (y=5.5 để nằm trong camera viewport).

            // Với orthographicSize ~6 và cam center ~y=0, viewport trên cùng ~y=6 → 5.5 an toàn.

            // Sprite Mothership scale 4.2x, canvas 128px/32PPU = 4 units → chiều cao ~2 units sau scale

            // → thân tàu chiếm y=4.5~5.5, vẫn thấy rõ trên màn.

            Vector2 shipPos = new Vector2(0f, 5.5f);

            Vector2 bossPos = new Vector2(0f, 3.2f);

            Vector2 leftPos = new Vector2(-4.2f, 3.8f);

            Vector2 rightPos = new Vector2(4.2f, 3.8f);



            var megaBeamObj = new GameObject("Mothership_MegaBeam");

            megaBeamObj.transform.position = new Vector3(shipPos.x, shipPos.y + 1.5f, 0f);

            var srMega = megaBeamObj.AddComponent<SpriteRenderer>();

            srMega.sprite = UISpriteLoader.GetTractorBeamVFX();

            srMega.sortingOrder = 290;

            srMega.color = new Color(0.95f, 0.15f, 0.85f, 0.95f);

            megaBeamObj.transform.localScale = new Vector3(9.0f, 4.8f, 1f);



            var labelObj = new GameObject("Label");

            labelObj.transform.SetParent(megaBeamObj.transform, false);

            labelObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            var tm = labelObj.AddComponent<TextMeshPro>();

            tm.text = "<color=#f43f5e><b>🚨 CẢNH BÁO: TÀU MẸ ĐÁP Ở RÌA TRANG TRẠI! 🚨</b></color>";

            tm.fontSize = 4.2f;

            tm.alignment = TextAlignmentOptions.Center;

            tm.sortingOrder = 800;



            var leftBeam = new GameObject("Elite_Left_Beam");

            leftBeam.transform.position = new Vector3(leftPos.x, leftPos.y + 1.6f, 0f);

            var srL = leftBeam.AddComponent<SpriteRenderer>();

            srL.sprite = UISpriteLoader.GetTractorBeamVFX();

            srL.sortingOrder = 280;

            srL.color = new Color(0.6f, 0.2f, 1f, 0.9f);

            leftBeam.transform.localScale = new Vector3(2.2f, 2.0f, 1f);



            var rightBeam = new GameObject("Elite_Right_Beam");

            rightBeam.transform.position = new Vector3(rightPos.x, rightPos.y + 1.6f, 0f);

            var srR = rightBeam.AddComponent<SpriteRenderer>();

            srR.sprite = UISpriteLoader.GetTractorBeamVFX();

            srR.sortingOrder = 280;

            srR.color = new Color(0.6f, 0.2f, 1f, 0.9f);

            rightBeam.transform.localScale = new Vector3(2.2f, 2.0f, 1f);



            float elapsed = 0f;

            while (elapsed < 2.5f)

            {

                elapsed += Time.deltaTime;

                float pulse = 8.5f + Mathf.PingPong(elapsed * 6f, 1.2f);

                if (megaBeamObj != null) megaBeamObj.transform.localScale = new Vector3(pulse, 4.8f, 1f);

                yield return null;

            }



            if (megaBeamObj != null) Destroy(megaBeamObj);

            if (leftBeam != null) Destroy(leftBeam);

            if (rightBeam != null) Destroy(rightBeam);



            // Sinh Tàu Mẹ ở rìa bản đồ, Boss Warlord và 2 Elite hộ tống tràn xuống phía trước

            SpawnEnemyAt(EnemyTypeId.UfoMothership, shipPos, day);

            SpawnEnemyAt(EnemyTypeId.BossAlienWarlord, bossPos, day);

            SpawnEnemyAt(EnemyTypeId.AlienElite, leftPos, day);

            SpawnEnemyAt(EnemyTypeId.AlienElite, rightPos, day);

        }



        private IEnumerator SpawnBanditRaidRoutine(int count)

        {

            Vector2 farmCenter = Vector2.zero;

            Vector2[] spawnOffsets = new Vector2[]

            {

                new Vector2(-15f, 9f),

                new Vector2(15f, 9f),

                new Vector2(-15f, -9f),

                new Vector2(15f, -9f),

                new Vector2(0f, 11f),

                new Vector2(0f, -11f)

            };



            // Dấu hiệu thị giác Telegraph (2.5s): Đuốc lửa cảnh báo bập bùng ở bìa rừng

            List<GameObject> warningMarkers = new List<GameObject>();

            List<Vector2> finalSpawnPositions = new List<Vector2>();



            for (int i = 0; i < count; i++)

            {

                Vector2 spawnPos = farmCenter + spawnOffsets[i % spawnOffsets.Length] + (Vector2)UnityEngine.Random.insideUnitCircle * 1.2f;

                finalSpawnPositions.Add(spawnPos);



                var markerObj = new GameObject($"Bandit_Torch_Telegraph_{i}");

                markerObj.transform.position = spawnPos;



                var sr = markerObj.AddComponent<SpriteRenderer>();

                sr.sprite = UISpriteLoader.GetTorchFireVFX() ?? UISpriteLoader.GetIconEnergyBolt();

                sr.sortingOrder = 300;

                sr.color = new Color(1f, 0.45f, 0.1f, 1f);



                // Nhãn cảnh báo nhấp nháy

                var labelObj = new GameObject("Label");

                labelObj.transform.SetParent(markerObj.transform, false);

                labelObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);

                var tm = labelObj.AddComponent<TextMeshPro>();

                tm.text = "<color=#ef4444><b>⚠️ ĐẠO TẶC!</b></color>";

                tm.fontSize = 3.2f;

                tm.alignment = TextAlignmentOptions.Center;

                tm.sortingOrder = 750;



                warningMarkers.Add(markerObj);

            }



            // Chờ 2.5s quan sát dấu hiệu

            float elapsed = 0f;

            while (elapsed < 2.5f)

            {

                elapsed += Time.deltaTime;

                float pulse = 0.85f + Mathf.PingPong(elapsed * 4f, 0.35f);

                foreach (var marker in warningMarkers)

                {

                    if (marker != null) marker.transform.localScale = Vector3.one * pulse;

                }

                yield return null;

            }



            // Xóa dấu hiệu đuốc và sinh quái tràn vào chuồng

            foreach (var marker in warningMarkers)

            {

                if (marker != null) Destroy(marker);

            }



            var engine = MobileGameController.Instance?.Engine;

            int day = engine?.Clock?.CurrentDay ?? 1;



            for (int i = 0; i < count; i++)

            {

                EnemyTypeId type = (i % 3 == 0) ? EnemyTypeId.KeBatHeo :

                                   (i % 3 == 1) ? EnemyTypeId.KeCaoPha : EnemyTypeId.ChuotGamKho;



                SpawnEnemyAt(type, finalSpawnPositions[i], day);

            }

        }



        private IEnumerator SpawnAlienRaidRoutine(int count)

        {

            // Dấu hiệu thị giác Telegraph (2.0s): Cột sáng Tractor Beam tím chiếu từ mây xuống

            List<GameObject> beamObjects = new List<GameObject>();

            List<Vector2> beamPositions = new List<Vector2>();

            List<EnemyTypeId> enemyTypes = new List<EnemyTypeId>();



            var engine = MobileGameController.Instance?.Engine;

            int day = engine?.Clock?.CurrentDay ?? 1;



            for (int i = 0; i < count; i++)

            {

                Vector2 beamPos = new Vector2(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-5f, 5f));

                beamPositions.Add(beamPos);



                // Cơ cấu Alien Raid: Alien Grunt (quân số chính), Alien Elite (chỉ huy), UFO (yểm trợ trên không)

                EnemyTypeId alienType;

                if (i == 0)

                {

                    alienType = EnemyTypeId.AlienElite; // 1 Elite chỉ huy

                }

                else if (i % 2 == 1)

                {

                    alienType = EnemyTypeId.AlienGrunt; // Lính bộ binh ngoài hành tinh

                }

                else

                {

                    alienType = (i % 4 == 0) ? EnemyTypeId.UfoTrinhSat : EnemyTypeId.AlienGrunt;

                }

                enemyTypes.Add(alienType);



                var beamObj = new GameObject($"Alien_TractorBeam_Telegraph_{i}");

                beamObj.transform.position = new Vector3(beamPos.x, beamPos.y + 1.8f, 0f);

                var sr = beamObj.AddComponent<SpriteRenderer>();

                sr.sprite = UISpriteLoader.GetTractorBeamVFX();

                sr.sortingOrder = 280;

                sr.color = (alienType == EnemyTypeId.AlienElite) ? new Color(1f, 0.2f, 0.6f, 0.95f) : new Color(0.6f, 0.3f, 1f, 0.9f);



                var labelObj = new GameObject("Label");

                labelObj.transform.SetParent(beamObj.transform, false);

                labelObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

                var tm = labelObj.AddComponent<TextMeshPro>();

                tm.text = (alienType == EnemyTypeId.AlienElite) ? "<color=#f43f5e><b>⚡ ALIEN ELITE</b></color>" : "<color=#c084fc><b>⚡ ALIEN TELEPORT</b></color>";

                tm.fontSize = 3.2f;

                tm.alignment = TextAlignmentOptions.Center;

                tm.sortingOrder = 750;



                beamObjects.Add(beamObj);

            }



            float elapsed = 0f;

            while (elapsed < 2.0f)

            {

                elapsed += Time.deltaTime;

                float pulse = 0.9f + Mathf.PingPong(elapsed * 6f, 0.3f);

                foreach (var b in beamObjects)

                {

                    if (b != null) b.transform.localScale = new Vector3(pulse, 1f, 1f);

                }

                yield return null;

            }



            // Xóa chùm sáng và xuất hiện các đơn vị quái Alien

            for (int i = 0; i < beamObjects.Count; i++)

            {

                if (beamObjects[i] != null) Destroy(beamObjects[i]);

                SpawnEnemyAt(enemyTypes[i], beamPositions[i], day);

            }

        }



        public static Vector3 GetScaleForEnemyType(EnemyTypeId type)

        {

            return type switch

            {

                EnemyTypeId.AlienGrunt => new Vector3(1.55f, 1.55f, 1f),

                EnemyTypeId.AlienElite => new Vector3(1.95f, 1.95f, 1f),

                EnemyTypeId.BossAlienWarlord => new Vector3(2.85f, 2.85f, 1f),

                EnemyTypeId.UfoTrinhSat or EnemyTypeId.UfoThuHoach => new Vector3(2.2f, 2.2f, 1f),

                EnemyTypeId.UfoMothership => new Vector3(4.2f, 4.2f, 1f),

                EnemyTypeId.KeBatHeo => new Vector3(1.35f, 1.35f, 1f),

                EnemyTypeId.KeCaoPha => new Vector3(1.5f, 1.5f, 1f),

                EnemyTypeId.DauLinhTrom => new Vector3(2.0f, 2.0f, 1f),

                EnemyTypeId.ChuotGamKho => new Vector3(1.15f, 1.15f, 1f),

                _ => new Vector3(1.35f, 1.35f, 1f)

            };

        }



        public EnemyAgentView SpawnEnemyAt(EnemyTypeId type, Vector2 position, int dayNumber = 1)

        {

            EnemyData data = type switch

            {

                EnemyTypeId.KeCaoPha => EnemyData.CreateKeCaoPha(),

                EnemyTypeId.ChuotGamKho => EnemyData.CreateChuotGamKho(),

                EnemyTypeId.DauLinhTrom => EnemyData.CreateDauLinhTrom(),

                EnemyTypeId.UfoTrinhSat => EnemyData.CreateUfoTrinhSat(),

                EnemyTypeId.UfoThuHoach => EnemyData.CreateUfoThuHoach(),

                EnemyTypeId.AlienGrunt => EnemyData.CreateAlienGrunt(dayNumber),

                EnemyTypeId.AlienElite => EnemyData.CreateAlienElite(dayNumber),

                EnemyTypeId.BossAlienWarlord => EnemyData.CreateBossAlienWarlord(dayNumber),

                EnemyTypeId.UfoMothership => EnemyData.CreateUfoMothership(dayNumber),

                _ => EnemyData.CreateKeBatHeo()

            };



            var enemyObj = new GameObject($"Enemy_{data.Name}_{Guid.NewGuid():N}".Substring(0, 20));

            enemyObj.transform.position = position;

            enemyObj.transform.localScale = GetScaleForEnemyType(type);



            var sr = enemyObj.AddComponent<SpriteRenderer>();

            sr.sortingOrder = (type == EnemyTypeId.UfoMothership) ? 95 : 100;



            var col = enemyObj.AddComponent<CapsuleCollider2D>();

            if (type == EnemyTypeId.ChuotGamKho)

            {

                col.size = new Vector2(0.55f, 0.4f);

                col.offset = new Vector2(0f, -0.2f);

            }

            else if (type == EnemyTypeId.UfoMothership)

            {

                col.size = new Vector2(3.6f, 1.8f);

                col.offset = new Vector2(0f, 0f);

            }

            else if (type == EnemyTypeId.BossAlienWarlord)

            {

                col.size = new Vector2(0.9f, 1.6f);

                col.offset = new Vector2(0f, 0.05f);

            }

            else if (type == EnemyTypeId.AlienElite)

            {

                col.size = new Vector2(0.75f, 1.55f);

                col.offset = new Vector2(0f, 0.05f);

            }

            else if (type == EnemyTypeId.UfoTrinhSat || type == EnemyTypeId.UfoThuHoach)

            {

                col.size = new Vector2(1.6f, 0.85f);

                col.offset = new Vector2(0f, 0f);

            }

            else

            {

                col.size = new Vector2(0.65f, 1.45f);

                col.offset = new Vector2(0f, 0.05f);

            }

            col.isTrigger = false;



            var rb = enemyObj.AddComponent<Rigidbody2D>();

            rb.gravityScale = 0f;

            rb.linearDamping = 6f;

            rb.freezeRotation = true;



            var agent = enemyObj.AddComponent<EnemyAgentView>();

            agent.Bind(data);

            agent.OnDefeated += OnEnemyDefeated;



            activeEnemies.Add(agent);

            return agent;

        }



        private void OnEnemyDefeated(EnemyAgentView enemy)

        {

            activeEnemies.Remove(enemy);



            if (activeEnemies.Count == 0 && IsWaveActive)

            {

                OnWaveCompleted();

            }

        }



        private void OnWaveCompleted()

        {

            IsWaveActive = false;



            var engine = MobileGameController.Instance?.Engine;

            if (engine != null)

            {

                engine.Clock.IsDuringCombatWave = false;

                engine.MinorWavesDefeated += 1;

                engine.Farm.AddGold(100);

                engine.Farm.AddFarmExp(50);

                MobileGameController.Instance?.OnStateUpdated?.Invoke();

            }



            ShowWaveBanner("ĐỢT TẤN CÔNG ĐÃ BỊ ĐẨY LÙI! Hãy kiểm tra rào chắn và thu gom chiến lợi phẩm.", isWarning: false);

        }



        private void CreateWaveBannerUI()

        {

            var canvas = FindAnyObjectByType<Canvas>();

            if (canvas == null) return;



            waveBannerObj = new GameObject("WaveNotificationBanner");

            waveBannerObj.transform.SetParent(canvas.transform, false);



            var rt = waveBannerObj.AddComponent<RectTransform>();

            rt.anchorMin = new Vector2(0.5f, 0.85f);

            rt.anchorMax = new Vector2(0.5f, 0.85f);

            rt.pivot = new Vector2(0.5f, 0.5f);

            rt.sizeDelta = new Vector2(650f, 50f);



            var bgImg = waveBannerObj.AddComponent<UnityEngine.UI.Image>();

            bgImg.sprite = UISpriteLoader.GetFrameWoodPanel();

            bgImg.type = UnityEngine.UI.Image.Type.Sliced;

            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);



            var textObj = new GameObject("Text");

            textObj.transform.SetParent(waveBannerObj.transform, false);

            var textRt = textObj.AddComponent<RectTransform>();

            textRt.anchorMin = Vector2.zero;

            textRt.anchorMax = Vector2.one;

            textRt.sizeDelta = Vector2.zero;



            waveBannerText = textObj.AddComponent<TextMeshProUGUI>();

            waveBannerText.fontSize = 20f;

            waveBannerText.fontStyle = FontStyles.Bold;

            waveBannerText.alignment = TextAlignmentOptions.Center;

            waveBannerText.color = Color.yellow;



            waveBannerObj.SetActive(false);

        }



        public void ShowWaveBanner(string message, bool isWarning = true)

        {

            if (waveBannerObj == null) CreateWaveBannerUI();

            if (waveBannerObj == null) return;



            if (waveBannerText != null)

            {

                waveBannerText.text = message;

                waveBannerText.color = isWarning ? new Color(1f, 0.35f, 0.25f, 1f) : new Color(0.3f, 1f, 0.4f, 1f);

            }



            waveBannerObj.SetActive(true);

            if (bannerCoroutine != null) StopCoroutine(bannerCoroutine);

            bannerCoroutine = StartCoroutine(BannerFadeRoutine());

        }



        private IEnumerator BannerFadeRoutine()

        {

            yield return new WaitForSeconds(3.5f);

            if (waveBannerObj != null)

            {

                waveBannerObj.SetActive(false);

            }

        }

    }

}

