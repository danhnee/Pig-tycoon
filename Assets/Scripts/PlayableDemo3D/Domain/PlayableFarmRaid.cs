using System;
using System.Collections.Generic;
using PigTycoon.Core;

namespace PigTycoon.PlayableDemo3D
{
    [Serializable]
    public sealed class FarmFenceState
    {
        public string Id = "";
        public float X, Y;
        public float Hp = 300f;
        public float Radius = 0.5f;
        public bool HasBoxFootprint;
        public float BoxHalfX, BoxHalfY, BoxYawRadians;
        public float DistanceSquaredToSurface(float worldX, float worldY)
        {
            float dx = worldX - X, dy = worldY - Y;
            if (!HasBoxFootprint)
            {
                float gap = Math.Max(0f, (float)Math.Sqrt(dx * dx + dy * dy) - Radius);
                return gap * gap;
            }
            float cos = (float)Math.Cos(BoxYawRadians), sin = (float)Math.Sin(BoxYawRadians);
            float gapX = Math.Max(0f, Math.Abs(dx * cos + dy * sin) - BoxHalfX);
            float gapY = Math.Max(0f, Math.Abs(-dx * sin + dy * cos) - BoxHalfY);
            return gapX * gapX + gapY * gapY;
        }
    }

    public sealed class FarmRaidEnemy
    {
        public EnemyData Model = null!;
        public float X, Y, SpawnX, SpawnY;
        public float Cooldown, Telegraph, PinSeconds;
        public string CapturedPigId = "";
        public float CaptureSeparation;
        public bool Escaped, TelegraphingPlayer;
        public bool MovedThisStep;
        public int TelegraphSequence, AttackSequence, HitSequence, BindSequence;
        public bool Resolved => !Model.IsAlive || Escaped;
    }

    /// <summary>
    /// Documented slice composition: one 150HP thief + one 120HP crawler (12+15 threat).
    /// Supports Dao Rua, Bua Ta, and Giao Tre melee weapons, 3-step combo, Martial Skills, and Active Guard/Parry.
    /// </summary>
    public sealed class PlayableFarmRaid
    {
        private readonly PlayableFarmSession session;
        private readonly List<FarmRaidEnemy> enemies = new List<FarmRaidEnemy>();
        private readonly Dictionary<string, float> towerTimers = new Dictionary<string, float>();
        private readonly Dictionary<string, float> initialTowerDurability = new Dictionary<string, float>();
        private readonly Dictionary<string, float> initialFenceHp = new Dictionary<string, float>();
        private readonly Dictionary<FlowDestinationGroup, (int gx, int gy)> targetCells = new Dictionary<FlowDestinationGroup, (int gx, int gy)>();
        private HerdFlowField? navigation;
        private float attackCooldown, secondsSinceAction = 10f;
        private float parryWindowRemaining;
        private int comboStep;
        private float comboWindowRemaining;
        private int kills, pigsLost, goldEarned, ammoSpent, startFaints, waveNumber;
        public IReadOnlyList<FarmRaidEnemy> Enemies => enemies;
        public bool Active { get; private set; }
        public bool Paused { get; set; }
        public bool HeavyWeaponEquipped => Engine.Character.Equipment.TryGetValue(EquipSlot.Weapon, out var item) && item?.Id == "wpn_bua_ta";
        public WaveBattleReport? LastReport { get; private set; }
        public event Action? PlayerRescued;
        public event Action<string>? TowerFired;
        public Func<string, (float x, float y)?>? TowerPosition;
        public Func<string, string, IReadOnlyList<string>>? TowerPierceTargets;
        public float ExactTowerWearSpent { get; private set; }
        public float FenceHpLost { get; private set; }
        public int CurrentComboStep => comboStep;
        public bool IsParrying => parryWindowRemaining > 0f;

        public PlayableFarmRaid(PlayableFarmSession session) { this.session = session; }
        private GameEngine Engine => session.Engine;
        private MapRect Map => Engine.PlayableMap?.MapBounds ?? FarmMainLayout.MapBounds;

