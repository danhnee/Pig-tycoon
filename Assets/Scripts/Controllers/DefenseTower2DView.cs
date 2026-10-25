using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Lớp hiển thị Tháp Phòng Thủ 2D chuyên sâu (Combat Juice / Technical Art).
    /// Áp dụng Cấu trúc phân tách Tháp 2 Lớp (Multi-part Rigging):
    /// - BaseRenderer: chân đế cố định bám đất
    /// - TurretRenderer: phần thân/nòng xoay hoặc nhấp nhô
    /// - GlowMaskRenderer: lớp phát sáng năng lượng cho phe Alien/Mystic
    /// - MuzzlePoint: điểm khạc đạn
    /// 
    /// Triển khai Procedural Animation:
    /// - Idle: Squash & Stretch nhịp thở phe Bio (C1-C5), Bobbing lơ lửng & Alpha Pulse phe Alien (A1-A5)
    /// - Charge-up: Rung lắc Jitter + kéo giãn tâm ngắm cho tháp hạng nặng (A5, B5, C5)
    /// - Fire & Recoil: Damped Spring (lò xo giảm chấn) giật lùi và nảy scale
    /// 
    /// Triển khai 7 Archetype Hoạt ảnh Tấn công & VFX chuyên biệt:
    /// 1. LaserBeam (A1, A5): LineRenderer 2 lớp (Inner White Core + Outer Cyan/Violet Glow) + Jitter + Scorch Decal
    /// 2. ParabolicMortar (A2, B2, C3): Tách Shadow trượt đất và Visual bay vồng, B2 xoay cưa + 8 đinh, C3 nảy 2 nhịp vỡ bãi axit
    /// 3. TractorCone & ConeFlamethrower (A3, B3, C2): Hạt phản trọng lực, vệt cháy đen 4s B3, nấm phồng xì bào tử C2
    /// 4. HarpoonChain (B4) & LeechTether (C4): Lao cắm giật lùi 3m xích sắt B4, huyết dẫn sóng sin hồi phục hàng rào C4
    /// 5. EMPShockwave (A4) & SpineBurst360 (C1): Sóng xung kích 5m rơi UFO A4, kén bật nắp bung 12 gai 360° C1
    /// 6. GroundSpikes (C5): Rung chấn rạn nứt 0.3s, chông xương trồi vọt Y: 0 -> 1.2 -> 1 hất tung mục tiêu
    /// 7. GatlingBallistic (B1) & HeavyHowitzer (B5): Xoay nòng + Muzzle Flash + văng vỏ đạn B1, Khói mù mịt + Rung màn hình B5
    /// </summary>
    public class DefenseTower2DView : MonoBehaviour, IDamageable
    {
        public DefenseBuildingInstance BuildingModel { get; private set; }

        [Header("Health & Durability")]
        public float MaxHp = 1500f;
        public float CurrentHp = 1500f;
        public bool IsAlive => CurrentHp > 0f && (BuildingModel == null || !BuildingModel.IsBroken);
        public Vector2 Position => transform.position;

        [Header("Multi-Part Rigging")]
        [FormerlySerializedAs("BaseSprite")]
        public SpriteRenderer BaseRenderer;

        [FormerlySerializedAs("TurretSprite")]
        public SpriteRenderer TurretRenderer;

        public SpriteRenderer GlowMaskRenderer;
        public Transform MuzzlePoint;

        [Header("Faction Diegetic Lighting & Visual Core")]
        public SpriteRenderer TorchRenderer;
        private GameObject warningEmoteObj;
        private SpriteRenderer warningEmoteSr;
        private float diegeticSmokeTimer = 0f;

        [Header("Bandit Mechanical Rigging Sub-Transforms")]
        public Transform DrumTransform;     // B1: Tang trống nỏ xoay tròn
        public Transform ArmTransform;      // B2: Cần bắn lò xo / cánh cung máy
        public Transform PistonTransform;   // B3: Piston thụt thò bơm hắc ín
        public Transform WinchTransform;    // B4: Cuộn tời dây xích kéo quái
        public Transform BarrelTransform;   // B5: Nòng pháo đại bác phình co giật

        // Backward-compatible properties
        public SpriteRenderer BaseSprite { get => BaseRenderer; set => BaseRenderer = value; }
        public SpriteRenderer TurretSprite { get => TurretRenderer; set => TurretRenderer = value; }

        [Header("Floating Info Bars")]
        public bool ShowFloatingBars = true;
        public bool OnlyShowBarsWhenDamaged = true;
        private GameObject healthBarRoot;
        private Transform healthBarFill;
        private Transform ammoBarFill;

        [Header("Damage Visuals")]
        private GameObject smokeEffectObj;
        private SpriteRenderer smokeSr;

        [Header("Targeting & Range")]
        public float RangeRadius = 7.0f;
        public LayerMask MonsterLayer;

        [Header("Bạch Vân Charge Effect")]
        public GameObject ChargingAura2D;

        [Header("Laser Beam Dual-Layer State")]
        private GameObject laserRootObj;
        private LineRenderer innerLaserLr;
        private LineRenderer outerLaserLr;
        private float laserRampUpTimer = 0f;
        private Transform lastLaserTarget = null;
        private GameObject activeScorchDecal = null;
        private Coroutine laserCooldownCoroutine = null;

        [Header("Showcase & Test Settings")]
        public bool TargetDummies = true;
        public bool InfiniteAmmoInTest = false;

        // Procedural Animation State
        private Vector3 initialBaseScale = Vector3.one;
        private Vector3 initialTurretLocalPos = Vector3.zero;
        private Vector3 initialTurretScale = Vector3.one;
        private Vector3 initialTowerLocalPos = Vector3.zero;
        private bool isAnchorCaptured = false;
        private float idleSeed = 0f;
        private bool isChargingUp = false;
        private Coroutine recoilCoroutine = null;
        private Coroutine jitterCoroutine = null;

        public Vector3 InitialTowerLocalPos => initialTowerLocalPos;

        public void SetAnchorPosition(Vector3 localPos)
        {
            initialTowerLocalPos = localPos;
            transform.localPosition = localPos;
            isAnchorCaptured = true;
        }

        public void CaptureAnchorPositionIfNeeded()
        {
            if (!isAnchorCaptured || (recoilCoroutine == null && jitterCoroutine == null && !isChargingUp))
            {
                initialTowerLocalPos = transform.localPosition;
                initialBaseScale = transform.localScale;
                isAnchorCaptured = true;
            }
        }

        private float shootCooldownTimer = 0f;
        private Transform currentTarget;

        private void Awake()
        {
            idleSeed = UnityEngine.Random.Range(0f, 100f);
            initialBaseScale = transform.localScale;
            initialTowerLocalPos = transform.localPosition;
            isAnchorCaptured = true;
            CombatObjectPool.EnsurePoolExists();
            EnsureRigging();
            EnsureBuildingModelInitialized();
        }

        public void EnsureRigging()
        {
            if (BaseRenderer == null)
            {
                BaseRenderer = GetComponent<SpriteRenderer>();
            }

            if (MuzzlePoint == null)
            {
                var mp = transform.Find("MuzzlePoint");
                if (mp == null)
                {
                    var newMp = new GameObject("MuzzlePoint");
                    newMp.transform.SetParent(transform, false);
                    newMp.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                    MuzzlePoint = newMp.transform;
                }
                else
                {
                    MuzzlePoint = mp;
                }
            }

            if (TurretRenderer != null)
            {
                initialTurretLocalPos = TurretRenderer.transform.localPosition;
                initialTurretScale = TurretRenderer.transform.localScale;
            }

            // Rigging cơ khí cho phe Băng Cướp: Tự động khởi tạo và gán các trục cơ học
            Transform parentT = (TurretRenderer != null && TurretRenderer.gameObject.activeSelf) ? TurretRenderer.transform : transform;
            if (DrumTransform == null)
            {
                var dt = parentT.Find("DrumTransform");
                if (dt == null)
                {
                    var newDt = new GameObject("DrumTransform");
                    newDt.transform.SetParent(parentT, false);
                    newDt.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                    DrumTransform = newDt.transform;
                }
                else DrumTransform = dt;
            }

            if (ArmTransform == null)
            {
                var at = parentT.Find("ArmTransform");
                if (at == null)
                {
                    var newAt = new GameObject("ArmTransform");
                    newAt.transform.SetParent(parentT, false);
                    newAt.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                    ArmTransform = newAt.transform;
                }
                else ArmTransform = at;
            }

            if (PistonTransform == null)
            {
                var pt = parentT.Find("PistonTransform");
                if (pt == null)
                {
                    var newPt = new GameObject("PistonTransform");
                    newPt.transform.SetParent(parentT, false);
                    newPt.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                    PistonTransform = newPt.transform;
                }
                else PistonTransform = pt;
            }

            if (WinchTransform == null)
            {
                var wt = parentT.Find("WinchTransform");
                if (wt == null)
                {
                    var newWt = new GameObject("WinchTransform");
                    newWt.transform.SetParent(parentT, false);
                    newWt.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                    WinchTransform = newWt.transform;
                }
                else WinchTransform = wt;
            }

            if (BarrelTransform == null)
            {
                var bt = parentT.Find("BarrelTransform");
                if (bt == null)
                {
                    var newBt = new GameObject("BarrelTransform");
                    newBt.transform.SetParent(parentT, false);
                    newBt.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                    BarrelTransform = newBt.transform;
                }
                else BarrelTransform = bt;
            }

            EnsureDiegeticLighting();
        }

        private void EnsureDiegeticLighting()
        {
            string bp = BuildingModel?.BlueprintId ?? gameObject.name.Replace("Tower_", "").Trim();
            int paren = bp.IndexOf('(');
            if (paren >= 0) bp = bp.Substring(0, paren).Trim();

            string faction = BuildingModel?.Faction ?? InferFaction(bp);

            // 1. GlowMask cho phe Alien, BioMystic, Bạch Vân
            bool needsGlowMask = faction == "Alien" || faction == "BioMystic" || faction == "BachVan" ||
                                 bp.StartsWith("TruPlasma") || bp.StartsWith("PhaoCoiLuongTu") || 
                                 bp.StartsWith("ThapPhanTrongLuc") || bp.StartsWith("TruXungDienEMP") || 
                                 bp.StartsWith("PhaoDaiPhanVatChat") || bp.StartsWith("CotToHutHon") || 
                                 bp.StartsWith("CotDiCot") || bp.StartsWith("CuuTieuLoiPhao") || 
                                 bp.StartsWith("VanLinh");

            if (needsGlowMask && BaseRenderer != null && GlowMaskRenderer == null)
            {
                var glowObj = transform.Find("GlowMask");
                if (glowObj == null)
                {
                    var newGlow = new GameObject("GlowMask");
                    newGlow.transform.SetParent(transform, false);
                    newGlow.transform.localPosition = Vector3.zero;
                    GlowMaskRenderer = newGlow.AddComponent<SpriteRenderer>();
                }
                else
                {
                    GlowMaskRenderer = glowObj.GetComponent<SpriteRenderer>();
                }

                if (GlowMaskRenderer != null)
                {
                    GlowMaskRenderer.sprite = BaseRenderer.sprite;
                    GlowMaskRenderer.sortingOrder = BaseRenderer.sortingOrder + 1;
                    Color glowTint = faction switch
                    {
                        "Alien" => (bp == "PhaoDaiPhanVatChat") ? new Color(0.85f, 0.15f, 1f, 0.65f) : new Color(0.15f, 0.90f, 1f, 0.65f),
                        "BioMystic" => bp.StartsWith("CotToHutHon") ? new Color(0.95f, 0.15f, 0.25f, 0.65f) : (bp.StartsWith("CotDiCot") ? new Color(0.75f, 0.25f, 0.95f, 0.65f) : new Color(0.20f, 0.95f, 0.35f, 0.65f)),
                        "BachVan" => new Color(1.0f, 0.88f, 0.35f, 0.75f),
                        _ => new Color(0.2f, 0.85f, 1f, 0.5f)
                    };
                    GlowMaskRenderer.color = glowTint;
                }
            }

            // 2. TorchRenderer cho phe Bandit & Base
            bool isBanditOrBase = faction == "Bandit" || faction == "Base" || bp.StartsWith("NoLienChau") || 
                                  bp.StartsWith("MayBanRiuCua") || bp.StartsWith("ThapPhunLua") || 
                                  bp.StartsWith("PhaoLaoMocXich") || bp.StartsWith("DaiBacChuaTrom") || 
                                  bp.StartsWith("NoXuyenVan");

            if (isBanditOrBase && TorchRenderer == null)
            {
                var torchObj = transform.Find("DiegeticTorch");
                if (torchObj == null)
                {
                    var newTorch = new GameObject("DiegeticTorch");
                    newTorch.transform.SetParent(transform, false);
                    newTorch.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                    TorchRenderer = newTorch.AddComponent<SpriteRenderer>();
                }
                else
                {
                    TorchRenderer = torchObj.GetComponent<SpriteRenderer>();
                }

                if (TorchRenderer != null)
                {
                    TorchRenderer.sprite = UISpriteLoader.GetTorchFireVFX() ?? UISpriteLoader.GetSoftCircleSprite();
                    TorchRenderer.sortingOrder = (BaseRenderer != null) ? BaseRenderer.sortingOrder + 5 : 300;
                    TorchRenderer.color = new Color(1.0f, 0.62f, 0.15f, 0.85f);
                    TorchRenderer.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                }
            }

            // 3. Emote bong bóng cảnh báo hết đạn (không dùng chữ số)
            if (warningEmoteObj == null)
            {
                var emote = transform.Find("WarningEmote");
                if (emote == null)
                {
                    warningEmoteObj = new GameObject("WarningEmote");
                    warningEmoteObj.transform.SetParent(transform, false);
                    warningEmoteObj.transform.localPosition = new Vector3(0f, 1.55f, 0f);
                    warningEmoteSr = warningEmoteObj.AddComponent<SpriteRenderer>();
                }
                else
                {
                    warningEmoteObj = emote.gameObject;
                    warningEmoteSr = emote.GetComponent<SpriteRenderer>();
                }

                if (warningEmoteSr != null)
                {
                    warningEmoteSr.sprite = UISpriteLoader.GetDefenseAmmoCrateSprite() ?? UISpriteLoader.GetIconEnergyBolt();
                    warningEmoteSr.sortingOrder = (BaseRenderer != null) ? BaseRenderer.sortingOrder + 25 : 450;
                    warningEmoteSr.color = new Color(1.0f, 0.35f, 0.15f, 0.95f);
                    warningEmoteObj.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
                    warningEmoteObj.SetActive(false);
                }
            }
        }

        public void Bind(DefenseBuildingInstance model)
        {
            BuildingModel = model;
            CaptureAnchorPositionIfNeeded();
            if (model != null)
            {
                MaxHp = model.MaxHp;
                CurrentHp = model.CurrentHp;
                RangeRadius = model.AttackRange > 0f ? model.AttackRange : 7.0f;

                EnsureRigging();

                if (BaseRenderer != null)
                {
                    BaseRenderer.sprite = UISpriteLoader.GetTowerSprite(model.BlueprintId);
                }

                EnsureDiegeticLighting();

                bool isStationary = model.IsBachVan || 
                    model.AttackType == "GravitonTractor" || 
                    model.AttackType == "RadialSpines" || 
                    model.AttackType == "ToxicSporeCloud" || 
                    model.AttackType == "LeechSwarm" || 
                    model.AttackType == "AncientGroundSpikes";

                if (TurretRenderer != null)
                {
                    TurretRenderer.gameObject.SetActive(!isStationary);
                }
            }

            EnsureFloatingBars();
            UpdateBarVisuals();
            UpdateDamageVisual();
        }

        public void EnsureBuildingModelInitialized()
        {
            string bpId = (BuildingModel != null && !string.IsNullOrEmpty(BuildingModel.BlueprintId)) 
                ? BuildingModel.BlueprintId 
                : gameObject.name.Replace("Tower_", "").Trim();

            int paren = bpId.IndexOf('(');
            if (paren >= 0) bpId = bpId.Substring(0, paren).Trim();

            // Đã được khởi tạo đầy đủ và khớp với blueprintId
            if (BuildingModel != null && BuildingModel.BlueprintId == bpId && !string.IsNullOrEmpty(BuildingModel.AttackType) && !string.IsNullOrEmpty(BuildingModel.AmmoName))
            {
                return;
            }

            var bp = DefenseManager.GetRegisteredBlueprint(bpId);
            if (bp != null)
            {
                var instance = DefenseManager.CreateDefaultInstance(bpId, transform.position.x, transform.position.y);
                Bind(instance);
                return;
            }

            // Fallback suy luận AttackType và Faction nếu ID bị biến tấu
            string inferredAttackType = InferAttackType(bpId);
            string inferredFaction = InferFaction(bpId);

            if (BuildingModel == null)
            {
                var fallbackInstance = new DefenseBuildingInstance
                {
                    InstanceId = $"tower_{bpId}_{Guid.NewGuid():N}",
                    BlueprintId = bpId,
                    Name = gameObject.name,
                    CurrentHp = (int)CurrentHp,
                    MaxHp = (int)MaxHp,
                    AmmoCapacity = 50,
                    AmmoCurrent = 50,
                    CooldownSeconds = 1.2f,
                    AttackRange = RangeRadius,
                    AttackType = inferredAttackType,
                    Faction = inferredFaction,
                    AmmoName = "Đạn Năng Lượng"
                };
                Bind(fallbackInstance);
            }
            else
            {
                BuildingModel.AttackType = inferredAttackType;
                BuildingModel.Faction = inferredFaction;
                if (string.IsNullOrEmpty(BuildingModel.AmmoName)) BuildingModel.AmmoName = "Đạn Năng Lượng";
            }
        }

        private static string InferAttackType(string bpId)
        {
            if (string.IsNullOrEmpty(bpId)) return "Direct";
            if (bpId.StartsWith("TruPlasma")) return "LaserBeam";
            if (bpId.StartsWith("PhaoCoiLuongTu")) return "ParabolicMortar";
            if (bpId.StartsWith("ThapPhanTrongLuc")) return "GravitonTractor";
            if (bpId.StartsWith("TruXungDienEMP")) return "EMPShockwave";
            if (bpId.StartsWith("PhaoDaiPhanVatChat")) return "AntimatterMegaBeam";
            if (bpId.StartsWith("NoLienChau")) return "RapidBallistic";
            if (bpId.StartsWith("MayBanRiuCua")) return "ParabolicSaw";
            if (bpId.StartsWith("ThapPhunLua")) return "ConeFlamethrower";
            if (bpId.StartsWith("PhaoLaoMocXich")) return "HarpoonPull";
            if (bpId.StartsWith("DaiBacChuaTrom")) return "RecoilHowitzer";
            if (bpId.StartsWith("UGaiDoc")) return "RadialSpines";
            if (bpId.StartsWith("TruNamBaoTu")) return "ToxicSporeCloud";
            if (bpId.StartsWith("OngBomDichAxit")) return "CorrosiveAcidGlob";
            if (bpId.StartsWith("CotToHutHon")) return "LeechSwarm";
            if (bpId.StartsWith("CotDiCot")) return "AncientGroundSpikes";
            if (bpId.StartsWith("CuuTieuLoiPhao")) return "BachVanLightning";
            if (bpId.StartsWith("ThienNhanVongDai")) return "BachVanVision";
            if (bpId.StartsWith("VanLinhHoiNguyenTran")) return "BachVanHealing";
            if (bpId.StartsWith("BayChongGo")) return "WoodSpikeTrap";
            return "Direct";
        }

        private static string InferFaction(string bpId)
        {
            if (string.IsNullOrEmpty(bpId)) return "Base";
            if (bpId.StartsWith("TruPlasma") || bpId.StartsWith("PhaoCoiLuongTu") || 
                bpId.StartsWith("ThapPhanTrongLuc") || bpId.StartsWith("TruXungDienEMP") || 
                bpId.StartsWith("PhaoDaiPhanVatChat")) return "Alien";
            if (bpId.StartsWith("NoLienChau") || bpId.StartsWith("MayBanRiuCua") || 
                bpId.StartsWith("ThapPhunLua") || bpId.StartsWith("PhaoLaoMocXich") || 
                bpId.StartsWith("DaiBacChuaTrom")) return "Bandit";
            if (bpId.StartsWith("UGaiDoc") || bpId.StartsWith("TruNamBaoTu") || 
                bpId.StartsWith("OngBomDichAxit") || bpId.StartsWith("CotToHutHon") || 
                bpId.StartsWith("CotDiCot")) return "BioMystic";
            if (bpId.StartsWith("CuuTieuLoiPhao") || bpId.StartsWith("ThienNhanVongDai") || 
                bpId.StartsWith("VanLinhHoiNguyenTran")) return "BachVan";
            return "Base";
        }

        private void Start()
        {
            CaptureAnchorPositionIfNeeded();
            EnsureBuildingModelInitialized();
            EnsureRigging();
            EnsureFloatingBars();
            UpdateBarVisuals();
            UpdateDamageVisual();
        }

        private void Update()
        {
            if (BuildingModel == null || BuildingModel.IsBroken)
            {
                DisableLaserBeam();
                return;
            }

            // Procedural Diegetic Lighting & Visual Ammo State
            UpdateDiegeticLighting();

            // Xử lý nạp năng lượng của Bạch Vân (Cửu Tiêu Lôi Pháo)
            if (BuildingModel.IsBachVan)
            {
                if (BuildingModel.CurrentCooldownRemaining > 0f)
                {
                    BuildingModel.CurrentCooldownRemaining -= Time.deltaTime;
                }

                if (ChargingAura2D != null)
                {
                    ChargingAura2D.SetActive(BuildingModel.CurrentCooldownRemaining > 0f);
                }
                UpdateBarVisuals();
                return;
            }

            // Đếm ngược cooldown bắn thường
            if (shootCooldownTimer > 0f)
            {
                shootCooldownTimer -= Time.deltaTime;
            }

            // Khi cạn đạn, ngắt laser/mục tiêu và giữ tư thế gục nòng (drooped pose)
            if (BuildingModel.AmmoCurrent <= 0)
            {
                DisableLaserBeam();
                currentTarget = null;
                return;
            }

            // Tìm mục tiêu quái gần nhất trong bán kính 2D
            FindNearestTarget2D();

            if (currentTarget != null)
            {
                Vector2 dir = (Vector2)currentTarget.position - (Vector2)transform.position;

                // Xoay nòng tháp nếu có TurretRenderer
                if (TurretRenderer != null && TurretRenderer.gameObject.activeSelf && !isChargingUp)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    TurretRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
                }

                // Tự động bắn nếu còn đạn và hết cooldown
                if (shootCooldownTimer <= 0f && BuildingModel.AmmoCurrent > 0)
                {
                    Shoot2D(dir.normalized);
                }
            }
            else
            {
                DisableLaserBeam();
            }
        }

        // =========================================================================
        // FACTION DIEGETIC LIGHTING & VISUAL AMMO STATE
        // =========================================================================

        private void UpdateDiegeticLighting()
        {
            if (isChargingUp) return;

            string bp = BuildingModel?.BlueprintId ?? gameObject.name.Replace("Tower_", "").Trim();
            int paren = bp.IndexOf('(');
            if (paren >= 0) bp = bp.Substring(0, paren).Trim();

            string faction = BuildingModel?.Faction ?? InferFaction(bp);

            float ammoRatio = (BuildingModel != null && BuildingModel.AmmoCapacity > 0)
                ? Mathf.Clamp01((float)BuildingModel.AmmoCurrent / BuildingModel.AmmoCapacity)
                : 1f;

            bool isOutOfAmmo = (ammoRatio <= 0f);
            bool isLowAmmo = (ammoRatio > 0f && ammoRatio < 0.25f);
            bool isHighAmmo = (ammoRatio >= 0.65f);

            // 1. PHE ALIEN (CÔNG NGHỆ NĂNG LƯỢNG HUYỀN ẢO & TRỌNG LỰC)
            if (faction == "Alien" || bp.StartsWith("TruPlasma") || bp.StartsWith("PhaoCoiLuongTu") || 
                bp.StartsWith("ThapPhanTrongLuc") || bp.StartsWith("TruXungDienEMP") || 
                bp.StartsWith("PhaoDaiPhanVatChat"))
            {
                Color baseGlowColor = (bp == "PhaoDaiPhanVatChat")
                    ? new Color(0.85f, 0.15f, 1f, 1f)
                    : new Color(0.15f, 0.90f, 1f, 1f);

                if (isOutOfAmmo)
                {
                    if (GlowMaskRenderer != null)
                    {
                        GlowMaskRenderer.color = new Color(baseGlowColor.r, baseGlowColor.g, baseGlowColor.b, 0f);
                    }
                    if (TurretRenderer != null && TurretRenderer.gameObject.activeSelf)
                    {
                        TurretRenderer.transform.localPosition = initialTurretLocalPos + new Vector3(0f, -0.09f, 0f);
                        if (currentTarget == null)
                        {
                            TurretRenderer.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                        }
                    }
                }
                else if (isLowAmmo)
                {
                    // Glitch / chớp giật điện tần số cao
                    float glitch = (Mathf.PerlinNoise(Time.time * 25f, idleSeed) > 0.45f) ? 0.65f : 0.08f;
                    if (GlowMaskRenderer != null)
                    {
                        GlowMaskRenderer.color = new Color(baseGlowColor.r, baseGlowColor.g, baseGlowColor.b, glitch);
                    }
                    if (TurretRenderer != null && TurretRenderer.gameObject.activeSelf)
                    {
                        float jitterY = (Mathf.PerlinNoise(Time.time * 30f, idleSeed) - 0.5f) * 0.03f;
                        TurretRenderer.transform.localPosition = initialTurretLocalPos + new Vector3(0f, -0.04f + jitterY, 0f);
                    }
                }
                else
                {
                    // Đạn ổn định / dồi dào: Bobbing lơ lửng điều hòa + phát quang
                    float bobSpeed = isHighAmmo ? 3.0f : 2.0f;
                    float bobAmp = isHighAmmo ? 0.07f : 0.04f;
                    float bob = Mathf.Sin(Time.time * bobSpeed + idleSeed) * bobAmp;

                    if (TurretRenderer != null && TurretRenderer.gameObject.activeSelf)
                    {
                        TurretRenderer.transform.localPosition = initialTurretLocalPos + new Vector3(0f, bob, 0f);
                        if (currentTarget == null && TurretRenderer.transform.rotation == Quaternion.Euler(0f, 0f, 180f))
                        {
                            TurretRenderer.transform.rotation = Quaternion.identity;
                        }
                    }

                    if (GlowMaskRenderer != null)
                    {
                        float minAlpha = isHighAmmo ? 0.55f : 0.35f;
                        float maxAlpha = isHighAmmo ? 0.95f : 0.65f;
                        float glowAlpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * 3.8f + idleSeed) + 1f) * 0.5f);
                        GlowMaskRenderer.color = new Color(baseGlowColor.r, baseGlowColor.g, baseGlowColor.b, glowAlpha);
                    }
                }
            }
            // 2. PHE BANDIT & CƠ BẢN (CƠ KHÍ CÔNG NGHIỆP, LÒ LỬA & KHÓI)
            else if (faction == "Bandit" || faction == "Base" || bp.StartsWith("NoLienChau") || 
                     bp.StartsWith("MayBanRiuCua") || bp.StartsWith("ThapPhunLua") || 
                     bp.StartsWith("PhaoLaoMocXich") || bp.StartsWith("DaiBacChuaTrom") || 
                     bp.StartsWith("NoXuyenVan"))
            {
                if (isOutOfAmmo)
                {
                    if (TorchRenderer != null)
                    {
                        TorchRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0f);
                        TorchRenderer.transform.localScale = Vector3.zero;
                    }
                    if (TurretRenderer != null && TurretRenderer.gameObject.activeSelf)
                    {
                        TurretRenderer.transform.localPosition = initialTurretLocalPos + new Vector3(0f, -0.06f, 0f);
                        if (currentTarget == null)
                        {
                            TurretRenderer.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                        }
                    }
                    if (ArmTransform != null && ArmTransform != TurretRenderer?.transform)
                    {
                        ArmTransform.localRotation = Quaternion.Euler(0f, 0f, -40f);
                    }
                }
                else if (isLowAmmo)
                {
                    // Than hồng sắp tàn (Dying Embers) + Phụt khói xả định kỳ
                    if (TorchRenderer != null)
                    {
                        float emberFlicker = 0.25f + 0.25f * Mathf.Sin(Time.time * 8f + idleSeed);
                        TorchRenderer.color = new Color(0.95f, 0.22f, 0.05f, emberFlicker);
                        TorchRenderer.transform.localScale = new Vector3(0.26f, 0.26f, 1f) * (0.85f + 0.15f * Mathf.Sin(Time.time * 6f));
                    }

                    diegeticSmokeTimer += Time.deltaTime;
                    if (diegeticSmokeTimer >= 1.4f)
                    {
                        diegeticSmokeTimer = 0f;
                        SpawnDiegeticExhaustPuff();
                    }
                }
                else
                {
                    // Ngọn lửa bập bùng ấm áp
                    if (TorchRenderer != null)
                    {
                        float flameScale = isHighAmmo ? (0.55f + 0.1f * Mathf.Sin(Time.time * 9f + idleSeed)) : (0.42f + 0.06f * Mathf.Sin(Time.time * 7f));
                        TorchRenderer.transform.localScale = new Vector3(flameScale, flameScale, 1f);
                        float alpha = isHighAmmo ? 0.9f : 0.7f;
                        Color flameColor = isHighAmmo ? new Color(1.0f, 0.72f, 0.2f, alpha) : new Color(1.0f, 0.55f, 0.15f, alpha);
                        TorchRenderer.color = flameColor;
                    }

                    if (TurretRenderer != null && TurretRenderer.gameObject.activeSelf)
                    {
                        TurretRenderer.transform.localPosition = initialTurretLocalPos;
                        if (currentTarget == null && TurretRenderer.transform.rotation == Quaternion.Euler(0f, 0f, 180f))
                        {
                            TurretRenderer.transform.rotation = Quaternion.identity;
                        }
                    }
                    if (ArmTransform != null && ArmTransform != TurretRenderer?.transform && ArmTransform.localRotation == Quaternion.Euler(0f, 0f, -40f))
                    {
                        ArmTransform.localRotation = Quaternion.identity;
                    }
                }
            }
            // 3. PHE BIO / MYSTIC (SINH THỂ QUÁI DỊ, NHỊP THỞ CO BÓP & PHÁT QUANG)
            else if (faction == "BioMystic" || bp.StartsWith("UGaiDoc") || bp.StartsWith("TruNamBaoTu") || 
                     bp.StartsWith("OngBomDichAxit") || bp.StartsWith("CotToHutHon") || 
                     bp.StartsWith("CotDiCot"))
            {
                Color bioColor = bp.StartsWith("CotToHutHon") 
                    ? new Color(0.95f, 0.15f, 0.25f, 1f) 
                    : (bp.StartsWith("CotDiCot") ? new Color(0.75f, 0.25f, 0.95f, 1f) : new Color(0.25f, 0.95f, 0.35f, 1f));

                if (isOutOfAmmo)
                {
                    // Héo rũ, xẹp lép, ngừng thở hoàn toàn
                    transform.localScale = new Vector3(initialBaseScale.x * 1.09f, initialBaseScale.y * 0.81f, initialBaseScale.z);
                    if (GlowMaskRenderer != null)
                    {
                        GlowMaskRenderer.color = new Color(bioColor.r * 0.2f, bioColor.g * 0.2f, bioColor.b * 0.2f, 0f);
                    }
                    if (BaseRenderer != null)
                    {
                        BaseRenderer.color = new Color(0.72f, 0.70f, 0.65f, 1f);
                    }
                }
                else if (isLowAmmo)
                {
                    // Nhịp thở đứt quãng hấp hối (Shallow Stutter Breath)
                    float spasm = (Mathf.Sin(Time.time * 7f + idleSeed) > 0.65f) ? 0.05f : -0.02f;
                    transform.localScale = new Vector3(initialBaseScale.x * (1f + spasm), initialBaseScale.y * (1f - spasm), initialBaseScale.z);

                    if (GlowMaskRenderer != null)
                    {
                        float sicklyAlpha = 0.15f + 0.20f * Mathf.Abs(Mathf.Sin(Time.time * 5f));
                        GlowMaskRenderer.color = new Color(bioColor.r * 0.8f, bioColor.g * 0.6f, bioColor.b * 0.3f, sicklyAlpha);
                    }
                    if (BaseRenderer != null)
                    {
                        BaseRenderer.color = new Color(0.88f, 0.85f, 0.78f, 1f);
                    }
                }
                else
                {
                    // Nhịp thở sống động căng tròn (Squash & Stretch), dịch huỳnh quang rực rỡ
                    float breathSpeed = isHighAmmo ? 3.2f : 2.2f;
                    float breathAmp = isHighAmmo ? 0.055f : 0.035f;
                    float breath = Mathf.Sin(Time.time * breathSpeed + idleSeed) * breathAmp;
                    transform.localScale = new Vector3(initialBaseScale.x * (1f + breath), initialBaseScale.y * (1f - breath), initialBaseScale.z);

                    if (GlowMaskRenderer != null)
                    {
                        float alpha = isHighAmmo 
                            ? (0.55f + 0.35f * Mathf.Sin(Time.time * 3.5f + idleSeed)) 
                            : (0.35f + 0.25f * Mathf.Sin(Time.time * 2.5f + idleSeed));
                        GlowMaskRenderer.color = new Color(bioColor.r, bioColor.g, bioColor.b, alpha);
                    }
                    if (BaseRenderer != null)
                    {
                        BaseRenderer.color = Color.white;
                    }
                }
            }
            // 4. PHE BẠCH VÂN (TIÊN ĐẠO TRẬN PHÁP)
            else if (faction == "BachVan" || bp.StartsWith("CuuTieuLoiPhao") || 
                     bp.StartsWith("ThienNhanVongDai") || bp.StartsWith("VanLinhHoiNguyenTran"))
            {
                if (isOutOfAmmo || (BuildingModel != null && BuildingModel.CurrentCooldownRemaining > 0f))
                {
                    if (GlowMaskRenderer != null)
                    {
                        GlowMaskRenderer.color = new Color(1f, 0.88f, 0.35f, 0.15f);
                    }
                }
                else
                {
                    if (GlowMaskRenderer != null)
                    {
                        float holyPulse = 0.65f + 0.35f * Mathf.Sin(Time.time * 4f + idleSeed);
                        GlowMaskRenderer.color = new Color(1.0f, 0.88f, 0.35f, holyPulse);
                    }
                }
            }

            // 5. DIEGETIC WARNING EMOTE (Biểu tượng cảnh báo nhỏ gọn, không dùng chữ số)
            if (warningEmoteObj != null)
            {
                if (isOutOfAmmo)
                {
                    warningEmoteObj.SetActive(true);
                    float blink = Mathf.Abs(Mathf.Sin(Time.time * 5.5f));
                    if (warningEmoteSr != null)
                    {
                        warningEmoteSr.color = new Color(1f, 0.18f, 0.15f, 0.35f + 0.65f * blink);
                    }
                    float bob = Mathf.Sin(Time.time * 6f) * 0.05f;
                    warningEmoteObj.transform.localPosition = new Vector3(0f, 1.55f + bob, 0f);
                }
                else if (isLowAmmo)
                {
                    warningEmoteObj.SetActive(true);
                    float pulse = Mathf.Abs(Mathf.Sin(Time.time * 3.2f));
                    if (warningEmoteSr != null)
                    {
                        warningEmoteSr.color = new Color(1f, 0.68f, 0.15f, 0.25f + 0.55f * pulse);
                    }
                    float bob = Mathf.Sin(Time.time * 3.5f) * 0.035f;
                    warningEmoteObj.transform.localPosition = new Vector3(0f, 1.55f + bob, 0f);
                }
                else
                {
                    warningEmoteObj.SetActive(false);
                }
            }
        }

        private void SpawnDiegeticExhaustPuff()
        {
            var puff = new GameObject("DiegeticExhaustPuff");
            puff.transform.position = transform.position + new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0.75f, 0f);
            var sr = puff.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetSolidWhiteSprite() ?? UISpriteLoader.GetFrameSlotNormal();
            sr.color = new Color(0.25f, 0.25f, 0.28f, 0.7f);
            sr.sortingOrder = (BaseRenderer != null) ? BaseRenderer.sortingOrder + 15 : 350;
            puff.transform.localScale = new Vector3(0.18f, 0.18f, 1f);

            StartCoroutine(DiegeticPuffRoutine(puff, sr));
        }

        private IEnumerator DiegeticPuffRoutine(GameObject puff, SpriteRenderer sr)
        {
            float elapsed = 0f;
            float duration = 0.75f;
            Vector3 startPos = (puff != null) ? puff.transform.position : transform.position;
            Vector3 targetPos = startPos + new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), 0.5f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (puff != null)
                {
                    puff.transform.position = Vector3.Lerp(startPos, targetPos, t);
                    puff.transform.localScale = Vector3.Lerp(new Vector3(0.18f, 0.18f, 1f), new Vector3(0.42f, 0.42f, 1f), t);
                }
                if (sr != null)
                {
                    sr.color = new Color(0.25f, 0.25f, 0.28f, Mathf.Lerp(0.7f, 0f, t));
                }
                yield return null;
            }
            if (puff != null) Destroy(puff);
        }

        // =========================================================================
        // CHARGE-UP & RECOIL SPRING ANIMATIONS
        // =========================================================================

        /// <summary>
        /// Procedural Charge-Up: Rung lắc Jitter + kéo giãn tâm ngắm cho tháp hạng nặng (A5, B5, C5)
        /// </summary>
        private IEnumerator ChargeUpRoutine(float duration, Vector2 dir, Action onComplete)
        {
            isChargingUp = true;
            float elapsed = 0f;
            Vector3 origLocalPos = isAnchorCaptured ? initialTowerLocalPos : transform.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Jitter tăng dần cường độ theo Perlin / Random
                float jitterStrength = Mathf.Lerp(0.02f, 0.08f, progress);
                Vector2 jitterOffset = UnityEngine.Random.insideUnitCircle * jitterStrength;
                transform.localPosition = origLocalPos + (Vector3)jitterOffset;

                // Kéo giãn tâm ngắm (Target stretch)
                if (TurretRenderer != null)
                {
                    float stretch = Mathf.Lerp(1.0f, 1.18f, progress);
                    float squash = Mathf.Lerp(1.0f, 0.88f, progress);
                    TurretRenderer.transform.localScale = new Vector3(initialTurretScale.x * squash, initialTurretScale.y * stretch, initialTurretScale.z);
                }

                yield return null;
            }

            transform.localPosition = origLocalPos;
            if (TurretRenderer != null)
            {
                TurretRenderer.transform.localScale = initialTurretScale;
            }
            isChargingUp = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Procedural Recoil Kickback: Giật lùi ngược hướng bắn và đàn hồi theo Damped Spring (Lò xo giảm chấn)
        /// </summary>
        public void ApplyRecoilKickback(Vector2 shootDir, float kickDistance = 0.22f)
        {
            if (recoilCoroutine != null)
            {
                StopCoroutine(recoilCoroutine);
                recoilCoroutine = null;
                ResetRecoilTransform();
            }
            recoilCoroutine = StartCoroutine(DampedSpringRecoilRoutine(shootDir, kickDistance));
        }

        private void ResetRecoilTransform()
        {
            Transform targetT = (TurretRenderer != null && TurretRenderer.gameObject.activeSelf) 
                ? TurretRenderer.transform 
                : (BaseRenderer != null && BaseRenderer.transform != transform ? BaseRenderer.transform : transform);

            if (targetT == transform)
            {
                transform.localPosition = initialTowerLocalPos;
                transform.localScale = initialBaseScale;
            }
            else if (targetT != null)
            {
                targetT.localPosition = initialTurretLocalPos;
                targetT.localScale = initialTurretScale;
            }
        }

        private IEnumerator DampedSpringRecoilRoutine(Vector2 shootDir, float kickDistance)
        {
            Transform targetT = (TurretRenderer != null && TurretRenderer.gameObject.activeSelf) 
                ? TurretRenderer.transform 
                : (BaseRenderer != null && BaseRenderer.transform != transform ? BaseRenderer.transform : transform);

            Vector3 origLocalPos = (targetT == transform) ? initialTowerLocalPos : initialTurretLocalPos;
            Vector3 origScale = (targetT == transform) ? initialBaseScale : initialTurretScale;

            float time = 0f;
            float duration = 0.42f;
            float angularFreq = 26f; // Tần số dao động đàn hồi
            float decay = 9f;        // Hệ số giảm chấn

            while (time < duration)
            {
                time += Time.deltaTime;

                // Phương trình dao động tắt dần: x(t) = -D * exp(-decay * t) * cos(omega * t)
                float displacement = -kickDistance * Mathf.Exp(-decay * time) * Mathf.Cos(angularFreq * time);
                targetT.localPosition = origLocalPos + (Vector3)shootDir * displacement;

                // Nảy scale nhẹ (Squash & Stretch impulse)
                float scaleImpulse = 0.16f * Mathf.Exp(-decay * time) * Mathf.Sin(angularFreq * time);
                targetT.localScale = new Vector3(origScale.x * (1f + scaleImpulse), origScale.y * (1f - scaleImpulse), origScale.z);

                yield return null;
            }

            targetT.localPosition = origLocalPos;
            targetT.localScale = origScale;
            recoilCoroutine = null;
        }

        // =========================================================================
        // ALIEN TECH SPRITE-SHEET 4-FRAME ANIMATION SEQUENCER
        // =========================================================================

        private Coroutine alienAnimCoroutine = null;

        public void PlayAlienAnimationSequence(string blueprintId, float chargeDur = 0.18f, float fireDur = 0.2f, float coolDur = 0.28f)
        {
            if (BaseRenderer == null) return;
            if (alienAnimCoroutine != null) StopCoroutine(alienAnimCoroutine);
            alienAnimCoroutine = StartCoroutine(AlienAnimationSequenceRoutine(blueprintId, chargeDur, fireDur, coolDur));
        }

        private IEnumerator AlienAnimationSequenceRoutine(string blueprintId, float chargeDur, float fireDur, float coolDur)
        {
            // Frame 1: Charge-up
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetAlienTowerFrame(blueprintId, 1);
            yield return new WaitForSeconds(chargeDur);

            // Frame 2: Fire & Recoil
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetAlienTowerFrame(blueprintId, 2);
            yield return new WaitForSeconds(fireDur);

            // Frame 3: Cooldown & Venting
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetAlienTowerFrame(blueprintId, 3);
            yield return new WaitForSeconds(coolDur);

            // Frame 0: Idle
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetAlienTowerFrame(blueprintId, 0);
            alienAnimCoroutine = null;
        }

        // =========================================================================
        // BANDIT SCRAP TECH SPRITE-SHEET 4-FRAME ANIMATION SEQUENCER & JITTER
        // =========================================================================

        private Coroutine banditAnimCoroutine = null;

        public void PlayBanditAnimationSequence(string blueprintId, float chargeDur = 0.15f, float fireDur = 0.2f, float coolDur = 0.25f)
        {
            if (BaseRenderer == null) return;
            if (banditAnimCoroutine != null) StopCoroutine(banditAnimCoroutine);
            banditAnimCoroutine = StartCoroutine(BanditAnimationSequenceRoutine(blueprintId, chargeDur, fireDur, coolDur));
        }

        private IEnumerator BanditAnimationSequenceRoutine(string blueprintId, float chargeDur, float fireDur, float coolDur)
        {
            // Frame 1: Windup / Tension / Charge
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetBanditTowerFrame(blueprintId, 1);
            yield return new WaitForSeconds(chargeDur);

            // Frame 2: Fire / Snap
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetBanditTowerFrame(blueprintId, 2);
            yield return new WaitForSeconds(fireDur);

            // Frame 3: Cooldown & Reset
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetBanditTowerFrame(blueprintId, 3);
            yield return new WaitForSeconds(coolDur);

            // Frame 0: Idle
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetBanditTowerFrame(blueprintId, 0);
            banditAnimCoroutine = null;
        }

        /// <summary>
        /// Hiệu ứng rung lắc cơ học (Mechanical Jitter) khi nạp đạn (cót két lò xo dây thừng, rung giật ratchet)
        /// </summary>
        public IEnumerator MechanicalJitterRoutine(float duration, float intensity = 0.05f)
        {
            Transform targetT = (TurretRenderer != null && TurretRenderer.gameObject.activeSelf) 
                ? TurretRenderer.transform 
                : (BaseRenderer != null && BaseRenderer.transform != transform ? BaseRenderer.transform : transform);
            Vector3 origLocalPos = (targetT == transform) ? initialTowerLocalPos : initialTurretLocalPos;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float currentIntensity = Mathf.Lerp(intensity * 0.4f, intensity, progress);

                Vector2 offset = UnityEngine.Random.insideUnitCircle * currentIntensity;
                targetT.localPosition = origLocalPos + (Vector3)offset;

                yield return null;
            }

            targetT.localPosition = origLocalPos;
            jitterCoroutine = null;
        }

        /// <summary>
        /// <summary>
        /// Văng mảnh vụn gỗ và đinh rỉ (Wood splinters & rusty nails) khi khai hỏa sử dụng Object Pool
        /// </summary>
        public void SpawnWoodAndNailDebris(Vector3 origin, Vector2 shootDir, int woodCount = 3, int nailCount = 2)
        {
            if (CombatObjectPool.Instance != null)
            {
                for (int i = 0; i < woodCount; i++)
                {
                    Vector2 ejectDir = -shootDir + UnityEngine.Random.insideUnitCircle * 0.6f;
                    Vector3 scale = new Vector3(UnityEngine.Random.Range(0.06f, 0.12f), UnityEngine.Random.Range(0.03f, 0.05f), 1f);
                    CombatObjectPool.Instance.SpawnVFX(origin, UISpriteLoader.GetSolidWhiteSprite(), new Color(0.48f, 0.32f, 0.22f), scale, UnityEngine.Random.Range(0.25f, 0.4f), ejectDir.normalized * UnityEngine.Random.Range(2.5f, 4.5f), UnityEngine.Random.Range(400f, 900f), 345);
                }

                for (int i = 0; i < nailCount; i++)
                {
                    Vector2 ejectDir = Vector2.Perpendicular(shootDir) * (UnityEngine.Random.value > 0.5f ? 1f : -1f) + UnityEngine.Random.insideUnitCircle * 0.4f;
                    CombatObjectPool.Instance.SpawnVFX(origin, UISpriteLoader.GetNailSprite() ?? UISpriteLoader.GetSolidWhiteSprite(), new Color(0.85f, 0.45f, 0.2f), new Vector3(0.35f, 0.35f, 1f), UnityEngine.Random.Range(0.3f, 0.45f), ejectDir.normalized * UnityEngine.Random.Range(3.0f, 5.5f), UnityEngine.Random.Range(400f, 900f), 346);
                }
            }
        }

        // =========================================================================
        // TARGETING & SHOOT DISPATCH
        // =========================================================================

        private void FindNearestTarget2D()
        {
            Collider2D[] hits = (MonsterLayer.value != 0) 
                ? Physics2D.OverlapCircleAll(transform.position, RangeRadius, MonsterLayer)
                : Physics2D.OverlapCircleAll(transform.position, RangeRadius);

            float nearestDistSqr = float.MaxValue;
            Transform best = null;

            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;
                if (col.GetComponent<PlayerCombatController>() != null) continue;
                if (col.GetComponent<Fence2DView>() != null) continue;

                var enemy = col.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive)
                {
                    float dSqr = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
                    if (dSqr < nearestDistSqr)
                    {
                        nearestDistSqr = dSqr;
                        best = col.transform;
                    }
                }
                else if (TargetDummies)
                {
                    var dummy = col.GetComponent<TrainingDummy2DView>();
                    if (dummy != null && dummy.IsAlive)
                    {
                        float dSqr = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
                        if (dSqr < nearestDistSqr)
                        {
                            nearestDistSqr = dSqr;
                            best = col.transform;
                        }
                    }
                }
            }
            currentTarget = best;
        }

        public Transform FindHighestHpTarget2D()
        {
            Collider2D[] hits = (MonsterLayer.value != 0) 
                ? Physics2D.OverlapCircleAll(transform.position, RangeRadius, MonsterLayer)
                : Physics2D.OverlapCircleAll(transform.position, RangeRadius);

            float highestHp = -1f;
            Transform best = null;

            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;
                if (col.GetComponent<PlayerCombatController>() != null) continue;
                if (col.GetComponent<Fence2DView>() != null) continue;

                var enemy = col.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive)
                {
                    if (enemy.CurrentHp > highestHp)
                    {
                        highestHp = enemy.CurrentHp;
                        best = col.transform;
                    }
                }
                else if (TargetDummies)
                {
                    var dummy = col.GetComponent<TrainingDummy2DView>();
                    if (dummy != null && dummy.IsAlive && dummy.CurrentHp > highestHp)
                    {
                        highestHp = dummy.CurrentHp;
                        best = col.transform;
                    }
                }
            }
            return best ?? currentTarget;
        }

        public void TestFireAt(Vector3 targetPos)
        {
            Vector2 dir = ((Vector2)targetPos - (Vector2)transform.position).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

            if (TurretRenderer != null && TurretRenderer.gameObject.activeSelf)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                TurretRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            }

            if (BuildingModel != null && InfiniteAmmoInTest)
            {
                BuildingModel.AmmoCurrent = BuildingModel.AmmoCapacity;
            }

            shootCooldownTimer = BuildingModel != null ? BuildingModel.CooldownSeconds : 1.0f;
            DispatchAttack(dir, targetPos);
            UpdateBarVisuals();
        }

        private void Shoot2D(Vector2 direction)
        {
            if (InfiniteAmmoInTest && BuildingModel != null)
            {
                BuildingModel.AmmoCurrent = BuildingModel.AmmoCapacity;
            }
            BuildingModel.AmmoCurrent = Mathf.Max(0, BuildingModel.AmmoCurrent - BuildingModel.AmmoCostPerShot);
            shootCooldownTimer = BuildingModel.CooldownSeconds;

            Vector3 targetPos = currentTarget != null ? currentTarget.position : (transform.position + (Vector3)direction * 3.5f);
            DispatchAttack(direction, targetPos);

            UpdateBarVisuals();
            Debug.Log($"<color=cyan>[Tháp 2D]</color> {BuildingModel.Name} ({BuildingModel.AttackType}) khai hỏa! Đạn: {BuildingModel.AmmoCurrent}/{BuildingModel.AmmoCapacity}");
        }

        private void DispatchAttack(Vector2 direction, Vector3 targetPos)
        {
            string attackType = BuildingModel != null ? (BuildingModel.AttackType ?? "Direct") : "Direct";

            switch (attackType)
            {
                case "LaserBeam":
                    ExecuteLaserBeam(direction);
                    break;
                case "ParabolicMortar":
                    ExecuteParabolicMortar(targetPos);
                    break;
                case "GravitonTractor":
                    ExecuteGravitonTractor(direction);
                    break;
                case "EMPShockwave":
                    ExecuteEMPShockwave(targetPos);
                    break;
                case "AntimatterMegaBeam":
                    StartCoroutine(AntimatterMegaBeamRoutine(direction));
                    break;
                case "RapidBallistic":
                    StartCoroutine(RapidBallisticRoutine(direction));
                    break;
                case "ParabolicSaw":
                    ExecuteParabolicSaw(targetPos);
                    break;
                case "ConeFlamethrower":
                    ExecuteConeFlamethrower(direction);
                    break;
                case "HarpoonPull":
                    ExecuteHarpoonPull(direction);
                    break;
                case "RecoilHowitzer":
                    ExecuteRecoilHowitzer(targetPos, direction);
                    break;
                case "RadialSpines":
                    ExecuteRadialSpines();
                    break;
                case "ToxicSporeCloud":
                    ExecuteToxicSporeCloud(targetPos);
                    break;
                case "CorrosiveAcidGlob":
                    ExecuteCorrosiveAcidGlob(targetPos);
                    break;
                case "LeechSwarm":
                    ExecuteLeechSwarm(currentTarget ?? transform);
                    break;
                case "AncientGroundSpikes":
                    ExecuteAncientGroundSpikes(targetPos);
                    break;
                case "BachVanLightning":
                    ExecuteBachVanMegaLightning();
                    break;
                case "BachVanVision":
                    ExecuteBachVanVision();
                    break;
                case "BachVanHealing":
                    ExecuteBachVanHealing();
                    break;
                case "WoodSpikeTrap":
                    ExecuteWoodSpikeTrap();
                    break;
                default:
                    ExecuteDirectDamage(direction);
                    break;
            }
        }

        // =========================================================================
        // ARCHETYPE 1: LASER BEAM (A1 - TRỤ PLASMA HỘI TỤ, A5 - MEGA BEAM)
        // =========================================================================

        private void ExecuteLaserBeam(Vector2 direction)
        {
            Vector3 startPos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.6f, 0f);
            Vector3 endPos;

            if (currentTarget != null)
            {
                if (lastLaserTarget != currentTarget)
                {
                    laserRampUpTimer = 0f;
                    lastLaserTarget = currentTarget;
                }

                laserRampUpTimer = Mathf.Min(3.0f, laserRampUpTimer + BuildingModel.CooldownSeconds);
                float rampRatio = laserRampUpTimer / 3.0f;
                float dps = Mathf.Lerp(30f, BuildingModel.SpecialValue > 0f ? BuildingModel.SpecialValue : 90f, rampRatio);
                float damageTick = dps * BuildingModel.CooldownSeconds;

                var dmg = currentTarget.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    dmg.TakeDamage(damageTick, direction, 0.4f);
                }
                endPos = currentTarget.position;
            }
            else
            {
                laserRampUpTimer = 1.5f;
                endPos = startPos + (Vector3)direction * 6.5f;
                if (laserCooldownCoroutine != null) StopCoroutine(laserCooldownCoroutine);
                laserCooldownCoroutine = StartCoroutine(LaserTestPulseRoutine(0.35f));
            }

            float ramp = laserRampUpTimer / 3.0f;
            float baseWidth = Mathf.Lerp(0.08f, 0.28f, ramp);
            Color glowColor = new Color(0.10f, 0.85f, 1f, Mathf.Lerp(0.6f, 1.0f, ramp));
            UpdateDualLayerLaser(startPos, endPos, glowColor, baseWidth);

            SpawnPlasmaSparks(endPos, 4);
            SpawnMeltedPlasmaScorchDecal(endPos, 0.75f, 3.5f);
            ApplyRecoilKickback(direction, 0.05f);

            if (BaseRenderer != null && BuildingModel != null && BuildingModel.BlueprintId == "TruPlasmaHoiTu")
            {
                BaseRenderer.sprite = UISpriteLoader.GetAlienTowerFrame("TruPlasmaHoiTu", 2);
            }
        }

        private IEnumerator LaserTestPulseRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            DisableLaserBeam();
            laserCooldownCoroutine = null;
        }

        private void UpdateDualLayerLaser(Vector3 start, Vector3 end, Color glowColor, float baseWidth)
        {
            if (laserRootObj == null)
            {
                laserRootObj = new GameObject("LaserBeam_DualLayer");
                laserRootObj.transform.SetParent(transform, false);

                var outerObj = new GameObject("OuterGlow");
                outerObj.transform.SetParent(laserRootObj.transform, false);
                outerLaserLr = outerObj.AddComponent<LineRenderer>();
                outerLaserLr.material = new Material(Shader.Find("Sprites/Default"));
                outerLaserLr.sortingOrder = 318;

                var innerObj = new GameObject("InnerCore");
                innerObj.transform.SetParent(laserRootObj.transform, false);
                innerLaserLr = innerObj.AddComponent<LineRenderer>();
                innerLaserLr.material = new Material(Shader.Find("Sprites/Default"));
                innerLaserLr.sortingOrder = 320;
            }

            laserRootObj.SetActive(true);

            // Dao động tần số cao (High-frequency width jitter)
            float jitter = 1f + (Mathf.PerlinNoise(Time.time * 38f, 0f) - 0.5f) * 0.35f;

            // Lớp ngoài (Outer Glow)
            outerLaserLr.enabled = true;
            outerLaserLr.startWidth = baseWidth * 1.6f * jitter;
            outerLaserLr.endWidth = baseWidth * 1.2f * jitter;
            outerLaserLr.startColor = glowColor;
            outerLaserLr.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.45f);
            outerLaserLr.positionCount = 2;
            outerLaserLr.SetPosition(0, start);
            outerLaserLr.SetPosition(1, end);

            // Lớp trong (Inner Core trắng rực)
            innerLaserLr.enabled = true;
            innerLaserLr.startWidth = baseWidth * 0.45f * jitter;
            innerLaserLr.endWidth = baseWidth * 0.35f * jitter;
            innerLaserLr.startColor = Color.white;
            innerLaserLr.endColor = new Color(1f, 1f, 1f, 0.85f);
            innerLaserLr.positionCount = 2;
            innerLaserLr.SetPosition(0, start);
            innerLaserLr.SetPosition(1, end);
        }

        private void DisableLaserBeam()
        {
            if (laserRootObj != null) laserRootObj.SetActive(false);
            laserRampUpTimer = 0f;
            lastLaserTarget = null;

            if (BaseRenderer != null && BuildingModel != null && BuildingModel.BlueprintId == "TruPlasmaHoiTu")
            {
                StartCoroutine(LaserCooldownRoutine());
            }
        }

        private IEnumerator LaserCooldownRoutine()
        {
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetAlienTowerFrame("TruPlasmaHoiTu", 3);
            yield return new WaitForSeconds(0.25f);
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetAlienTowerFrame("TruPlasmaHoiTu", 0);
        }

        // A5: Pháo Đài Phản Vật Chất Khổng Lồ (Antimatter Mega Beam)
        private IEnumerator AntimatterMegaBeamRoutine(Vector2 direction)
        {
            Vector3 startPos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.8f, 0f);

            // Giai đoạn 1: Tụ quả cầu đen tím hút các hạt sáng vào tâm trong 2.5s (Singularity Charge-up)
            CameraShake2D.Instance?.Shake(0.35f, 2.5f);
            var chargeOrb = SpawnGlowOrb(startPos, 2.5f, new Color(0.35f, 0.05f, 0.65f, 0.95f));
            SpawnSingularitySuckVFX(startPos, 2.5f);
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetAlienTowerFrame("PhaoDaiPhanVatChat", 1);

            yield return StartCoroutine(ChargeUpRoutine(2.5f, direction, null));

            if (chargeOrb != null && chargeOrb.gameObject.activeSelf) chargeOrb.gameObject.SetActive(false);

            // Giai đoạn 2: Khai hỏa - phóng cột Mega Beam khổng lồ (rộng 1.5m) xuyên toàn bản đồ
            CameraShake2D.Instance?.Shake(0.85f, 0.65f);
            ApplyRecoilKickback(direction, 0.50f);
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetAlienTowerFrame("PhaoDaiPhanVatChat", 2);

            Vector3 endPos = startPos + (Vector3)direction * 32.0f;
            UpdateDualLayerLaser(startPos, endPos, new Color(0.85f, 0.15f, 1f, 0.95f), 1.5f);

            // Sát thương 1200 xuyên tuyến + Scorch Decal + Vệt nứt năng lượng dọc đường bắn
            DamageEnemiesAlongLine(startPos, endPos, 1.8f, BuildingModel.Damage > 0f ? BuildingModel.Damage : 1200f, direction);
            SpawnEnergyFissureDecalsAlongLine(startPos, endPos, 4.5f);

            yield return new WaitForSeconds(0.55f);
            DisableLaserBeam();

            // Giai đoạn 3: Tản nhiệt (Frame 3: Cooldown) rồi trở về Frame 0: Idle
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetAlienTowerFrame("PhaoDaiPhanVatChat", 3);
            yield return new WaitForSeconds(0.40f);
            if (BaseRenderer != null) BaseRenderer.sprite = UISpriteLoader.GetAlienTowerFrame("PhaoDaiPhanVatChat", 0);
        }

        // =========================================================================
        // ARCHETYPE 2: PARABOLIC MORTAR (A2, B2, C3)
        // =========================================================================

        // A2: Pháo Cối Phân Rã Lượng Tử (Quantum Mortar)
        private void ExecuteParabolicMortar(Vector3 targetPos)
        {
            Vector3 startPos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.65f, 0f);
            Vector2 dir = ((Vector2)targetPos - (Vector2)transform.position).normalized;
            ApplyRecoilKickback(dir, 0.15f);
            PlayAlienAnimationSequence("PhaoCoiLuongTu", 0.18f, 0.22f, 0.28f);

            CombatProjectile2D proj = CombatObjectPool.Instance != null 
                ? CombatObjectPool.Instance.SpawnProjectile() 
                : new GameObject("QuantumMortar_Projectile").AddComponent<CombatProjectile2D>();

            proj.InitializeParabolic(startPos, targetPos, 2.3f, 0.65f, BuildingModel.Damage > 0f ? BuildingModel.Damage : 140f, () =>
            {
                // Nổ quầng sáng AoE (3 ô gạch ~ 3.2m)
                SpawnExplosionVFX(targetPos, 3.2f, new Color(0.65f, 0.25f, 1f));
                DamageEnemiesInRadius(targetPos, 3.2f, BuildingModel.Damage > 0f ? BuildingModel.Damage : 140f);

                // Vùng bức xạ tím nhấp nháy dưới đất trong 4s (+25% sát thương nhận vào)
                SpawnQuantumRadiationZone(targetPos, 3.2f, 4.0f);
            }, Color.white, UISpriteLoader.GetQuantumOrbSprite(), new Vector3(0.85f, 0.85f, 1f), "quantum", new Color(0.75f, 0.25f, 1f));
        }

        // B2: Máy Bắn Rìu Cưa Gai Vụn (Scrap Saw Catapult) - Kéo căng lò xo, bật mạnh văng đạn cưa xoay Parabol + 8 mảnh đinh rỉ gây Bleed
        private void ExecuteParabolicSaw(Vector3 targetPos)
        {
            StartCoroutine(SawCatapultRoutine(targetPos));
        }

        private IEnumerator SawCatapultRoutine(Vector3 targetPos)
        {
            string bpId = BuildingModel?.BlueprintId ?? "MayBanRiuCua";
            Vector3 startPos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.6f, 0f);
            Vector2 dir = ((Vector2)targetPos - (Vector2)transform.position).normalized;

            // 1. ArmTransform kéo căng lò xo về sau (Tension Pull-Back) kèm Mechanical Jitter
            Transform arm = (ArmTransform != null) ? ArmTransform : ((TurretRenderer != null) ? TurretRenderer.transform : transform);
            Quaternion origRot = arm.localRotation;
            float pullDur = 0.18f;
            float elapsed = 0f;
            while (elapsed < pullDur && arm != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pullDur;
                arm.localRotation = origRot * Quaternion.Euler(0f, 0f, -25f * t);
                yield return null;
            }

            // 2. Kích hoạt hoạt ảnh 4-frame sprite sheet
            PlayBanditAnimationSequence(bpId, 0.12f, 0.22f, 0.28f);

            // 3. Arm bật mạnh về trước (Elastic Snap) + dao động tắt dần
            if (arm != null)
            {
                StartCoroutine(CatapultArmSnapRoutine(arm));
            }

            // Văng mảnh vụn gỗ và đinh rỉ kèm giật lùi lò xo mạnh
            SpawnWoodAndNailDebris(startPos, dir, 5, 3);
            ApplyRecoilKickback(dir, 0.22f);

            // 4. Phóng đạn lưỡi cưa xoay tròn (Rotate Z) bay theo quỹ đạo Parabol (tách bóng dưới đất)
            CombatProjectile2D proj = CombatObjectPool.Instance != null 
                ? CombatObjectPool.Instance.SpawnProjectile() 
                : new GameObject("SpinningSaw_Projectile").AddComponent<CombatProjectile2D>();

            proj.InitializeSpinningSaw(startPos, targetPos, 2.2f, 0.60f, BuildingModel.Damage > 0f ? BuildingModel.Damage : 95f, () =>
            {
                CameraShake2D.Instance?.Shake(0.35f, 0.25f);
                SpawnExplosionVFX(targetPos, 2.0f, new Color(0.85f, 0.45f, 0.15f));
            });
        }

        private IEnumerator CatapultArmSnapRoutine(Transform arm)
        {
            float snapTime = 0.05f;
            float elapsed = 0f;
            Quaternion initRot = arm.localRotation;
            Quaternion snapTarget = initRot * Quaternion.Euler(0f, 0f, 35f);

            while (elapsed < snapTime && arm != null)
            {
                elapsed += Time.deltaTime;
                arm.localRotation = Quaternion.Slerp(initRot, snapTarget, elapsed / snapTime);
                yield return null;
            }

            float bounceDur = 0.28f;
            elapsed = 0f;
            while (elapsed < bounceDur && arm != null)
            {
                elapsed += Time.deltaTime;
                float angle = 20f * Mathf.Exp(-9f * elapsed) * Mathf.Cos(28f * elapsed);
                arm.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            if (arm != null) arm.localRotation = Quaternion.identity;
        }

        // C3: Ống Bơm Dịch Nhầy Axit (Corrosive Bile Spitter) - Parabol nảy 2 nhịp vỡ bãi axit sủi bọt
        private void ExecuteCorrosiveAcidGlob(Vector3 targetPos)
        {
            Vector3 startPos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.55f, 0f);
            Vector2 dir = ((Vector2)targetPos - (Vector2)transform.position).normalized;
            ApplyRecoilKickback(dir, 0.12f);

            CombatProjectile2D proj = CombatObjectPool.Instance != null 
                ? CombatObjectPool.Instance.SpawnProjectile() 
                : new GameObject("BouncingAcid_Projectile").AddComponent<CombatProjectile2D>();

            proj.InitializeBouncingAcid(startPos, targetPos, 1.8f, 0.62f, BuildingModel.Damage > 0f ? BuildingModel.Damage : 80f, () =>
            {
                DamageEnemiesInRadius(targetPos, 2.2f, BuildingModel.Damage > 0f ? BuildingModel.Damage : 80f);
            });
        }

        // =========================================================================
        // ARCHETYPE 3: TRACTOR CONE & FLAMETHROWER (A3, B3, C2)
        // =========================================================================

        // A3: Tháp Phát Phản Trọng Lực (Graviton Tractor Pillar) - Quét chùm sáng nón + Hạt bay ngược
        private void ExecuteGravitonTractor(Vector2 direction)
        {
            Vector3 startPos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.85f, 0f);
            PlayAlienAnimationSequence("ThapPhanTrongLuc", 0.15f, 0.45f, 0.28f);
            SpawnConeBeamVFX(startPos, direction, 9.2f, 45f, new Color(0.92f, 0.98f, 0.22f, 0.65f), 0.65f);

            // Spawns Anti-gravity rising particles (Hạt graviton bay ngược lên trời)
            SpawnAntiGravityMotes(startPos, direction, 8.5f, 14);

            var enemies = Physics2D.OverlapCircleAll(transform.position, RangeRadius);
            foreach (var col in enemies)
            {
                var enemy = col.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.ApplySlow(0.35f, 2.0f);
                    if (enemy.IsCarryingPig)
                    {
                        Vector3 pigPos = enemy.transform.position;
                        enemy.DropCarriedPig();
                        SpawnPigRescueWhiteFlash(pigPos);
                        Debug.Log("<color=yellow>[Graviton Tractor]</color> Cắt đứt dây thừng bắt trộm, giải cứu chú heo an toàn!");
                    }
                }
            }
        }

        // B3: Tháp Phun Lửa Hắc Ín (Tar Flamethrower) - Piston thụt thò nhịp nhàng, phun nón lửa 4 ô gạch, vệt than hồng 4 giây
        private void ExecuteConeFlamethrower(Vector2 direction)
        {
            StartCoroutine(TarFlamethrowerRoutine(direction));
        }

        private IEnumerator TarFlamethrowerRoutine(Vector2 direction)
        {
            string bpId = BuildingModel?.BlueprintId ?? "ThapPhunLuaHacIn";
            Vector3 startPos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.5f, 0f);

            // 1. PistonTransform thụt thò nhịp nhàng (Reciprocating Piston Pump)
            Transform piston = (PistonTransform != null) ? PistonTransform : ((TurretRenderer != null) ? TurretRenderer.transform : transform);
            if (piston != null)
            {
                StartCoroutine(PistonPumpRoutine(piston, 0.45f));
            }

            // Rung lắc cơ khí khi nén buồng hắc ín
            yield return StartCoroutine(MechanicalJitterRoutine(0.12f, 0.05f));
            PlayBanditAnimationSequence(bpId, 0.10f, 0.38f, 0.25f);

            // 2. Phụt ParticleSystem / Cone luồng lửa đỏ cam hình nón dài 4 ô gạch (4.5m)
            SpawnBanditFlameConeVFX(startPos, direction, 4.5f, 42f, 0.55f);

            // Văng tàn đinh & dăm gỗ, giật lùi lò xo
            SpawnWoodAndNailDebris(startPos, direction, 3, 2);
            ApplyRecoilKickback(direction, 0.16f);

            // 3. Tạo Decal vệt than hồng bốc khói dưới đất tồn tại 4 giây
            Vector3 burnCenter = startPos + (Vector3)direction * 2.6f;
            SpawnGlowingEmberDecal(burnCenter, 2.6f, 4.0f);

            // 4. Gây sát thương và thiêu đốt quái vật trong luồng nón
            var hits = Physics2D.OverlapCircleAll(transform.position, 4.8f);
            foreach (var col in hits)
            {
                var enemy = col.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive)
                {
                    Vector2 toEnemy = ((Vector2)enemy.transform.position - (Vector2)startPos).normalized;
                    if (Vector2.Angle(direction, toEnemy) <= 35f)
                    {
                        enemy.TakeDamage(BuildingModel.Damage > 0f ? BuildingModel.Damage : 65f, direction, 1.2f);
                    }
                }
            }
        }

        private IEnumerator PistonPumpRoutine(Transform piston, float duration)
        {
            Vector3 initPos = piston.localPosition;
            float elapsed = 0f;
            while (elapsed < duration && piston != null)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float stroke = Mathf.Sin(progress * Mathf.PI * 3f) * 0.18f;
                piston.localPosition = initPos + new Vector3(0f, -stroke, 0f);
                yield return null;
            }
            if (piston != null) piston.localPosition = initPos;
        }

        private void SpawnBanditFlameConeVFX(Vector3 start, Vector2 dir, float length, float coneAngle, float duration)
        {
            var coneObj = new GameObject("VFX_BanditFlameCone");
            coneObj.transform.position = start;
            var sr = coneObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetTractorBeamVFX() ?? UISpriteLoader.GetSolidWhiteSprite();
            sr.color = new Color(1f, 0.38f, 0.08f, 0.95f);
            sr.sortingOrder = 350;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            coneObj.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            coneObj.transform.localScale = new Vector3(length * 0.55f, length, 1f);

            StartCoroutine(FadeAndDestroyVFX(coneObj, sr, duration));

            for (int i = 0; i < 6; i++)
            {
                Vector3 particlePos = start + (Vector3)(dir * UnityEngine.Random.Range(1.0f, length)) + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.6f);
                Color sootColor = (i % 2 == 0) ? new Color(0.2f, 0.2f, 0.2f, 0.75f) : new Color(1f, 0.6f, 0.1f, 0.85f);
                Vector2 flyDir = (Vector2.up + UnityEngine.Random.insideUnitCircle * 0.5f).normalized * 1.8f;

                if (CombatObjectPool.Instance != null)
                {
                    CombatObjectPool.Instance.SpawnVFX(particlePos, UISpriteLoader.GetSoftCircleSprite(), sootColor, new Vector3(0.35f, 0.35f, 1f), 0.45f, flyDir, 0f, 355);
                }
                else
                {
                    var soot = new GameObject("VFX_TarSmoke");
                    soot.transform.position = particlePos;
                    var sootSr = soot.AddComponent<SpriteRenderer>();
                    sootSr.sprite = UISpriteLoader.GetSoftCircleSprite();
                    sootSr.color = sootColor;
                    sootSr.sortingOrder = 355;
                    soot.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
                    StartCoroutine(DebrisFlyRoutine(soot, flyDir.normalized, 1.8f, 0.45f));
                }
            }
        }

        private IEnumerator DebrisFlyRoutine(GameObject obj, Vector2 dir, float speed, float duration)
        {
            float elapsed = 0f;
            var sr = obj != null ? obj.GetComponent<SpriteRenderer>() : null;
            Color initColor = sr != null ? sr.color : Color.white;

            while (elapsed < duration && obj != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                obj.transform.position += (Vector3)(dir * (speed * Time.deltaTime * (1f - t * 0.5f)));
                if (sr != null)
                {
                    sr.color = new Color(initColor.r, initColor.g, initColor.b, Mathf.Lerp(initColor.a, 0f, t));
                }
                yield return null;
            }

            if (obj != null) Destroy(obj);
        }

        private void SpawnGlowingEmberDecal(Vector3 pos, float radius, float duration)
        {
            var decalObj = new GameObject("VFX_TarEmberDecal");
            decalObj.transform.position = pos;
            var sr = decalObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetSoftCircleSprite();
            sr.color = new Color(0.12f, 0.08f, 0.06f, 0.88f);
            sr.sortingOrder = 160;
            decalObj.transform.localScale = new Vector3(radius * 1.6f, radius * 1.0f, 1f);

            for (int i = 0; i < 5; i++)
            {
                Vector3 emberOffset = (Vector3)(UnityEngine.Random.insideUnitCircle * (radius * 0.45f));
                var ember = new GameObject("VFX_EmberSpark");
                ember.transform.SetParent(decalObj.transform, false);
                ember.transform.localPosition = emberOffset;
                var emberSr = ember.AddComponent<SpriteRenderer>();
                emberSr.sprite = UISpriteLoader.GetSolidWhiteSprite();
                emberSr.color = new Color(1f, 0.45f, 0.1f, 0.95f);
                emberSr.sortingOrder = 162;
                ember.transform.localScale = new Vector3(0.08f, 0.08f, 1f);
                StartCoroutine(EmberFlickerRoutine(ember, emberSr, duration));
            }

            StartCoroutine(FadeAndDestroyVFX(decalObj, sr, duration));

            SpawnLingeringZone(pos, radius, duration, new Color(1f, 0.25f, 0.05f, 0.15f), (enemy) =>
            {
                enemy.TakeDamage((BuildingModel != null && BuildingModel.Damage > 0f ? BuildingModel.Damage * 0.3f : 20f) * Time.deltaTime, Vector2.zero, 0f);
            });
        }

        private IEnumerator EmberFlickerRoutine(GameObject ember, SpriteRenderer sr, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && ember != null)
            {
                elapsed += Time.deltaTime;
                float flicker = 0.5f + 0.5f * Mathf.Sin(Time.time * 12f + ember.transform.localPosition.x * 10f);
                if (sr != null) sr.color = new Color(1f, Mathf.Lerp(0.2f, 0.6f, flicker), 0.1f, Mathf.Lerp(0.4f, 1f, flicker));
                yield return null;
            }
        }

        // C2: Trụ Nấm Phun Bào Tử Mù (Toxic Spore Emitter) - Phồng to đầu nấm rồi xì mây bào tử xanh rêu
        private void ExecuteToxicSporeCloud(Vector3 targetPos)
        {
            StartCoroutine(FungalCapPuffRoutine(targetPos));
        }

        private IEnumerator FungalCapPuffRoutine(Vector3 targetPos)
        {
            // Phồng to đầu nấm (Cap Squash & Stretch)
            float puffTime = 0.22f;
            float elapsed = 0f;
            Transform capT = TurretRenderer != null ? TurretRenderer.transform : (BaseRenderer != null ? BaseRenderer.transform : transform);
            Vector3 initScale = capT.localScale;

            while (elapsed < puffTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / puffTime;
                float swell = Mathf.Sin(t * Mathf.PI) * 0.38f;
                capT.localScale = new Vector3(initScale.x * (1f + swell), initScale.y * (1f + swell * 1.3f), initScale.z);
                yield return null;
            }
            capT.localScale = initScale;

            // Xì mây bào tử xanh rêu lơ lửng tồn tại 6s (Quái đi vào hiện icon xoay vòng mù/lảo đảo)
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnGroundZone(targetPos, 3.8f, 6.0f, UISpriteLoader.GetSoftCircleSprite(), new Color(0.20f, 0.72f, 0.28f, 0.55f), (enemy) =>
                {
                    enemy.TakeDamage((BuildingModel.Damage > 0f ? BuildingModel.Damage : 45f) * Time.deltaTime, Vector2.zero, 0f);
                    enemy.ApplySlow(0.35f, 1.0f);
                    enemy.ApplyConfusion(3.0f);
                }, 0.25f, 175);
            }
            else
            {
                SpawnLingeringZone(targetPos, 3.8f, 6.0f, new Color(0.22f, 0.78f, 0.32f, 0.48f), (enemy) =>
                {
                    enemy.TakeDamage((BuildingModel.Damage > 0f ? BuildingModel.Damage : 45f) * Time.deltaTime, Vector2.zero, 0f);
                    enemy.ApplySlow(0.35f, 1.0f);
                    enemy.ApplyConfusion(3.0f);
                });
            }
        }

        // =========================================================================
        // ARCHETYPE 4: HARPOON CHAIN (B4) & LEECH TETHER (C4)
        // =========================================================================

        // B4: Pháo Lao Móc Xích Quỷ (Harpoon Chain Ballista) - Mũi lao sắt, LineRenderer xích, cuộn tời xoay ngược giật lùi 3m trói chân 2s
        private void ExecuteHarpoonPull(Vector2 direction)
        {
            StartCoroutine(HarpoonPullRoutine(direction, currentTarget));
        }

        private IEnumerator HarpoonPullRoutine(Vector2 direction, Transform targetTransform)
        {
            string bpId = BuildingModel?.BlueprintId ?? "PhaoLaoMocXich";
            Vector3 startPos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.55f, 0f);

            // 1. Rung lắc cơ khí khi tời căng hết cỡ
            yield return StartCoroutine(MechanicalJitterRoutine(0.12f, 0.06f));
            PlayBanditAnimationSequence(bpId, 0.12f, 0.25f, 0.30f);

            // 2. Giật lùi mạnh (Spring Recoil) kèm văng dăm gỗ / đinh rỉ
            ApplyRecoilKickback(direction, 0.32f);
            SpawnWoodAndNailDebris(startPos, direction, 4, 3);

            Transform effectiveTarget = targetTransform;
            GameObject tempTargetObj = null;
            if (effectiveTarget == null)
            {
                tempTargetObj = new GameObject("Harpoon_TestHitPoint");
                tempTargetObj.transform.position = startPos + (Vector3)direction * 5.5f;
                effectiveTarget = tempTargetObj.transform;
            }

            // 3. Bắn mũi lao sắt tốc độ cao, LineRenderer vẽ dây xích nối từ tháp đến mục tiêu
            CombatProjectile2D proj = CombatObjectPool.Instance != null 
                ? CombatObjectPool.Instance.SpawnProjectile() 
                : new GameObject("Harpoon_Projectile").AddComponent<CombatProjectile2D>();

            proj.InitializeHarpoon(startPos, effectiveTarget, 18f, BuildingModel != null && BuildingModel.Damage > 0f ? BuildingModel.Damage : 260f, transform, (target, dir) =>
            {
                // Khi trúng: Cuộn tời xoay ngược, kéo căng dây xích
                Transform winch = (WinchTransform != null) ? WinchTransform : ((TurretRenderer != null) ? TurretRenderer.transform : transform);
                if (winch != null)
                {
                    StartCoroutine(WinchReverseReelRoutine(winch, 0.35f));
                }

                if (target != null)
                {
                    var enemy = target.GetComponent<EnemyAgentView>();
                    if (enemy != null && enemy.IsAlive)
                    {
                        Vector2 toTower = ((Vector2)transform.position - (Vector2)enemy.transform.position).normalized;
                        enemy.TakeDamage(BuildingModel != null && BuildingModel.Damage > 0f ? BuildingModel.Damage : 260f, toTower, 3.5f);

                        // Trói chân cứng trong 2 giây (Root 2s) kèm vòng xích khóa dưới chân 2s
                        enemy.ApplySlow(0f, 2.0f);
                        SpawnChainLockDecal(enemy.transform.position, 2.0f);
                        SpawnMetallicMuzzleSparks(enemy.transform.position, toTower, 0.8f);
                        CameraShake2D.Instance?.Shake(0.38f, 0.25f);
                    }
                    else
                    {
                        SpawnMetallicMuzzleSparks(target.position, -direction, 0.6f);
                        SpawnChainLockDecal(target.position, 1.5f);
                    }
                }

                if (tempTargetObj != null) Destroy(tempTargetObj);
            });
        }

        private IEnumerator WinchReverseReelRoutine(Transform winch, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && winch != null)
            {
                elapsed += Time.deltaTime;
                // Cuộn tời xoay ngược tốc độ cao
                winch.Rotate(0f, 0f, -720f * Time.deltaTime);
                yield return null;
            }
        }

        // C4: Cột Tổ Hút Hồn Tầm Huyết (Leech Swarm Obelisk) - Huyết dẫn uốn lượn sóng sin hồi máu rào
        private void ExecuteLeechSwarm(Transform target)
        {
            Vector3 startPos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.85f, 0f);
            Vector3 targetPos = (target != null && target != transform) ? target.position : (startPos + Vector3.down * 4.5f);

            // 1. Phóng đàn bọ phát sáng đỏ bu quanh quái vật
            SpawnLeechSwarmVFX(targetPos, 0.6f);

            // 2. Tìm hàng rào gần nhất để truyền máu
            Fence2DView targetFence = FindNearestDamagedFence(10.0f);
            Vector3 healTargetPos = targetFence != null ? targetFence.transform.position : (transform.position + new Vector3(0.8f, 0f, 0f));

            // 3. Vẽ tia LineRenderer màu đỏ tươi uốn lượn hình sóng sin truyền dòng máu từ quái về hàng rào
            StartCoroutine(UndulatingBloodTetherRoutine(targetPos, healTargetPos, 0.45f));

            if (target != null && target != transform)
            {
                var dmg = target.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    float amount = BuildingModel.Damage > 0f ? BuildingModel.Damage : 60f;
                    dmg.TakeDamage(amount, Vector2.zero, 0f);

                    // Hồi máu cho hàng rào và phát hạt cộng máu xanh lá
                    if (targetFence != null)
                    {
                        targetFence.Repair(amount * 0.75f);
                        SpawnFenceHealingVFX(targetFence.transform.position);
                    }
                    else
                    {
                        Repair(amount * 0.5f);
                    }
                }
            }
            else
            {
                SpawnFenceHealingVFX(healTargetPos);
            }
        }

        private IEnumerator UndulatingBloodTetherRoutine(Vector3 start, Vector3 endPos, float duration)
        {
            var lrObj = new GameObject("BloodTether_Line");
            lrObj.transform.SetParent(transform, false);
            var lr = lrObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 325;
            lr.startWidth = 0.14f;
            lr.endWidth = 0.08f;
            lr.startColor = new Color(0.95f, 0.12f, 0.18f, 0.95f);
            lr.endColor = new Color(0.55f, 0.05f, 0.1f, 0.85f);

            int segments = 20;
            lr.positionCount = segments;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Vector3 tangent = Vector3.Cross((endPos - start).normalized, Vector3.forward);

                for (int i = 0; i < segments; i++)
                {
                    float t = (float)i / (segments - 1);
                    Vector3 linearPos = Vector3.Lerp(start, endPos, t);

                    // Sóng Sin uốn lượn
                    float wave = Mathf.Sin(Time.time * 24f + i * 0.75f) * 0.18f * Mathf.Sin(t * Mathf.PI);
                    Vector3 vertexPos = linearPos + tangent * wave;
                    lr.SetPosition(i, vertexPos);
                }
                yield return null;
            }

            Destroy(lrObj);
        }

        // =========================================================================
        // ARCHETYPE 5: EMP SHOCKWAVE (A4) & SPINE BURST 360 (C1)
        // =========================================================================

        // A4: Trụ Xung Điện EMP Phòng Không - Phóng cầu điện bung vòng xung kích 5m rơi UFO
        private void ExecuteEMPShockwave(Vector3 targetPos)
        {
            Vector3 startPos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.75f, 0f);
            Vector2 dir = ((Vector2)targetPos - (Vector2)transform.position).normalized;
            ApplyRecoilKickback(dir, 0.12f);
            PlayAlienAnimationSequence("TruXungDienEMP", 0.18f, 0.25f, 0.28f);

            CombatProjectile2D proj = CombatObjectPool.Instance != null 
                ? CombatObjectPool.Instance.SpawnProjectile() 
                : new GameObject("EMPOrb_Projectile").AddComponent<CombatProjectile2D>();

            proj.InitializeParabolic(startPos, targetPos, 2.6f, 0.50f, BuildingModel.Damage > 0f ? BuildingModel.Damage : 220f, () =>
            {
                SpawnShockwaveRingVFX(targetPos, 5.0f, 0.45f, new Color(0.28f, 0.88f, 1f));
                SpawnZigZagLightningVFX(targetPos, 5.0f, 8);
                CameraShake2D.Instance?.Shake(0.55f, 0.38f);

                var hits = Physics2D.OverlapCircleAll(targetPos, 5.0f);
                foreach (var col in hits)
                {
                    var enemy = col.GetComponent<EnemyAgentView>();
                    if (enemy != null && enemy.IsAlive)
                    {
                        Vector2 knock = ((Vector2)col.transform.position - (Vector2)targetPos).normalized;
                        enemy.TakeDamage(BuildingModel.Damage > 0f ? BuildingModel.Damage : 220f, knock, 2.5f);

                        // Hiệu ứng vỡ khiên
                        enemy.BreakPlasmaShield();

                        // Kéo UFO rơi xuống đất + stun 2.8s
                        enemy.GroundAirborneUFO();
                    }
                }
            }, Color.white, UISpriteLoader.GetEmpOrbSprite(), new Vector3(0.9f, 0.9f, 1f), "quantum", new Color(0.2f, 0.85f, 1f));
        }

        // C1: Ụ Gai Độc Gặm Nhấm (Vermin Spine Ejector) - Mở nắp kén bắn tỏa 12 mũi gai 360°
        private void ExecuteRadialSpines()
        {
            StartCoroutine(RadialSpineBurstRoutine());
        }

        private IEnumerator RadialSpineBurstRoutine()
        {
            // Mở nắp kén (Pop scale impulse)
            Transform capT = TurretRenderer != null ? TurretRenderer.transform : (BaseRenderer != null ? BaseRenderer.transform : transform);
            Vector3 initScale = capT.localScale;
            capT.localScale = new Vector3(initScale.x * 1.25f, initScale.y * 0.85f, initScale.z);

            Vector3 center = transform.position + new Vector3(0f, 0.35f, 0f);

            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                CombatProjectile2D proj = CombatObjectPool.Instance != null 
                    ? CombatObjectPool.Instance.SpawnProjectile() 
                    : new GameObject($"Spine_{i}").AddComponent<CombatProjectile2D>();

                proj.InitializeLinear(center, dir, BuildingModel.Damage > 0f ? BuildingModel.Damage : 40f, 9.0f, 0.55f, Color.white, UISpriteLoader.GetVerminSpineSprite(), new Vector3(0.65f, 0.85f, 1f), "slime", new Color(0.70f, 0.15f, 0.85f));
                proj.CausesSlimeSlow = true;
                proj.SlimeSlowAmount = 0.30f;
                proj.SlimeSlowDuration = 3.0f;
            }

            yield return new WaitForSeconds(0.12f);
            capT.localScale = initScale;
        }

        // =========================================================================
        // ARCHETYPE 6: GROUND SPIKES (C5 - CỘT DỊ CỐT SƯƠNG HUYỀN)
        // =========================================================================

        private void ExecuteAncientGroundSpikes(Vector3 targetPos)
        {
            StartCoroutine(GroundBoneSpikesRoutine(targetPos));
        }

        private IEnumerator GroundBoneSpikesRoutine(Vector3 targetPos)
        {
            // Bước 1: Hiện vệt nứt đất kèm sương mù tím dưới chân mục tiêu 0.3s
            SpawnFissureWithPurpleMist(targetPos, 0.35f);
            CameraShake2D.Instance?.Shake(0.25f, 0.3f);
            yield return new WaitForSeconds(0.3f);

            // Bước 2: Cọc xương trồi vọt từ dưới đất lên (Scale Y từ 0 lên 1.35 rồi về 1)
            var spikeObj = new GameObject("BoneSpike_Eruption");
            spikeObj.transform.position = targetPos;
            var sr = spikeObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetSpikeVfxSprite() ?? UISpriteLoader.GetSolidWhiteSprite();
            sr.color = new Color(0.92f, 0.88f, 0.78f);
            sr.sortingOrder = 330;

            CameraShake2D.Instance?.Shake(0.45f, 0.35f);

            float animDur = 0.12f;
            float e = 0f;
            while (e < animDur)
            {
                e += Time.deltaTime;
                float t = e / animDur;
                float scaleY = Mathf.Lerp(0f, 1.35f, t);
                spikeObj.transform.localScale = new Vector3(1.2f, scaleY, 1f);
                yield return null;
            }
            spikeObj.transform.localScale = new Vector3(1.2f, 1.0f, 1f);

            // Bước 3: Sát thương 750 + Hất tung lên trời 4.5m + Hiện vòng ấn khóa kỹ năng 3s
            SpawnSilenceSealRingDecal(targetPos, 3.0f);

            var hits = Physics2D.OverlapCircleAll(targetPos, 2.6f);
            foreach (var col in hits)
            {
                var enemy = col.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(BuildingModel.Damage > 0f ? BuildingModel.Damage : 750f, Vector2.up, 4.5f);
                    enemy.ApplySilence(3.0f); // Khóa kỹ năng 3s
                }
            }

            yield return new WaitForSeconds(0.6f);
            StartCoroutine(FadeAndDestroyVFX(spikeObj, sr, 0.25f));
        }

        // =========================================================================
        // ARCHETYPE 7: GATLING (B1) & HEAVY HOWITZER (B5)
        // =========================================================================

        // B1: Nỏ Liên Châu Bọc Thép (Gatling) - Xoay tròn DrumTransform liên tục (0.3s/loạt), phóng 3 mũi tên sắt lệch góc (-6°, 0°, +6°), lóe tia lửa kim loại
        private IEnumerator RapidBallisticRoutine(Vector2 direction)
        {
            string bpId = BuildingModel?.BlueprintId ?? "NoLienChauThep";
            Vector3 muzzlePos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.5f, 0f);

            // 1. Mechanical Jitter nạp đạn (cót két ròng rọc gỗ)
            yield return StartCoroutine(MechanicalJitterRoutine(0.10f, 0.04f));

            // 2. Kích hoạt hoạt ảnh 4 frame
            PlayBanditAnimationSequence(bpId, 0.08f, 0.22f, 0.15f);

            // 3. Xoay tròn DrumTransform liên tục trong 0.3s loạt bắn
            Transform drum = (DrumTransform != null) ? DrumTransform : ((TurretRenderer != null) ? TurretRenderer.transform : transform);
            if (drum != null)
            {
                StartCoroutine(RotateDrumRoutine(drum, 360f, 0.30f));
            }

            // 4. Phóng chùm 3 mũi tên sắt lệch góc nhẹ (-6°, 0°, +6°) kèm vệt gió trắng ngắn
            float[] angleOffsets = new float[] { -6f, 0f, 6f };
            for (int i = 0; i < 3; i++)
            {
                muzzlePos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.5f, 0f);
                Vector2 baseDir = currentTarget != null ? ((Vector2)currentTarget.position - (Vector2)muzzlePos).normalized : direction;

                // Lệch góc nón bắn
                float rad = angleOffsets[i] * Mathf.Deg2Rad;
                Vector2 spreadDir = new Vector2(
                    baseDir.x * Mathf.Cos(rad) - baseDir.y * Mathf.Sin(rad),
                    baseDir.x * Mathf.Sin(rad) + baseDir.y * Mathf.Cos(rad)
                ).normalized;

                // Lóe tia lửa kim loại đầu nòng (Metallic Sparks)
                SpawnMetallicMuzzleSparks(muzzlePos, spreadDir, 0.6f);
                SpawnMuzzleFlash(muzzlePos, spreadDir, new Color(1f, 0.85f, 0.35f), 0.45f);

                // Văng dăm gỗ và đinh rỉ
                SpawnWoodAndNailDebris(muzzlePos, spreadDir, 2, 1);

                // Đạn nỏ sắt bắn thẳng tốc độ cao kèm vệt gió trắng ngắn
                CombatProjectile2D proj = CombatObjectPool.Instance != null 
                    ? CombatObjectPool.Instance.SpawnProjectile() 
                    : new GameObject("ScrapBolt_Bullet").AddComponent<CombatProjectile2D>();

                proj.InitializeLinear(muzzlePos, spreadDir, BuildingModel.Damage > 0f ? BuildingModel.Damage : 18f, 16f, 1.2f, new Color(0.95f, 0.95f, 0.95f), UISpriteLoader.GetIronBoltSprite(), new Vector3(0.45f, 0.45f, 1f), "wind", Color.white);

                // Giật lùi mạnh (Spring Recoil)
                ApplyRecoilKickback(spreadDir, 0.12f);

                yield return new WaitForSeconds(0.10f);
            }
        }

        private IEnumerator RotateDrumRoutine(Transform drum, float totalAngle, float duration)
        {
            float elapsed = 0f;
            float startAngle = drum.localEulerAngles.z;
            while (elapsed < duration && drum != null)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                drum.localRotation = Quaternion.Euler(0f, 0f, startAngle + totalAngle * progress);
                yield return null;
            }
        }

        private void SpawnMetallicMuzzleSparks(Vector3 pos, Vector2 dir, float scale = 0.5f)
        {
            if (CombatObjectPool.Instance != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Color col = (i % 2 == 0) ? new Color(1f, 0.95f, 0.6f) : new Color(1f, 0.7f, 0.2f);
                    Vector2 sparkDir = dir + UnityEngine.Random.insideUnitCircle * 0.4f;
                    CombatObjectPool.Instance.SpawnVFX(pos, UISpriteLoader.GetSolidWhiteSprite(), col, new Vector3(0.06f * scale, 0.06f * scale, 1f), 0.14f, sparkDir.normalized * 4.5f, 0f, 365);
                }
            }
        }

        // B5: Đại Bác Công Thành "Chúa Trộm" (Howitzer) - Nòng phình nhẹ khi nạp, giật lùi cực mạnh (Kickback), phụt khói đen mù mịt, gọi CameraShake, đạn pháo Parabol nổ AoE 4m hất tung 5m
        private void ExecuteRecoilHowitzer(Vector3 targetPos, Vector2 direction)
        {
            StartCoroutine(WarlordHowitzerRoutine(targetPos, direction));
        }

        private IEnumerator WarlordHowitzerRoutine(Vector3 targetPos, Vector2 direction)
        {
            string bpId = BuildingModel?.BlueprintId ?? "DaiBacChuaTrom";
            Vector3 muzzlePos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.75f, 0f);

            // 1. Nòng phình nhẹ khi nạp (Squash & Stretch scale) + Rung cơ học mồi ngòi nổ
            Transform bT = (BarrelTransform != null) ? BarrelTransform : ((TurretRenderer != null) ? TurretRenderer.transform : transform);
            Vector3 initScale = bT.localScale;

            float chargeDur = 0.25f;
            float elapsed = 0f;
            while (elapsed < chargeDur)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / chargeDur;
                float swell = Mathf.Sin(progress * Mathf.PI) * 0.18f;
                bT.localScale = new Vector3(initScale.x * (1f + swell), initScale.y * (1f - swell * 0.6f), initScale.z);
                // Tia lửa ngòi nổ
                SpawnMetallicMuzzleSparks(muzzlePos + new Vector3(0.15f, -0.15f, 0f), Vector2.up, 0.35f);
                yield return null;
            }
            bT.localScale = initScale;

            PlayBanditAnimationSequence(bpId, 0.15f, 0.35f, 0.35f);

            // 2. Khai hỏa: Giật lùi cực mạnh (Kickback 0.55m) + Phụt khói đen mù mịt + Gọi CameraShake
            ApplyRecoilKickback(direction, 0.55f);
            CameraShake2D.Instance?.Shake(0.65f, 0.50f);

            // Phụt khói đen mù mịt & Muzzle Fire Flash
            SpawnDenseBlackSmokeBlast(muzzlePos, direction);
            SpawnMuzzleFlash(muzzlePos, direction, new Color(1f, 0.45f, 0.1f), 1.5f);

            // Văng nhiều dăm gỗ cọc rào & đinh rỉ
            SpawnWoodAndNailDebris(muzzlePos, direction, 8, 5);

            // 3. Quả pháo sắt bay Parabol kèm đuôi khói xám đặc
            CombatProjectile2D proj = CombatObjectPool.Instance != null 
                ? CombatObjectPool.Instance.SpawnProjectile() 
                : new GameObject("WarlordHowitzer_Shell").AddComponent<CombatProjectile2D>();

            proj.InitializeParabolic(muzzlePos, targetPos, 3.2f, 0.75f, BuildingModel.Damage > 0f ? BuildingModel.Damage : 850f, () =>
            {
                // 4. Đập xuống đất nổ AoE 4m, tạo hố đen cháy xém và hất tung (Knockback 5.2m) quái vật
                CameraShake2D.Instance?.Shake(0.75f, 0.60f);
                SpawnExplosionVFX(targetPos, 4.2f, new Color(0.95f, 0.38f, 0.1f));
                SpawnScorchDecal(targetPos, 3.5f, 5.0f);

                var hits = Physics2D.OverlapCircleAll(targetPos, 4.0f);
                foreach (var col in hits)
                {
                    var enemy = col.GetComponent<EnemyAgentView>();
                    if (enemy != null && enemy.IsAlive)
                    {
                        Vector2 knockDir = ((Vector2)col.transform.position - (Vector2)targetPos).normalized;
                        if (knockDir.sqrMagnitude < 0.01f) knockDir = Vector2.up;
                        // Hất tung cực mạnh 5.2m
                        enemy.TakeDamage(BuildingModel.Damage > 0f ? BuildingModel.Damage : 850f, knockDir, 5.2f);
                        // Làm choáng
                        enemy.ApplySlow(0.05f, 2.2f);
                    }
                }
            }, Color.white, UISpriteLoader.GetCannonballSprite(), new Vector3(1.15f, 1.15f, 1f), "smoke", new Color(0.25f, 0.25f, 0.28f));
        }

        private void SpawnDenseBlackSmokeBlast(Vector3 pos, Vector2 dir)
        {
            if (CombatObjectPool.Instance != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector3 smokePos = pos + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.35f);
                    Vector2 smokeDir = dir + UnityEngine.Random.insideUnitCircle * 0.7f;
                    CombatObjectPool.Instance.SpawnVFX(smokePos, UISpriteLoader.GetSoftCircleSprite(), new Color(0.18f, 0.18f, 0.2f, 0.85f), new Vector3(0.5f, 0.5f, 1f), UnityEngine.Random.Range(0.45f, 0.75f), smokeDir.normalized * UnityEngine.Random.Range(2.0f, 3.8f), UnityEngine.Random.Range(-90f, 90f), 358);
                }
            }
        }

        // =========================================================================
        // BẠCH VÂN MEGA LIGHTNING
        // =========================================================================

        public void ExecuteBachVanMegaLightning()
        {
            Debug.Log("<color=yellow><b>⚡ [BẠCH VÂN - CỬU TIÊU LÔI PHÁO]</b> SẤM SÉT THẦN KHÍ KHỞI ĐỘNG!</color>");
            CameraShake2D.Instance?.Shake(0.65f, 0.65f);

            var enemies = FindObjectsByType<EnemyAgentView>();
            int hitCount = 0;

            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    SpawnExplosionVFX(enemy.transform.position, 2.2f, Color.yellow);
                    enemy.TakeDamage(250f, Vector2.up, 2.8f);
                    enemy.ApplySlow(0.2f, 3.5f);
                    hitCount++;
                }
            }

            Debug.Log($"<color=green>⚡ [Lôi Pháo]</color> Đã giáng sấm sét trúng {hitCount} kẻ địch trên toàn bản đồ!");
            UpdateBarVisuals();
        }

        public void ExecuteBachVanVision()
        {
            Vector3 center = transform.position;
            SpawnShockwaveRingVFX(center, 12f, 0.8f, new Color(1f, 0.9f, 0.2f, 0.8f));
            CameraShake2D.Instance?.Shake(0.3f, 0.4f);
            Debug.Log("<color=gold>[Bạch Vân · Thiên Nhãn Vọng Đài]</color> Quét toàn bản đồ, kích hoạt tầm nhìn thần nhãn!");
            PlayerInteractionController.Instance?.ShowFeedback("👁️ THIÊN NHÃN VỌNG ĐÀI: ĐÃ QUÉT VÀ HIỆN THỊ TOÀN BỘ CHIẾN TRƯỜNG!");
            UpdateBarVisuals();
        }

        public void ExecuteBachVanHealing()
        {
            Vector3 center = transform.position;
            SpawnShockwaveRingVFX(center, 15f, 1.0f, new Color(0.2f, 0.95f, 0.4f, 0.8f));
            CameraShake2D.Instance?.Shake(0.35f, 0.4f);
            foreach (var pig in FindObjectsByType<PigAgentView>())
            {
                if (pig != null) SpawnFenceHealingVFX(pig.transform.position);
            }
            foreach (var fence in FindObjectsByType<Fence2DView>())
            {
                if (fence != null)
                {
                    fence.Repair(500f);
                    SpawnFenceHealingVFX(fence.transform.position);
                }
            }
            PlayerCombatController.Instance?.Heal(500f);
            Debug.Log("<color=green>[Bạch Vân · Vạn Linh Hồi Nguyên Trận]</color> Phục hồi toàn thể đàn heo, hàng rào và nông dân!");
            PlayerInteractionController.Instance?.ShowFeedback("🌸 VẠN LINH HỒI NGUYÊN: HỒI PHỤC TOÀN BỘ TRẠI HEO VÀ CÔNG TRÌNH!");
            UpdateBarVisuals();
        }

        public void ExecuteWoodSpikeTrap()
        {
            Vector3 center = transform.position;
            SpawnExplosionVFX(center, 1.8f, new Color(0.6f, 0.4f, 0.2f));
            SpawnWoodAndNailDebris(center, Vector2.up, 6, 4);
            CameraShake2D.Instance?.Shake(0.2f, 0.2f);
            var hits = Physics2D.OverlapCircleAll(center, 2.0f);
            foreach (var col in hits)
            {
                var enemy = col.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(BuildingModel != null && BuildingModel.Damage > 0f ? BuildingModel.Damage : 60f, Vector2.up, 1.5f);
                    enemy.ApplySlow(0.5f, 3.0f);
                }
            }
            UpdateBarVisuals();
        }

        // =========================================================================
        // REUSABLE JUICE & VFX HELPERS (ZERO-ALLOCATION OBJECT POOLING)
        // =========================================================================

        private void SpawnMuzzleFlash(Vector3 pos, Vector2 dir, Color col, float scale = 0.6f)
        {
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnVFX(pos, UISpriteLoader.GetSoftCircleSprite(), col, new Vector3(scale, scale, 1f), 0.06f, Vector2.zero, 0f, 360);
            }
        }

        private void SpawnCasingEjection(Vector3 pos, Vector2 shootDir)
        {
            if (CombatObjectPool.Instance != null)
            {
                Vector2 ejectDir = Vector2.Perpendicular(shootDir) + UnityEngine.Random.insideUnitCircle * 0.3f;
                CombatObjectPool.Instance.SpawnVFX(pos, UISpriteLoader.GetSolidWhiteSprite(), new Color(0.85f, 0.65f, 0.25f), new Vector3(0.08f, 0.04f, 1f), 0.35f, ejectDir.normalized * 3.5f, 720f, 310);
            }
        }

        private void SpawnHitSparks(Vector3 pos, Color color)
        {
            if (CombatObjectPool.Instance != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                    CombatObjectPool.Instance.SpawnVFX(pos, UISpriteLoader.GetSolidWhiteSprite(), color, new Vector3(0.08f, 0.08f, 1f), 0.14f, dir * 4.5f, 0f, 350);
                }
            }
        }

        private void SpawnPlasmaSparks(Vector3 pos, int count = 4)
        {
            if (CombatObjectPool.Instance != null)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                    Color sparkCol = (i % 2 == 0) ? new Color(0.12f, 0.95f, 1f, 0.95f) : Color.white;
                    CombatObjectPool.Instance.SpawnVFX(pos, UISpriteLoader.GetSolidWhiteSprite(), sparkCol, new Vector3(0.08f, 0.08f, 1f), 0.16f, dir * 4.8f, 0f, 360);
                }
            }
        }

        private void SpawnMeltedPlasmaScorchDecal(Vector3 pos, float radius, float duration)
        {
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnGroundZone(pos, radius, duration, UISpriteLoader.GetSoftCircleSprite(), new Color(0.12f, 0.85f, 1f, 0.55f), (enemy) =>
                {
                    enemy.TakeDamage(16f * Time.deltaTime, Vector2.zero, 0f);
                }, 0.25f, 160);

                for (int i = 0; i < 3; i++)
                {
                    Vector3 offset = (Vector3)(UnityEngine.Random.insideUnitCircle * (radius * 0.45f));
                    CombatObjectPool.Instance.SpawnVFX(pos + offset, UISpriteLoader.GetSoftCircleSprite(), new Color(0.2f, 0.95f, 1f, 0.8f), new Vector3(0.12f, 0.12f, 1f), UnityEngine.Random.Range(0.3f, 0.6f), Vector2.up * 0.8f, 0f, 165);
                }
            }
        }

        private void SpawnQuantumRadiationZone(Vector3 center, float radius, float duration)
        {
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnGroundZone(center, radius, duration, UISpriteLoader.GetSoftCircleSprite(), new Color(0.55f, 0.15f, 0.92f, 0.40f), (enemy) =>
                {
                    enemy.TakeDamage(18f * Time.deltaTime, Vector2.zero, 0f);
                    enemy.ApplyVulnerability(1.25f, 1.5f); // +25% sát thương nhận vào
                }, 0.25f, 160);

                for (int i = 0; i < 5; i++)
                {
                    Vector3 offset = (Vector3)(UnityEngine.Random.insideUnitCircle * (radius * 0.55f));
                    CombatObjectPool.Instance.SpawnVFX(center + offset, UISpriteLoader.GetSoftCircleSprite(), new Color(0.85f, 0.35f, 1f, 0.75f), new Vector3(0.15f, 0.15f, 1f), UnityEngine.Random.Range(0.4f, 0.8f), Vector2.up * 1.2f, 0f, 165);
                }
            }
        }

        private void SpawnPigRescueWhiteFlash(Vector3 pos)
        {
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnVFX(pos, UISpriteLoader.GetSoftCircleSprite(), Color.white, new Vector3(1.4f, 1.4f, 1f), 0.22f, Vector2.zero, 0f, 380);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                    CombatObjectPool.Instance.SpawnVFX(pos, UISpriteLoader.GetSolidWhiteSprite(), new Color(1f, 1f, 0.7f), new Vector3(0.08f, 0.08f, 1f), 0.25f, dir * 3.5f, 0f, 375);
                }
            }
        }

        private void SpawnZigZagLightningVFX(Vector3 center, float radius, int count = 8)
        {
            if (CombatObjectPool.Instance != null)
            {
                float step = 360f / count;
                for (int i = 0; i < count; i++)
                {
                    float angle = (i * step + UnityEngine.Random.Range(-10f, 10f)) * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector3 sparkPos = center + (Vector3)(dir * UnityEngine.Random.Range(0.5f, radius * 0.85f));
                    CombatObjectPool.Instance.SpawnVFX(sparkPos, UISpriteLoader.GetVfxLightningStrikeSprite() ?? UISpriteLoader.GetSolidWhiteSprite(), new Color(0.25f, 0.95f, 1f, 0.95f), new Vector3(0.45f, 0.45f, 1f), 0.18f, dir * 2.5f, UnityEngine.Random.Range(-180f, 180f), 370);
                }
            }
        }

        private void SpawnSingularitySuckVFX(Vector3 center, float duration)
        {
            StartCoroutine(SingularitySuckRoutine(center, duration));
        }

        private IEnumerator SingularitySuckRoutine(Vector3 center, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (CombatObjectPool.Instance != null)
                {
                    Vector2 fromCircle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.5f, 2.8f);
                    Vector3 spawnPos = center + (Vector3)fromCircle;
                    Vector2 suckVel = -fromCircle.normalized * 3.6f;
                    CombatObjectPool.Instance.SpawnVFX(spawnPos, UISpriteLoader.GetSoftCircleSprite(), new Color(0.85f, 0.25f, 1f, 0.85f), new Vector3(0.18f, 0.18f, 1f), 0.35f, suckVel, 0f, 360);
                }
                yield return new WaitForSeconds(0.06f);
            }
        }

        private void SpawnEnergyFissureDecalsAlongLine(Vector3 start, Vector3 end, float duration)
        {
            Vector3 diff = end - start;
            float totalDist = diff.magnitude;
            Vector3 dir = diff / totalDist;

            for (float d = 2.0f; d < totalDist; d += 3.5f)
            {
                Vector3 pos = start + dir * d;
                if (CombatObjectPool.Instance != null)
                {
                    CombatObjectPool.Instance.SpawnGroundZone(pos, 1.4f, duration, UISpriteLoader.GetSoftCircleSprite(), new Color(0.65f, 0.15f, 0.95f, 0.55f), (enemy) =>
                    {
                        enemy.TakeDamage(25f * Time.deltaTime, Vector2.zero, 0f);
                    }, 0.25f, 160);
                }
                else
                {
                    SpawnScorchDecal(pos, 1.4f, duration);
                }
            }
        }

        private void SpawnChainLockDecal(Vector3 pos, float duration)
        {
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnGroundZone(pos, 1.1f, duration, UISpriteLoader.GetSoftCircleSprite(), new Color(0.35f, 0.35f, 0.38f, 0.75f), null, 1.0f, 165);
            }
        }

        private Fence2DView FindNearestDamagedFence(float maxDistance = 10.0f)
        {
            Fence2DView best = null;
            float nearestDist = float.MaxValue;
            var fences = FindObjectsByType<Fence2DView>();

            foreach (var f in fences)
            {
                float d = Vector2.Distance(transform.position, f.transform.position);
                if (d <= maxDistance && f.CurrentHp < f.MaxHp && d < nearestDist)
                {
                    nearestDist = d;
                    best = f;
                }
            }

            if (best == null && fences.Length > 0)
            {
                foreach (var f in fences)
                {
                    float d = Vector2.Distance(transform.position, f.transform.position);
                    if (d <= maxDistance && d < nearestDist)
                    {
                        nearestDist = d;
                        best = f;
                    }
                }
            }
            return best;
        }

        private void SpawnLeechSwarmVFX(Vector3 pos, float duration)
        {
            if (CombatObjectPool.Instance != null)
            {
                for (int i = 0; i < 7; i++)
                {
                    Vector3 offset = (Vector3)(UnityEngine.Random.insideUnitCircle * 0.45f);
                    Vector2 swarmVel = UnityEngine.Random.insideUnitCircle * 1.5f;
                    CombatObjectPool.Instance.SpawnVFX(pos + offset, UISpriteLoader.GetSoftCircleSprite(), new Color(0.95f, 0.15f, 0.15f, 0.85f), new Vector3(0.12f, 0.12f, 1f), duration, swarmVel, 0f, 360);
                }
            }
        }

        private void SpawnFenceHealingVFX(Vector3 pos)
        {
            if (CombatObjectPool.Instance != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector3 offset = (Vector3)(UnityEngine.Random.insideUnitCircle * 0.35f);
                    CombatObjectPool.Instance.SpawnVFX(pos + offset, UISpriteLoader.GetSoftCircleSprite(), new Color(0.2f, 0.95f, 0.35f, 0.85f), new Vector3(0.15f, 0.15f, 1f), 0.4f, Vector2.up * 1.8f, 0f, 360);
                }
            }
        }

        private void SpawnFissureWithPurpleMist(Vector3 pos, float duration)
        {
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnGroundZone(pos, 1.2f, duration, UISpriteLoader.GetSoftCircleSprite(), new Color(0.35f, 0.15f, 0.45f, 0.65f), null, 1.0f, 160);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 offset = (Vector3)(UnityEngine.Random.insideUnitCircle * 0.5f);
                    CombatObjectPool.Instance.SpawnVFX(pos + offset, UISpriteLoader.GetSoftCircleSprite(), new Color(0.65f, 0.25f, 0.85f, 0.55f), new Vector3(0.35f, 0.35f, 1f), duration, Vector2.up * 0.8f, 0f, 325);
                }
            }
        }

        private void SpawnSilenceSealRingDecal(Vector3 pos, float duration)
        {
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnGroundZone(pos, 1.5f, duration, UISpriteLoader.GetSoftCircleSprite(), new Color(0.55f, 0.15f, 0.85f, 0.55f), (enemy) =>
                {
                    enemy.ApplySilence(1.0f);
                }, 0.25f, 165);
            }
        }

        private void SpawnScorchDecal(Vector3 pos, float radius, float duration, bool isFlamethrower = false)
        {
            Color col = isFlamethrower ? new Color(0.12f, 0.06f, 0.04f, 0.85f) : new Color(0.1f, 0.1f, 0.1f, 0.75f);
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnGroundZone(pos, radius, duration, UISpriteLoader.GetSoftCircleSprite(), col, null, 1.0f, 160);
            }
            else
            {
                var decalObj = new GameObject("VFX_ScorchDecal");
                decalObj.transform.position = pos;
                var sr = decalObj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetSoftCircleSprite();
                sr.color = col;
                sr.sortingOrder = 160;
                decalObj.transform.localScale = new Vector3(radius, radius * 0.7f, 1f);
                StartCoroutine(FadeAndDestroyVFX(decalObj, sr, duration));
            }
        }

        private void SpawnAntiGravityMotes(Vector3 origin, Vector2 dir, float length, int count)
        {
            if (CombatObjectPool.Instance != null)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = origin + (Vector3)(dir * UnityEngine.Random.Range(1f, length)) + (Vector3)(UnityEngine.Random.insideUnitCircle * 1.2f);
                    CombatObjectPool.Instance.SpawnVFX(pos, UISpriteLoader.GetSoftCircleSprite(), new Color(0.9f, 0.95f, 0.3f, 0.75f), new Vector3(0.15f, 0.15f, 1f), UnityEngine.Random.Range(0.4f, 0.8f), Vector2.up * 2.2f, 0f, 340);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = origin + (Vector3)(dir * UnityEngine.Random.Range(1f, length)) + (Vector3)(UnityEngine.Random.insideUnitCircle * 1.2f);
                    var mote = new GameObject("GravityMote");
                    mote.transform.position = pos;
                    var sr = mote.AddComponent<SpriteRenderer>();
                    sr.sprite = UISpriteLoader.GetSoftCircleSprite();
                    sr.color = new Color(0.9f, 0.95f, 0.3f, 0.75f);
                    sr.sortingOrder = 340;
                    mote.transform.localScale = new Vector3(0.15f, 0.15f, 1f);
                    StartCoroutine(MoteRiseRoutine(mote, UnityEngine.Random.Range(0.4f, 0.8f)));
                }
            }
        }

        private IEnumerator MoteRiseRoutine(GameObject mote, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && mote != null)
            {
                elapsed += Time.deltaTime;
                mote.transform.position += Vector3.up * (2.2f * Time.deltaTime);
                yield return null;
            }
            if (mote != null) Destroy(mote);
        }

        private void ExecuteDirectDamage(Vector2 direction)
        {
            if (BuildingModel != null && BuildingModel.BlueprintId == "BayChongGo")
            {
                ExecuteWoodSpikeTrap();
                return;
            }

            Vector3 startPos = MuzzlePoint != null ? MuzzlePoint.position : transform.position + new Vector3(0f, 0.5f, 0f);
            Vector3 endPos = currentTarget != null ? currentTarget.position : (startPos + (Vector3)direction * 8.0f);
            Vector2 shootDir = ((Vector2)endPos - (Vector2)startPos).normalized;

            CombatProjectile2D proj = CombatObjectPool.Instance != null 
                ? CombatObjectPool.Instance.SpawnProjectile() 
                : new GameObject("Crossbow_Bolt").AddComponent<CombatProjectile2D>();

            proj.InitializeLinear(startPos, shootDir, BuildingModel != null && BuildingModel.Damage > 0f ? BuildingModel.Damage : 35f, 14f, 1.0f, Color.white, UISpriteLoader.GetIronBoltSprite(), new Vector3(0.6f, 0.6f, 1f), "wind", new Color(1f, 1f, 1f, 0.6f));

            SpawnMuzzleFlash(startPos, shootDir, new Color(1f, 0.9f, 0.4f), 0.4f);
            ApplyRecoilKickback(shootDir, 0.12f);
        }

        private void SpawnExplosionVFX(Vector3 pos, float radius, Color color)
        {
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnVFX(pos, UISpriteLoader.GetSmashImpactVfx() ?? UISpriteLoader.GetVfxLightningStrikeSprite() ?? UISpriteLoader.GetSolidWhiteSprite(), color, new Vector3(radius, radius, 1f), 0.38f, Vector2.zero, 0f, 340);
            }
            else
            {
                var obj = new GameObject("VFX_Explosion");
                obj.transform.position = pos;
                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetSmashImpactVfx() ?? UISpriteLoader.GetVfxLightningStrikeSprite() ?? UISpriteLoader.GetSolidWhiteSprite();
                sr.color = color;
                sr.sortingOrder = 340;
                obj.transform.localScale = new Vector3(radius, radius, 1f);
                StartCoroutine(FadeAndDestroyVFX(obj, sr, 0.38f));
            }
        }

        private void SpawnShockwaveRingVFX(Vector3 pos, float maxRadius, float duration, Color color)
        {
            var obj = new GameObject("VFX_ShockwaveRing");
            obj.transform.position = pos;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetSoftCircleSprite();
            sr.color = color;
            sr.sortingOrder = 340;
            StartCoroutine(ExpandAndFadeRing(obj, sr, maxRadius, duration));
        }

        private IEnumerator ExpandAndFadeRing(GameObject obj, SpriteRenderer sr, float maxRadius, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && obj != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float r = Mathf.Lerp(0.5f, maxRadius, t);
                obj.transform.localScale = new Vector3(r, r, 1f);
                if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, (1f - t) * 0.9f);
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }

        private void SpawnConeBeamVFX(Vector3 pos, Vector2 dir, float length, float angleDeg, Color color, float duration)
        {
            var obj = new GameObject("VFX_ConeBeam");
            obj.transform.position = pos;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetTractorBeamVFX() ?? UISpriteLoader.GetSolidWhiteSprite();
            sr.color = color;
            sr.sortingOrder = 310;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            obj.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            obj.transform.localScale = new Vector3(length * 0.45f, length, 1f);
            StartCoroutine(FadeAndDestroyVFX(obj, sr, duration));
        }

        private GameObject SpawnGlowOrb(Vector3 pos, float duration, Color color)
        {
            if (CombatObjectPool.Instance != null)
            {
                var vfx = CombatObjectPool.Instance.SpawnVFX(pos, UISpriteLoader.GetSoftCircleSprite(), color, new Vector3(0.9f, 0.9f, 1f), duration, Vector2.zero, 0f, 350);
                return vfx.gameObject;
            }
            var obj = new GameObject("VFX_GlowOrb");
            obj.transform.position = pos;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetSoftCircleSprite();
            sr.color = color;
            sr.sortingOrder = 350;
            obj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            StartCoroutine(FadeAndDestroyVFX(obj, sr, duration));
            return obj;
        }

        private void SpawnLingeringZone(Vector3 center, float radius, float duration, Color color, Action<EnemyAgentView> onTick)
        {
            if (CombatObjectPool.Instance != null)
            {
                CombatObjectPool.Instance.SpawnGroundZone(center, radius, duration, UISpriteLoader.GetSoftCircleSprite(), color, onTick, 0.25f, 180);
            }
            else
            {
                var zone = new GameObject("LingeringZone");
                zone.transform.position = center;
                var sr = zone.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetSoftCircleSprite();
                sr.color = color;
                sr.sortingOrder = 180;
                zone.transform.localScale = new Vector3(radius * 2f, radius * 1.5f, 1f);
                StartCoroutine(LingeringZoneRoutine(zone, center, radius, duration, onTick));
            }
        }

        private IEnumerator LingeringZoneRoutine(GameObject zone, Vector3 center, float radius, float duration, Action<EnemyAgentView> onTick)
        {
            float elapsed = 0f;
            while (elapsed < duration && zone != null)
            {
                elapsed += Time.deltaTime;
                var hits = Physics2D.OverlapCircleAll(center, radius);
                foreach (var col in hits)
                {
                    var enemy = col.GetComponent<EnemyAgentView>();
                    if (enemy != null && enemy.IsAlive)
                    {
                        onTick?.Invoke(enemy);
                    }
                }
                yield return null;
            }
            if (zone != null) Destroy(zone);
        }

        private void DamageEnemiesInRadius(Vector3 center, float radius, float damage)
        {
            var hits = Physics2D.OverlapCircleAll(center, radius);
            foreach (var col in hits)
            {
                var enemy = col.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(damage, (col.transform.position - center).normalized, 2.2f);
                }
                else
                {
                    var dummy = col.GetComponent<TrainingDummy2DView>();
                    if (dummy != null && dummy.IsAlive)
                    {
                        dummy.TakeDamage(damage, (col.transform.position - center).normalized, 2.2f);
                    }
                }
            }
        }

        private void DamageEnemiesAlongLine(Vector3 start, Vector3 end, float width, float damage, Vector2 knockDir)
        {
            var enemies = FindObjectsByType<EnemyAgentView>();
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    float d = DistancePointToLine(start, end, enemy.transform.position);
                    if (d <= width)
                    {
                        enemy.TakeDamage(damage, knockDir, 5.0f);
                    }
                }
            }

            var dummies = FindObjectsByType<TrainingDummy2DView>();
            foreach (var dummy in dummies)
            {
                if (dummy != null && dummy.IsAlive)
                {
                    float d = DistancePointToLine(start, end, dummy.transform.position);
                    if (d <= width)
                    {
                        dummy.TakeDamage(damage, knockDir, 5.0f);
                    }
                }
            }
        }

        private float DistancePointToLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
        {
            Vector3 line = lineEnd - lineStart;
            float len = line.magnitude;
            if (len <= 0.001f) return Vector3.Distance(lineStart, point);
            Vector3 dir = line / len;
            float proj = Vector3.Dot(point - lineStart, dir);
            proj = Mathf.Clamp(proj, 0f, len);
            Vector3 closest = lineStart + dir * proj;
            return Vector3.Distance(closest, point);
        }

        private IEnumerator FadeAndDestroyVFX(GameObject obj, SpriteRenderer sr, float duration)
        {
            float elapsed = 0f;
            Color initCol = sr != null ? sr.color : Color.white;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (sr != null)
                {
                    float alpha = (1f - (elapsed / duration)) * initCol.a;
                    sr.color = new Color(initCol.r, initCol.g, initCol.b, alpha);
                }
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }

        // =========================================================================
        // HEALTH, AMMO, REPAIR & FLOATING BARS
        // =========================================================================

        public void ReloadAmmo(int amount)
        {
            if (BuildingModel == null) return;
            int space = BuildingModel.AmmoCapacity - BuildingModel.AmmoCurrent;
            int add = Mathf.Min(space, amount);
            BuildingModel.AmmoCurrent += add;
            UpdateBarVisuals();
            Debug.Log($"<color=green>[Nạp Đạn]</color> Đã nạp {add} {BuildingModel.AmmoName}! Hiện có: {BuildingModel.AmmoCurrent}/{BuildingModel.AmmoCapacity}");
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection = default, float knockbackForce = 0f)
        {
            if (!IsAlive) return;

            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            if (BuildingModel != null)
            {
                BuildingModel.CurrentHp = (int)CurrentHp;
                if (CurrentHp <= 0f)
                {
                    BuildingModel.IsBroken = true;
                }
            }

            UpdateDamageVisual();
            UpdateBarVisuals();
        }

        public void Repair(float amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            if (BuildingModel != null)
            {
                BuildingModel.CurrentHp = (int)CurrentHp;
                if (CurrentHp > 0f)
                {
                    BuildingModel.IsBroken = false;
                }
            }
            UpdateDamageVisual();
            UpdateBarVisuals();
        }

        private void EnsureFloatingBars()
        {
            if (healthBarRoot != null) return;

            healthBarRoot = new GameObject("TowerFloatingBars");
            healthBarRoot.transform.SetParent(transform, false);
            healthBarRoot.transform.localPosition = new Vector3(0f, 1.35f, 0f);

            var whiteSp = UISpriteLoader.GetSolidWhiteSprite();

            var bgObj = new GameObject("BarBg");
            bgObj.transform.SetParent(healthBarRoot.transform, false);
            var bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = whiteSp;
            bgSr.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);
            bgSr.sortingOrder = (BaseRenderer != null) ? BaseRenderer.sortingOrder + 20 : 250;
            bgObj.transform.localScale = new Vector3(0.85f, 0.05f, 1f);

            var hpFillObj = new GameObject("HpFill");
            hpFillObj.transform.SetParent(healthBarRoot.transform, false);
            hpFillObj.transform.localPosition = Vector3.zero;
            var hpSr = hpFillObj.AddComponent<SpriteRenderer>();
            hpSr.sprite = whiteSp;
            hpSr.color = new Color(0.2f, 0.88f, 0.35f, 0.95f);
            hpSr.sortingOrder = bgSr.sortingOrder + 1;
            hpFillObj.transform.localScale = new Vector3(0.81f, 0.035f, 1f);
            healthBarFill = hpFillObj.transform;

            // Thanh Đạn: Ẩn hoàn toàn vì trạng thái đạn đã được thể hiện diegetic qua lõi, lửa, khói và tư thế nòng tháp
            var ammoFillObj = new GameObject("AmmoFill");
            ammoFillObj.transform.SetParent(healthBarRoot.transform, false);
            ammoBarFill = ammoFillObj.transform;
            ammoFillObj.SetActive(false);

            UpdateBarVisuals();
        }

        private void UpdateBarVisuals()
        {
            if (healthBarRoot != null)
            {
                if (!ShowFloatingBars)
                {
                    healthBarRoot.SetActive(false);
                    return;
                }

                if (OnlyShowBarsWhenDamaged)
                {
                    bool isDamaged = CurrentHp < MaxHp;
                    // Chỉ hiển thị thanh máu khi tháp bị tổn thương
                    healthBarRoot.SetActive(isDamaged);
                }
                else
                {
                    healthBarRoot.SetActive(true);
                }
            }

            if (healthBarFill != null && MaxHp > 0f)
            {
                float hpRatio = Mathf.Clamp01(CurrentHp / MaxHp);
                healthBarFill.localScale = new Vector3(0.81f * hpRatio, 0.035f, 1f);
            }

            if (ammoBarFill != null && ammoBarFill.gameObject.activeSelf)
            {
                ammoBarFill.gameObject.SetActive(false);
            }
        }

        private void UpdateDamageVisual()
        {
            if (smokeEffectObj == null)
            {
                smokeEffectObj = new GameObject("TowerDamageSmoke");
                smokeEffectObj.transform.SetParent(transform, false);
                smokeEffectObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);

                smokeSr = smokeEffectObj.AddComponent<SpriteRenderer>();
                smokeSr.sprite = UISpriteLoader.GetSolidWhiteSprite();
                smokeSr.sortingOrder = (BaseRenderer != null) ? BaseRenderer.sortingOrder + 1 : 220;
                smokeEffectObj.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
                smokeEffectObj.SetActive(false);
            }

            if (!IsAlive)
            {
                smokeEffectObj.SetActive(true);
                if (smokeSr != null) smokeSr.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
                if (BaseRenderer != null) BaseRenderer.color = new Color(0.35f, 0.35f, 0.35f, 0.7f);
                if (TurretRenderer != null) TurretRenderer.gameObject.SetActive(false);
                if (GlowMaskRenderer != null) GlowMaskRenderer.gameObject.SetActive(false);
            }
            else if (CurrentHp < MaxHp * 0.5f)
            {
                smokeEffectObj.SetActive(true);
                if (smokeSr != null) smokeSr.color = new Color(0.4f, 0.4f, 0.4f, 0.75f);
                if (BaseRenderer != null) BaseRenderer.color = new Color(0.9f, 0.6f, 0.45f, 1f);
                if (TurretRenderer != null && BuildingModel?.BlueprintId != "CuuTieuLoiPhao") TurretRenderer.gameObject.SetActive(true);
            }
            else
            {
                smokeEffectObj.SetActive(false);
                if (BaseRenderer != null) BaseRenderer.color = Color.white;
                if (TurretRenderer != null && BuildingModel?.BlueprintId != "CuuTieuLoiPhao") TurretRenderer.gameObject.SetActive(true);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, RangeRadius);
        }
    }
}
