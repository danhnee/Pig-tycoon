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

            // Kích hoạt khi GameClock báo có đợt quái đột kích (Minor Wave) hoặc buổi tối ngẫu nhiên
            if (engine.Clock.IsMinorWaveTriggered && !IsWaveActive)
            {
                TriggerWave("Đạo Tặc Đột Kích Đêm", 3);
            }
        }

        public void TriggerWave(string waveName = "Đạo Tặc Thảo Nguyên", int count = 3)
        {
            if (IsWaveActive && activeEnemies.Count > 0)
            {
                ShowWaveBanner($"Đợt quái hiện tại vẫn đang diễn ra! ({activeEnemies.Count} tên còn lại)");
                return;
            }

            IsWaveActive = true;
            activeEnemies.Clear();

            ShowWaveBanner($"CẢNH BÁO: {waveName.ToUpper()} ĐANG TẤN CÔNG!", isWarning: true);

            var env = FarmEnvironment2D.Instance;
            Vector2 farmCenter = Vector2.zero;

            // Sinh quái vật tại 4 góc / ranh giới ngoài nông trại
            Vector2[] spawnOffsets = new Vector2[]
            {
                new Vector2(-12f, 8f),
                new Vector2(12f, 8f),
                new Vector2(-12f, -8f),
                new Vector2(12f, -8f),
                new Vector2(0f, 12f)
            };

            for (int i = 0; i < count; i++)
            {
                Vector2 spawnPos = farmCenter + spawnOffsets[i % spawnOffsets.Length] + UnityEngine.Random.insideUnitCircle * 1.5f;

                EnemyTypeId type = (i % 3 == 0) ? EnemyTypeId.KeBatHeo :
                                   (i % 3 == 1) ? EnemyTypeId.KeCaoPha : EnemyTypeId.ChuotGamKho;

                SpawnEnemyAt(type, spawnPos);
            }

            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                engine.Clock.IsDuringCombatWave = true;
            }
        }

        public EnemyAgentView SpawnEnemyAt(EnemyTypeId type, Vector2 position)
        {
            EnemyData data = type switch
            {
                EnemyTypeId.KeCaoPha => EnemyData.CreateKeCaoPha(),
                EnemyTypeId.ChuotGamKho => EnemyData.CreateChuotGamKho(),
                EnemyTypeId.DauLinhTrom => EnemyData.CreateDauLinhTrom(),
                _ => EnemyData.CreateKeBatHeo()
            };

            var enemyObj = new GameObject($"Enemy_{data.Name}_{Guid.NewGuid():N}".Substring(0, 20));
            enemyObj.transform.position = position;

            var sr = enemyObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 100;

            var col = enemyObj.AddComponent<CapsuleCollider2D>();
            col.size = (type == EnemyTypeId.ChuotGamKho) ? new Vector2(0.6f, 0.5f) : new Vector2(0.7f, 1.0f);
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

            ShowWaveBanner("CHIẾN THẮNG: ĐÃ ĐẨY LÙI ĐỢT TẤN CÔNG! (+100G, +50 EXP)", isWarning: false);
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