        public FarmActionResult TryStartScheduledMinor(float spawnX, float spawnY, Func<float, float, bool> isVisible)
        {
            if (Paused || Active || Engine.Clock.IsDuringCombatWave) return new FarmActionResult(false, "Chưa thể bắt đầu đợt quái.");
            if (!session.State.PendingRaid && !Engine.Clock.IsMinorWaveTriggered) return new FarmActionResult(false, "Chưa có đợt quấy phá đến hạn.");
            int majorBudget = EnemyData.CalculateWaveThreatBudget(Engine.Clock.CycleNumber, Engine.Pigs.FindAll(p => p.IsAlive).Count);
            const int scenarioThreat = 12 + 15;
            if (scenarioThreat < majorBudget * 0.15f || scenarioThreat > majorBudget * 0.30f)
                return new FarmActionResult(false, "Cấu hình đợt nhỏ này không phù hợp ngân sách hiện tại.");
            if (isVisible == null || isVisible(spawnX, spawnY) || !Map.Contains(spawnX, spawnY))
                return new FarmActionResult(false, "Chưa có điểm vào ngoài tầm nhìn.");
            if (Squared(spawnX, spawnY, session.PlayerX, session.PlayerY) < 625f)
                return new FarmActionResult(false, "Điểm vào quá gần người chơi.");
            foreach (var pig in Engine.Pigs) if (pig.IsAlive && Squared(spawnX, spawnY, pig.PositionX, pig.PositionY) < 625f)
                return new FarmActionResult(false, "Điểm vào quá gần đàn heo.");
            foreach (var structure in Engine.GridZones.Structures)
            {
                var c = structure.WorldCenter(Engine.GridZones.OriginX, Engine.GridZones.OriginY);
                if (Squared(spawnX, spawnY, c.wx, c.wy) < 625f) return new FarmActionResult(false, "Điểm vào quá gần công trình.");
            }
            foreach (var fence in session.State.Fences)
                if (fence.Hp > 0 && Squared(spawnX, spawnY, fence.X, fence.Y) < 625f)
                    return new FarmActionResult(false, "Điểm vào quá gần rào.");
            foreach (var point in Engine.GridZones.SceneServicePoints)
                if (Squared(spawnX, spawnY, point.X, point.Y) < 625f)
                    return new FarmActionResult(false, "Điểm vào quá gần điểm phục vụ.");
            if (TowerPosition != null)
                foreach (var building in Engine.Defense.Buildings)
                {
                    var position = TowerPosition(building.InstanceId);
                    if (position.HasValue && Squared(spawnX, spawnY, position.Value.x, position.Value.y) < 625f)
                        return new FarmActionResult(false, "Điểm vào quá gần công trình phòng thủ.");
                }
            enemies.Clear(); towerTimers.Clear(); initialTowerDurability.Clear(); initialFenceHp.Clear();
            foreach (var tower in Engine.Defense.Buildings) initialTowerDurability[tower.InstanceId] = EffectiveTowerDurability(tower);
            foreach (var fence in session.State.Fences) initialFenceHp[fence.Id] = fence.Hp;
            enemies.Add(new FarmRaidEnemy { Model = EnemyData.CreateKeBatHeo(), X = spawnX, Y = spawnY, SpawnX = spawnX, SpawnY = spawnY });
            enemies.Add(new FarmRaidEnemy { Model = EnemyData.CreateKeCaoPha(), X = spawnX, Y = spawnY, SpawnX = spawnX, SpawnY = spawnY });
            navigation = new HerdFlowField(Engine.FlowField.OriginX, Engine.FlowField.OriginY, Engine.FlowField.Width, Engine.FlowField.Height);
            RebuildObstacles();
            kills = pigsLost = goldEarned = ammoSpent = 0; startFaints = Engine.Character.FaintCount;
            waveNumber = Engine.DeepMechanics.BattleReports.Count + 1;
            Active = true; session.State.PendingRaid = false; Engine.Clock.IsMinorWaveTriggered = false;
            Engine.EventBus.Publish(new WaveStarted(waveNumber, scenarioThreat, false, Engine.Clock.CurrentDay));
            return new FarmActionResult(true, "Đạo tặc đã vào trại. Giữ heo và bảo vệ rào!");
        }

