using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.LinglonCandy
{
    /// <summary>
    /// 玲珑糖心灵光环 GameCondition——维护持有 MCW_LinglonAura 的 Mincho 列表，
    /// 每 tick 给敌对 pawn 施加/刷新意识减益。列表为空则自动消失。
    /// </summary>
    public class GameCondition_LinglonAura : GameCondition
    {
        private HashSet<Hediff_LinglonAura> registeredHediffs = new HashSet<Hediff_LinglonAura>();
        private const int ApplyIntervalTicks = 60; // 每秒施加一次

        public override void Init()
        {
            base.Init();
            Permanent = true;
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();

            registeredHediffs.RemoveWhere(h =>
            {
                if (h == null || h.pawn == null || h.pawn.Dead || h.pawn.Destroyed) return true;
                if (h.pawn.Map != SingleMap) return true;
                if (h.pawn.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_LinglonAura) != h) return true;
                return false;
            });

            if (registeredHediffs.Count == 0)
            {
                End();
                return;
            }

            if (Find.TickManager.TicksGame % ApplyIntervalTicks != 0)
            {
                return;
            }

            foreach (Pawn pawn in SingleMap.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Dead || pawn.Faction == null)
                {
                    continue;
                }

                if (!registeredHediffs.Any(h => pawn.HostileTo(h.pawn.Faction)))
                {
                    continue;
                }

                Hediff? debuff = pawn.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_LinglonAuraDebuff);
                if (debuff == null)
                {
                    pawn.health.AddHediff(HediffMaker.MakeHediff(MCW_DefOf.MCW_LinglonAuraDebuff, pawn));
                }
                else
                {
                    debuff.TryGetComp<HediffComp_Disappears>()?.ResetElapsedTicks();
                }
            }
        }

        public void RegisterHediff(Hediff_LinglonAura hediff)
        {
            registeredHediffs.Add(hediff);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref registeredHediffs, "registeredHediffs", LookMode.Reference);
        }
    }
}
