using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MinchoCandyWars
{
    /// <summary>
    /// 战争信标：每隔 intervalTicks 在自己附近刷新一批单位。
    ///
    /// 两种用途（同一个组件，靠 XML 配置区分）：
    ///   · spawnRecycle = true  → 刷新回收部队，并用 LordJob_AssaultColony 让它们主动进攻；
    ///   · spawnMincho  = true  → 刷新珉巧，直接加入玩家阵营（自动参战）。
    ///
    /// questFF1 的据点里：4 个回收传送点（spawnRecycle） + 1 个珉巧援军信标（spawnMincho）。
    /// </summary>
    public class CompProperties_WarBeacon : CompProperties
    {
        /// <summary>刷新回收部队（敌人，主动进攻）。</summary>
        public bool spawnRecycle;

        /// <summary>刷新珉巧（加入玩家）。</summary>
        public bool spawnMincho;

        /// <summary>刷新间隔（tick），默认 1 小时。</summary>
        public int intervalTicks = 2500;

        /// <summary>每次刷新的数量。</summary>
        public int count = 2;

        /// <summary>刷新间隔的随机浮动（tick）。</summary>
        public int intervalVariance = 600;

        public CompProperties_WarBeacon()
        {
            compClass = typeof(Comp_WarBeacon);
        }
    }

    public class Comp_WarBeacon : ThingComp
    {
        public CompProperties_WarBeacon Props => (CompProperties_WarBeacon)props;

        private int ticksToNextSpawn;

        private const string RecycleFactionDefName = "MCW_RecycleCorp_Faction";

        /// <summary>回收部队刷新的兵种（按顺序随机）。</summary>
        private static readonly string[] RecycleKinds =
        {
            "MCW_RecycleCorp_Member",
            "MCW_RecycleCorp_Member",
            "MCW_RecycleCorp_Member",
            "MCW_RecycleCorp_Leader",
        };

        /// <summary>珉巧援军刷新的兵种（按顺序随机）。</summary>
        private static readonly string[] MinchoKinds =
        {
            "Mincho_Mercenary",
            "Mincho_Mercenary",
            "Mincho_Knight_Elite",
            "Mincho_Defender",
            "Mincho_SpaceRefugee",
            "MCW_SmartMincho",
            // 珉巧机械体（次级珉巧子个体）
            "MCW_Mech_Battle",
            "MCW_Mech_Defender",
            "MCW_Mech_Battle",
            "MCW_Mech_Defender",
        };

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (ticksToNextSpawn <= 0)
            {
                ticksToNextSpawn = Props.intervalTicks;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!parent.Spawned || parent.Map == null)
            {
                return;
            }

            if (--ticksToNextSpawn > 0)
            {
                return;
            }

            int variance = Props.intervalVariance;
            ticksToNextSpawn = Props.intervalTicks + (variance > 0 ? Rand.Range(-variance, variance) : 0);
            if (ticksToNextSpawn < 60)
            {
                ticksToNextSpawn = 60;
            }

            SpawnBatch();
        }

        private void SpawnBatch()
        {
            Map map = parent.Map;
            if (map == null)
            {
                return;
            }

            if (Props.spawnRecycle)
            {
                FactionDef factionDef = DefDatabase<FactionDef>.GetNamed(RecycleFactionDefName, errorOnFail: false);
                Faction faction = factionDef == null ? null : Find.FactionManager.FirstFactionOfDef(factionDef);
                if (faction != null)
                {
                    SpawnPawns(map, faction, RecycleKinds, Props.count, attack: true);
                }
            }

            if (Props.spawnMincho)
            {
                SpawnPawns(map, Faction.OfPlayer, MinchoKinds, Props.count, attack: false);
            }
        }

        private void SpawnPawns(Map map, Faction faction, string[] kindDefNames, int count, bool attack)
        {
            List<Pawn> spawned = new List<Pawn>();

            for (int i = 0; i < count; i++)
            {
                string kindName = kindDefNames.RandomElement();
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamed(kindName, errorOnFail: false);
                if (kind == null)
                {
                    continue;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    kind, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true));
                if (pawn == null)
                {
                    continue;
                }

                // 智慧珉巧：激活核心
                if (kindName == "MCW_SmartMincho")
                {
                    CompMinchoCore core = pawn.GetComp<CompMinchoCore>();
                    if (core != null && !core.Active)
                    {
                        core.Active = true;
                    }
                }

                IntVec3 cell = CellFinder.RandomClosewalkCellNear(parent.Position, map, 6);
                if (!cell.IsValid)
                {
                    cell = parent.Position;
                }
                GenSpawn.Spawn(pawn, cell, map);
                spawned.Add(pawn);
            }

            // 敌人成团主动进攻
            if (attack && spawned.Count > 0)
            {
                LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction), map, spawned);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksToNextSpawn, "ticksToNextSpawn");
        }
    }
}