        public FarmActionResult ForceStartOpenWorldSkirmish(float spawnX, float spawnY)
        {
            if (Paused) return new FarmActionResult(false, "Đang tạm dừng.");
            if (Active) return new FarmActionResult(true, "Đợt quái thực chiến đang diễn ra!");
            enemies.Clear(); towerTimers.Clear(); initialTowerDurability.Clear(); initialFenceHp.Clear();
            foreach (var tower in Engine.Defense.Buildings) initialTowerDurability[tower.InstanceId] = EffectiveTowerDurability(tower);
            foreach (var fence in session.State.Fences) initialFenceHp[fence.Id] = fence.Hp;
            enemies.Add(new FarmRaidEnemy { Model = EnemyData.CreateKeBatHeo(), X = spawnX, Y = spawnY, SpawnX = spawnX, SpawnY = spawnY });
            enemies.Add(new FarmRaidEnemy { Model = EnemyData.CreateKeCaoPha(), X = spawnX + 1.8f, Y = spawnY - 1.2f, SpawnX = spawnX, SpawnY = spawnY });
            navigation = new HerdFlowField(Engine.FlowField.OriginX, Engine.FlowField.OriginY, Engine.FlowField.Width, Engine.FlowField.Height);
            RebuildObstacles();
            kills = pigsLost = goldEarned = ammoSpent = 0; startFaints = Engine.Character.FaintCount;
            waveNumber = Engine.DeepMechanics.BattleReports.Count + 1;
            Active = true; session.State.PendingRaid = false; Engine.Clock.IsMinorWaveTriggered = false;
            Engine.EventBus.Publish(new WaveStarted(waveNumber, 27, false, Engine.Clock.CurrentDay));
            return new FarmActionResult(true, "Triệu hồi đợt quái thực chiến! Kẻ Bắt Heo & Kẻ Cào Phá đã xuất hiện!");
        }

        public void RebuildObstacles()
        {
            if (navigation == null) return;
            for (int x = 0; x < navigation.Width; x++) for (int y = 0; y < navigation.Height; y++)
                navigation.SetBlocked(x, y, Engine.FlowField.IsBlocked(x, y));
            targetCells.Clear();
        }

