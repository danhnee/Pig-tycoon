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
                ShowWaveBanner("CẢNH BÁO: BẦU TRỜI CHỚP TÍM - NHIỄU SÓNG TỪ TRƯỜNG! ALIEN ĐANG HẠ CÁNH...", isWarning: true);
                StartCoroutine(SpawnAlienRaidRoutine(count));
            }
            else
            {
                ShowWaveBanner("CẢNH BÁO: TIẾNG ĐUỐC VÀ CHÓ SỦA Ở BÌA RỪNG! ĐẠO TẶC TIẾP CẬN...", isWarning: true);
                StartCoroutine(SpawnBanditRaidRoutine(count));
            }
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

            for (int i = 0; i < count; i++)
            {
                EnemyTypeId type = (i % 3 == 0) ? EnemyTypeId.KeBatHeo :
                                   (i % 3 == 1) ? EnemyTypeId.KeCaoPha : EnemyTypeId.ChuotGamKho;

                SpawnEnemyAt(type, finalSpawnPositions[i]);
            }
        }

        private IEnumerator SpawnAlienRaidRoutine(int count)
        {
            // Dấu hiệu thị giác Telegraph (2.0s): Cột sáng Tractor Beam tím chiếu từ mây xuống
            List<GameObject> beamObjects = new List<GameObject>();
            List<Vector2> beamPositions = new List<Vector2>();

            for (int i = 0; i < count; i++)
            {
                Vector2 beamPos = new Vector2(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-5f, 5f));
                beamPositions.Add(beamPos);

                var beamObj = new GameObject($"Alien_TractorBeam_Telegraph_{i}");
                beamObj.transform.position = new Vector3(beamPos.x, beamPos.y + 1.8f, 0f);
                var sr = beamObj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetTractorBeamVFX();
                sr.sortingOrder = 280;
                sr.color = new Color(0.6f, 0.3f, 1f, 0.9f); // Ánh tím cosmic

                var labelObj = new GameObject("Label");
                labelObj.transform.SetParent(beamObj.transform, false);
                labelObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                var tm = labelObj.AddComponent<TextMeshPro>();
                tm.text = "<color=#c084fc><b>⚡ UFO TELEPORT</b></color>";
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

            // Xóa chùm sáng và đáp UFO xuống
            for (int i = 0; i < beamObjects.Count; i++)
            {
                if (beamObjects[i] != null) Destroy(beamObjects[i]);

                EnemyTypeId alienType = (i % 2 == 0) ? EnemyTypeId.UfoTrinhSat : EnemyTypeId.UfoThuHoach;
                SpawnEnemyAt(alienType, beamPositions[i]);
            }
        }

        public EnemyAgentView SpawnEnemyAt(EnemyTypeId type, Vector2 position)
        {
            EnemyData data = type switch
            {
                EnemyTypeId.KeCaoPha => EnemyData.CreateKeCaoPha(),
                EnemyTypeId.ChuotGamKho => EnemyData.CreateChuotGamKho(),
                EnemyTypeId.DauLinhTrom => EnemyData.CreateDauLinhTrom(),
                EnemyTypeId.UfoTrinhSat => EnemyData.CreateUfoTrinhSat(),
                EnemyTypeId.UfoThuHoach => EnemyData.CreateUfoThuHoach(),
                _ => EnemyData.CreateKeBatHeo()
            };

            var enemyObj = new GameObject($"Enemy_{data.Name}_{Guid.NewGuid():N}".Substring(0, 20));
            enemyObj.transform.position = position;

            var sr = enemyObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 100;

            var col = enemyObj.AddComponent<CapsuleCollider2D>();
            col.size = (type == EnemyTypeId.ChuotGamKho) ? new Vector2(0.55f, 0.4f) : new Vector2(0.65f, 1.45f);
            col.offset = (type == EnemyTypeId.ChuotGamKho) ? new Vector2(0f, -0.2f) : new Vector2(0f, 0.05f);
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
            StopAllCoroutines();
            StartCoroutine(BannerFadeRoutine());
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
