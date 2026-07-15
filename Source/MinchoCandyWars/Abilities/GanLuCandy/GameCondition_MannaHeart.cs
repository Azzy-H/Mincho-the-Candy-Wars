using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.GanLuCandy
{
    /// <summary>
    /// 心之甘露 GameCondition —— 维护持有 MCW_MannaHeart hediff 的 Mincho 列表。
    /// 每 tick 检查有效性，列表为空则自动消失。
    /// ThoughtWorker_MannaHeart 检测此条件来给全图殖民者 +5 心情。
    /// </summary>
    public class GameCondition_MannaHeart : GameCondition
    {
        private HashSet<Hediff_MannaHeart> registeredHediffs = new HashSet<Hediff_MannaHeart>();

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
                if (h == null || h.pawn == null || h.pawn.Dead || h.pawn.Destroyed)
                    return true;
                if (h.pawn.Map != SingleMap)
                    return true;
                if (h.pawn.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MannaHeart) != h)
                    return true;
                return false;
            });

            if (registeredHediffs.Count == 0)
            {
                End();
            }
        }

        public void RegisterHediff(Hediff_MannaHeart hediff)
        {
            registeredHediffs.Add(hediff);
        }

        public void UnregisterHediff(Hediff_MannaHeart hediff)
        {
            registeredHediffs.Remove(hediff);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref registeredHediffs, "registeredHediffs", LookMode.Reference);
        }
    }
}