        public void Advance(float seconds, Func<float, float, float, float, bool> canTraverse)
        {
            if (parryWindowRemaining > 0f) parryWindowRemaining = Math.Max(0f, parryWindowRemaining - seconds);
            if (comboWindowRemaining > 0f)
            {
                comboWindowRemaining = Math.Max(0f, comboWindowRemaining - seconds);
                if (comboWindowRemaining <= 0f) comboStep = 0;
            }
            if (!Active || Paused || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            float remaining = Math.Min(5f, seconds);
            while (remaining > 0 && Active)
            {
                float dt = Math.Min(0.05f, remaining); remaining -= dt;
                attackCooldown = Math.Max(0, attackCooldown - dt); secondsSinceAction += dt;
                Engine.Character.TickCombatRegen(dt, true, secondsSinceAction);
                foreach (var enemy in enemies) if (!enemy.Resolved) AdvanceEnemy(enemy, dt, canTraverse);
                AdvanceTowers(dt);
                if (enemies.TrueForAll(e => e.Resolved)) Finish();
            }
        }

        private void AdvanceEnemy(FarmRaidEnemy enemy, float dt, Func<float, float, float, float, bool> canTraverse)
        {
            enemy.MovedThisStep = false;
            enemy.Cooldown = Math.Max(0f, enemy.Cooldown - dt);
            if (enemy.PinSeconds > 0f) { enemy.PinSeconds -= dt; return; }
            if (!string.IsNullOrEmpty(enemy.CapturedPigId))
            {
                var pig = session.GetPig(enemy.CapturedPigId);
                if (pig == null) { Release(enemy); return; }
                var exit = ExitPoint(enemy);
                float previousX = enemy.X, previousY = enemy.Y;
                Move(enemy, exit.x, exit.y, dt, enemy.Model.MoveSpeed * 1.25f, FlowDestinationGroup.GateExit, canTraverse);
                float dx = enemy.X - pig.PositionX, dy = enemy.Y - pig.PositionY;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                if (distance > enemy.CaptureSeparation && distance > 0f)
                {
                    float pull = distance - enemy.CaptureSeparation;
                    float x = pig.PositionX + dx / distance * pull, y = pig.PositionY + dy / distance * pull;
                    if (!CanTraverseNavigation(pig.PositionX, pig.PositionY, x, y) || !canTraverse(pig.PositionX, pig.PositionY, x, y))
                    { enemy.X = previousX; enemy.Y = previousY; enemy.MovedThisStep = false; return; }
                    pig.PositionX = x; pig.PositionY = y;
                }
                if (!Map.Contains(pig.PositionX, pig.PositionY))
                {
                    Engine.Pigs.Remove(pig); Engine.Herd.OnPigRemovedFromHerd(pig, Engine.Pigs, true, out _, out _, out _);
                    pigsLost++; Release(enemy); enemy.Escaped = true;
                }
                return;
            }
            if (enemy.Telegraph > 0f)
            {
                enemy.Telegraph -= dt;
                if (enemy.Telegraph <= 0f) enemy.AttackSequence++;
                if (enemy.Telegraph <= 0f && enemy.TelegraphingPlayer &&
                    Squared(enemy.X, enemy.Y, session.PlayerX, session.PlayerY) <= Square(enemy.Model.AttackRange + 0.35f))
                {
                    bool parried = parryWindowRemaining > 0f;
                    Engine.Character.ProcessIncomingAttack(enemy.Model.AttackDamage, parried);
                    if (parried)
                    {
                        enemy.PinSeconds = 1.2f;
                        Damage(enemy, 18f);
                    }
                    if (Engine.Character.IsFainted) { Engine.ExecutePlayerFaintRescue(); PlayerRescued?.Invoke(); }
                }
                return;
            }
            if (enemy.Cooldown <= 0 && Squared(enemy.X, enemy.Y, session.PlayerX, session.PlayerY) <= Square(enemy.Model.AttackRange + 0.35f))
            {
                enemy.Telegraph = 0.18f; enemy.TelegraphingPlayer = true; enemy.Cooldown = enemy.Model.AttackCooldown;
                enemy.TelegraphSequence++;
                return;
            }
            if (enemy.Model.TypeId == EnemyTypeId.KeBatHeo)
            {
                Pig? target = null;
                foreach (var pig in Engine.Pigs)
                    if (pig.IsAlive && !pig.IsIsolated && !session.ExternallyDrivenPigIds.Contains(pig.Id) &&
                        (target == null || pig.AdhesionScore < target.AdhesionScore)) target = pig;
                if (target == null) { Retreat(enemy, dt, canTraverse); return; }
                if (Squared(enemy.X, enemy.Y, target.PositionX, target.PositionY) <= Square(enemy.Model.AttackRange))
                {
                    enemy.CapturedPigId = target.Id;
                    enemy.CaptureSeparation = (float)Math.Sqrt(Squared(enemy.X, enemy.Y, target.PositionX, target.PositionY));
                    enemy.BindSequence++; session.SetPigExternallyDriven(target.Id, true); return;
                }
                Move(enemy, target.PositionX, target.PositionY, dt, enemy.Model.MoveSpeed, FlowDestinationGroup.Feeder, canTraverse);
            }
            else
            {
                FarmFenceState? target = null; float nearest = float.PositiveInfinity;
                foreach (var fence in session.State.Fences)
                {
                    float distance = Squared(enemy.X, enemy.Y, fence.X, fence.Y);
                    if (fence.Hp > 0 && distance < nearest) { target = fence; nearest = distance; }
                }
                if (target == null) { Retreat(enemy, dt, canTraverse); return; }
                if (target.DistanceSquaredToSurface(enemy.X, enemy.Y) <= Square(enemy.Model.AttackRange))
                {
                    if (enemy.Cooldown <= 0f)
                    {
                        target.Hp = Math.Max(0, target.Hp - enemy.Model.AttackDamage);
                        enemy.AttackSequence++;
                        enemy.Cooldown = enemy.Model.AttackCooldown;
                    }
                    return;
                }
                Move(enemy, target.X, target.Y, dt, enemy.Model.MoveSpeed, FlowDestinationGroup.Shelter, canTraverse);
            }
        }

        private void Retreat(FarmRaidEnemy enemy, float dt, Func<float, float, float, float, bool> traversable)
        {
            var exit = ExitPoint(enemy);
            Move(enemy, exit.x, exit.y, dt, enemy.Model.MoveSpeed, FlowDestinationGroup.GateExit, traversable);
            if (!Map.Contains(enemy.X, enemy.Y)) enemy.Escaped = true;
        }
        private void Move(FarmRaidEnemy enemy, float tx, float ty, float dt, float speed, FlowDestinationGroup group,
            Func<float, float, float, float, bool> traversable)
        {
            if (navigation == null) return;
            var cell = navigation.WorldToGrid(tx, ty);
            if (!targetCells.TryGetValue(group, out var old) || old != cell)
            { navigation.SetTargets(group, new[] { (tx, ty) }); targetCells[group] = cell; }
            var flow = navigation.SampleFlow(group, enemy.X, enemy.Y);
            float dx = tx - enemy.X, dy = ty - enemy.Y, length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length > 0 && (length <= 2f || (!Map.Contains(tx, ty) && navigation.WorldToGrid(enemy.X, enemy.Y) == cell)))
                flow = new FlowVector2D(dx / length, dy / length);
            float step = Math.Min(speed * dt, length), x = enemy.X + flow.X * step, y = enemy.Y + flow.Y * step;
            if (!CanTraverseNavigation(enemy.X, enemy.Y, x, y) || !traversable(enemy.X, enemy.Y, x, y)) return;
            enemy.MovedThisStep = Squared(enemy.X, enemy.Y, x, y) > 0.000001f;
            enemy.X = x; enemy.Y = y;
        }

