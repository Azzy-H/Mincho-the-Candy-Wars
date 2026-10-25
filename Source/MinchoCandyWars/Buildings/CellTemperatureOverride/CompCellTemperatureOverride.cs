using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Buildings
{
    /// <summary>
    /// 格子温度覆盖：把建筑影响的格子温度恒定覆盖为指定值。
    ///
    /// 两种作用范围（由 affectWorkTables 决定）：
    ///   · affectWorkTables = false（默认）：只覆盖建筑**自身占据的格子** ——
    ///     用于珉巧床：躺在这张床上的人（正好位于床格）才会被恒温保护。
    ///   · affectWorkTables = true：覆盖**工作台**的工作台格子 + 互动格（pawn 工作时站的位置），
    ///     用于饼干操作臂：让工作台前的 pawn 读取到正常温度，而**不影响**房间其它格子。
    ///     具体覆盖哪些工作台取决于本建筑有没有设施组件：
    ///       - 有 CompFacility（操作臂）：只覆盖**真正连接到本建筑**的工作台，
    ///         距离 / 视线 / 每台一个的规则全部交给原版 CompFacility；
    ///       - 没有设施组件：退回旧行为，覆盖所在房间内所有工作台。
    ///
    /// 连接变化通过 CompFacility.OnLinkAdded / OnLinkRemoved 事件感知（不需要 Harmony 补丁），
    /// 另外每 250 tick 兜底重注册一次，跟随房间结构变化。
    /// </summary>
    public class CompProperties_CellTemperatureOverride : CompProperties
    {
        public CompProperties_CellTemperatureOverride()
        {
            this.compClass = typeof(CompCellTemperatureOverride);
        }

        /// <summary>温度覆盖值（摄氏度）。</summary>
        public float temperature = 0f;

        /// <summary>
        /// 是否覆盖房间内所有工作台（工作台格子 + 互动格）。
        /// false（默认）= 只覆盖建筑自身占据的格子。
        /// </summary>
        public bool affectWorkTables = false;
    }

    public class CompCellTemperatureOverride : ThingComp
    {
        public CompProperties_CellTemperatureOverride Props => (CompProperties_CellTemperatureOverride)this.props;

        private readonly List<IntVec3> registeredCells = new List<IntVec3>();

        /// <summary>重新注册的检测间隔（tick）。</summary>
        private const int RefreshInterval = 250;

        private int ticksToRefresh;

        /// <summary>本建筑的设施组件（饼干操作臂才有），用于感知连接变化。</summary>
        private CompFacility? facility;

        /// <summary>连接刚刚变化，下一 tick 需要重注册。</summary>
        private bool linksDirty;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            HookFacilityLinks();
            RegisterToMap(parent.Map);
            ticksToRefresh = RefreshInterval;
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            UnhookFacilityLinks();
            DeregisterFromMap(map);
            base.PostDeSpawn(map, mode);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            UnhookFacilityLinks();
            DeregisterFromMap(previousMap);
            base.PostDestroy(mode, previousMap);
        }

        /// <summary>
        /// 订阅设施连接事件，替代原先对 CompFacility.Notify_NewLink / Notify_LinkRemoved /
        /// LinkToNearbyBuildings 的 Harmony 补丁 —— 原版本来就是公开 event，
        /// 而且 LinkToNearbyBuildings / UnlinkAll 内部也会触发它们。
        /// </summary>
        private void HookFacilityLinks()
        {
            facility = parent.GetComp<CompFacility>();
            if (facility == null)
            {
                return;
            }

            // 先减后加，避免重复订阅（例如读档后重复调用）
            facility.OnLinkAdded -= OnFacilityLinkChanged;
            facility.OnLinkAdded += OnFacilityLinkChanged;
            facility.OnLinkRemoved -= OnFacilityLinkChanged;
            facility.OnLinkRemoved += OnFacilityLinkChanged;
        }

        private void UnhookFacilityLinks()
        {
            if (facility == null)
            {
                return;
            }

            facility.OnLinkAdded -= OnFacilityLinkChanged;
            facility.OnLinkRemoved -= OnFacilityLinkChanged;
            facility = null;
        }

        /// <summary>
        /// 只打脏标记，实际重注册在下一 tick 统一做：
        /// 一次重连可能连/断多个工作台，逐个重注册会白白刷 N 遍。
        /// </summary>
        private void OnFacilityLinkChanged(CompFacility changed, Thing other)
        {
            linksDirty = true;
        }

        public override void CompTick()
        {
            base.CompTick();

            // 只覆盖自身格子的建筑（如珉巧床）不需要刷新
            if (!Props.affectWorkTables)
            {
                return;
            }

            bool linksChanged = linksDirty;
            linksDirty = false;

            if (!linksChanged && --ticksToRefresh > 0)
            {
                return;
            }
            ticksToRefresh = RefreshInterval;

            if (!parent.Spawned || parent.Map == null)
            {
                return;
            }

            // 连接变化（events 标记）或定期兜底：工作台可能被建造/拆除/移动，房间也可能变化
            RegisterToMap(parent.Map);
        }

        private void RegisterToMap(Map map)
        {
            if (map == null || !parent.Spawned)
            {
                return;
            }

            DeregisterFromMap(map);

            foreach (IntVec3 cell in AffectedCells())
            {
                map.GetComponent<MapComponent_CellTemperatureOverrideRegistry>().Register(cell, this);
                registeredCells.Add(cell);
            }
        }

        /// <summary>
        /// 本建筑温度覆盖影响的格子：
        ///   affectWorkTables=true 且挂了设施组件 → 已连接工作台的格子 + 互动格；
        ///   affectWorkTables=true 且没有设施组件 → 房间内所有工作台的格子 + 互动格；
        ///   否则 → 仅建筑自身占据的格子。
        /// </summary>
        private IEnumerable<IntVec3> AffectedCells()
        {
            if (!Props.affectWorkTables)
            {
                foreach (IntVec3 cell in parent.OccupiedRect())
                {
                    yield return cell;
                }
                yield break;
            }

            // 操作臂：只认真正连上的工作台。距离、视线、每台一个的判定都在 CompFacility 里，
            // 这里不重复实现，避免两套规则打架。
            CompFacility facility = parent.GetComp<CompFacility>();
            if (facility != null)
            {
                if (!facility.CanBeActive)
                {
                    yield break;
                }

                foreach (Thing target in facility.LinkedBuildings)
                {
                    if (!target.Spawned || target.Destroyed || target.Map != parent.Map) continue;

                    CompAffectedByFacilities affected = target.TryGetComp<CompAffectedByFacilities>();
                    if (affected == null || !affected.IsFacilityActive(parent)) continue;

                    foreach (IntVec3 cell in target.OccupiedRect())
                    {
                        yield return cell;
                    }

                    if (target.def.hasInteractionCell && target.InteractionCell.IsValid)
                    {
                        yield return target.InteractionCell;
                    }
                }
                yield break;
            }

            // 没挂设施组件时的旧行为：覆盖房间内所有工作台
            Room room = parent.GetRoom();
            if (room != null)
            {
                foreach (Thing thing in room.ContainedAndAdjacentThings)
                {
                    // 研究台（Building_ResearchBench）不是 Building_WorkTable 的子类，必须单独判。
                    if (!(thing is Building_WorkTable || thing is Building_ResearchBench)) continue;
                    if (thing.Destroyed || !thing.Spawned) continue;

                    foreach (IntVec3 cell in thing.OccupiedRect())
                    {
                        yield return cell;
                    }

                    if (thing.def.hasInteractionCell && thing.InteractionCell.IsValid)
                    {
                        yield return thing.InteractionCell;
                    }
                }
                yield break;
            }

            // 操作臂不在房间里（户外/无屋顶）时退化为自身格子
            foreach (IntVec3 cell in parent.OccupiedRect())
            {
                yield return cell;
            }
        }

        private void DeregisterFromMap(Map map)
        {
            if (map == null)
            {
                registeredCells.Clear();
                return;
            }

            MapComponent_CellTemperatureOverrideRegistry registry = map.GetComponent<MapComponent_CellTemperatureOverrideRegistry>();
            for (int i = 0; i < registeredCells.Count; i++)
            {
                registry.Deregister(registeredCells[i], this);
            }
            registeredCells.Clear();
        }
    }
}