        private bool CanTraverseNavigation(float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            int samples = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dy * dy) / 0.25f));
            for (int i = 1; i <= samples; i++)
            {
                float x = ax + dx * i / samples, y = ay + dy * i / samples;
                if (!Map.Contains(x, y)) continue;
                var cell = Engine.FlowField.WorldToGrid(x, y);
                if (Engine.FlowField.IsBlocked(cell.gx, cell.gy)) return false;
            }
            return true;
        }

        public FarmActionResult TriggerParryStance()
        {
            parryWindowRemaining = 0.45f;
            Engine.Character.CurrentGuard = Math.Min(Engine.Character.MaxGuard, Engine.Character.CurrentGuard + 6f);
            if (Active && !Paused)
            {
                foreach (var enemy in enemies)
                {
                    if (!enemy.Resolved && enemy.Telegraph > 0f &&
                        Squared(enemy.X, enemy.Y, session.PlayerX, session.PlayerY) <= Square(3.8f))
                    {
                        enemy.Telegraph = 0f;
                        enemy.PinSeconds = 1.5f;
                        Damage(enemy, 24f);
                        return new FarmActionResult(true, $"PHẢN ĐÒN HOÀN HẢO! Đánh bật {enemy.Model.Name}, gây 24 sát thương & làm choáng 1.5s!");
                    }
                }
            }
            return new FarmActionResult(true, "Thế Đỡ Chủ Động (Parry 0.45s): Giảm sát thương và phản đòn làm choáng đạo tặc!");
        }

        public FarmActionResult ExecuteMartialSkillSlash(string? targetEnemyId = null)
        {
            var skill = Engine.ExecuteMartialSkillWithCombo("skill_sat_gia_tram");
            if (Active && !Paused)
            {
                foreach (var enemy in enemies)
                {
                    if (!enemy.Resolved &&
                        (enemy.Model.Id == targetEnemyId || Squared(enemy.X, enemy.Y, session.PlayerX, session.PlayerY) <= Square(3.5f)))
                    {
                        Damage(enemy, 38f * Math.Max(1f, skill.damageMultiplier));
                    }
                }
            }
            return new FarmActionResult(true, string.IsNullOrEmpty(skill.message)
                ? "Thi triển Tuyệt Kỹ Võ Học [Sát Gia Trảm] quét vòng cung 3.5m!"
                : skill.message);
        }

        public FarmActionResult AttackEnemy(string id, bool heavy = false)
        {
            var enemy = enemies.Find(e => e.Model.Id == id && !e.Resolved);
            if (!Active || Paused || enemy == null || attackCooldown > 0f) return new FarmActionResult(false, "Chưa thể ra đòn.");
            if (!Engine.Character.Equipment.TryGetValue(EquipSlot.Weapon, out var equipped) || equipped == null ||
                (heavy ? equipped.Id != "wpn_bua_ta" : (equipped.Id != "wpn_dao_rua" && equipped.Id != "wpn_giao_tre")))
                return new FarmActionResult(false, "Cần trang bị đúng vũ khí đang sở hữu trước khi ra đòn.");
            var weapon = heavy ? WeaponData.CreateHammer() : WeaponData.CreateDefaultSword();
            float reach = equipped.Id == "wpn_giao_tre" ? 2.8f : weapon.AttackRange;
            if (Squared(enemy.X, enemy.Y, session.PlayerX, session.PlayerY) > Square(reach + 0.35f))
                return new FarmActionResult(false, "Mục tiêu ngoài tầm vũ khí.");
            float stamina = heavy ? CombatMath.HeavyStamina : CombatMath.LightStamina;
            if (!Engine.Character.Stamina.CanAfford(stamina) || !Engine.Character.TryConsumeCombatStamina(stamina,
                heavy ? CombatMath.HeavyDailyLoadFraction : CombatMath.LightDailyLoadFraction, out _))
                return new FarmActionResult(false, "Không đủ thể lực ra đòn.");
            comboStep = (comboStep % 3) + 1;
            comboWindowRemaining = 1.8f;
            float damage = heavy ? CombatMath.HeavyRaw(CombatMath.BuaTaHeavyDamage, Engine.Character.BaseStats.Str) : CombatMath.DaoRuaLightDamage;
            Damage(enemy, damage); attackCooldown = heavy ? 1.5f : 0.5f; secondsSinceAction = 0f;
            return new FarmActionResult(true, $"Đòn trúng {enemy.Model.Name} (Liên hoàn thức {comboStep}/3).");
        }

        private void AdvanceTowers(float dt)
        {
            if (TowerPosition == null) return;
            foreach (var tower in Engine.Defense.Buildings)
            {
                if (tower.BlueprintId != "NoXuyenVan" || tower.IsBroken || !tower.IsPowered || tower.AmmoCurrent < 1) continue;
                var position = TowerPosition(tower.InstanceId); if (!position.HasValue) continue;
                towerTimers.TryGetValue(tower.InstanceId, out float timer); timer -= dt;
                towerTimers[tower.InstanceId] = timer; if (timer > 0) continue;
                var target = enemies.Find(e => !e.Resolved && Squared(e.X, e.Y, position.Value.x, position.Value.y) <= 22f * 22f);
                if (target == null) continue;
                tower.AmmoCurrent--; ammoSpent++;
                var struck = TowerPierceTargets?.Invoke(tower.InstanceId, target.Model.Id);
                int hitCount = 0; string previous = "";
                if (struck != null)
                    foreach (string id in struck)
                    {
                        var hit = enemies.Find(e => e.Model.Id == id && !e.Resolved);
                        if (hit == null || id == previous || Squared(hit.X, hit.Y, position.Value.x, position.Value.y) > 22f * 22f) continue;
                        Damage(hit, 40f); if (hit.MovedThisStep) hit.PinSeconds = 0.5f; previous = id;
                        if (++hitCount == 2) break;
                    }
                if (struck == null) { Damage(target, 40f); if (target.MovedThisStep) target.PinSeconds = 0.5f; }
                session.State.TowerShotsSinceWind.TryGetValue(tower.InstanceId, out int shots); shots++;
                session.State.TowerShotsSinceWind[tower.InstanceId] = shots % 10;
                towerTimers[tower.InstanceId] = shots % 10 == 0 ? 4f : 1.2f;
                session.State.TowerWearTenths.TryGetValue(tower.InstanceId, out int wear); wear += 4;
                tower.Durability = Math.Max(0, tower.Durability - wear / 10);
                session.State.TowerWearTenths[tower.InstanceId] = wear % 10;
                if (EffectiveTowerDurability(tower) <= 0f) tower.IsBroken = true;
                TowerFired?.Invoke(tower.InstanceId);
            }
        }
        private void Damage(FarmRaidEnemy enemy, float amount)
        {
            if (enemy.Resolved) return;
            Release(enemy); float before = enemy.Model.CurrentHp; enemy.Model.TakeDamage(amount);
            if (enemy.Model.CurrentHp < before) enemy.HitSequence++;
            if (!enemy.Model.IsAlive)
            {
                kills++; goldEarned += enemy.Model.GoldReward;
                Engine.Ledger.Earn(enemy.Model.GoldReward, LedgerReason.WaveReward, enemy.Model.Id);
                Engine.Character.Exp += enemy.Model.ExpReward;
            }
        }
        private void Release(FarmRaidEnemy enemy)
        {
            if (!string.IsNullOrEmpty(enemy.CapturedPigId)) session.SetPigExternallyDriven(enemy.CapturedPigId, false);
            enemy.CapturedPigId = ""; enemy.CaptureSeparation = 0f;
        }
        private void Finish()
        {
            Active = false;
            bool victory = pigsLost == 0 && enemies.TrueForAll(e => !e.Model.IsAlive);
            if (victory) Engine.MinorWavesDefeated++;
            ExactTowerWearSpent = 0; FenceHpLost = 0;
            foreach (var tower in Engine.Defense.Buildings)
                if (initialTowerDurability.TryGetValue(tower.InstanceId, out float durability)) ExactTowerWearSpent += durability - EffectiveTowerDurability(tower);
            foreach (var fence in session.State.Fences)
                if (initialFenceHp.TryGetValue(fence.Id, out float hp)) FenceHpLost += hp - fence.Hp;
            session.State.RecoverableArrowTenths += ammoSpent * 3;
            LastReport = Engine.DeepMechanics.RecordWaveBattleReport(waveNumber, victory, kills, pigsLost, ammoSpent,
                (int)Math.Round(ExactTowerWearSpent, MidpointRounding.AwayFromZero),
                Engine.Character.FaintCount - startFaints, goldEarned);
            LastReport.SummaryText += $" | Hao bền nỏ chính xác: {ExactTowerWearSpent:0.0} | Rào mất: {FenceHpLost:0} HP";
            Engine.EventBus.Publish(new WaveEnded(waveNumber, victory, pigsLost, Engine.Clock.CurrentDay));
        }
        private float EffectiveTowerDurability(DefenseBuildingInstance tower)
        { session.State.TowerWearTenths.TryGetValue(tower.InstanceId, out int pending); return Math.Max(0f, tower.Durability - pending / 10f); }
        private (float x, float y) ExitPoint(FarmRaidEnemy enemy)
        {
            float west = enemy.SpawnX - Map.XMin, east = Map.XMax - enemy.SpawnX;
            float south = enemy.SpawnY - Map.YMin, north = Map.YMax - enemy.SpawnY;
            float distance = Math.Min(Math.Min(west, east), Math.Min(south, north));
            float outside = 1f + enemy.CaptureSeparation;
            if (distance == west) return (Map.XMin - outside, enemy.SpawnY);
            if (distance == east) return (Map.XMax + outside, enemy.SpawnY);
            return distance == south ? (enemy.SpawnX, Map.YMin - outside) : (enemy.SpawnX, Map.YMax + outside);
        }
        private static float Square(float value) => value * value;
        private static float Squared(float ax, float ay, float bx, float by) => Square(ax - bx) + Square(ay - by);
    }
}
