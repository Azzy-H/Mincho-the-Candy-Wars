using System;
using System.Collections.Generic;
using System.Linq;
using MinchoCandyWars.Golem;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MinchoCandyWars.DeathAction
{
    /// <summary>机械傀儡的死亡动作配置。</summary>
    public class DeathActionProperties_MechGolem : DeathActionProperties
    {
        public DeathActionProperties_MechGolem()
        {
            workerClass = typeof(DeathActionWorker_MechGolem);
        }
    }

    /// <summary>
    /// 机械傀儡死亡：生成一只狂暴的迷途傀儡，把原尸体整只收进它体内，
    /// 等它死亡时再把原尸体放回地面（见 <see cref="CompGolemRemains"/>）。
    /// </summary>
    public class DeathActionWorker_MechGolem : DeathActionWorker
    {
        /// <summary>稳定度起点：一天左右致死（沿用迷途傀儡的原有节奏）。</summary>
        private const float StabilitySeverity = 0.1f;

        private const int MinInjuries = 2;
        private const int MaxInjuries = 4;
        private const float MinInjuryFraction = 0.15f;
        private const float MaxInjuryFraction = 0.35f;

        /// <summary>四肢部位名后缀。HAR 种族沿用原版命名，这里同时兼容带/不带下划线。</summary>
        private static readonly string[] LimbNameEndings = { "Arm", "Hand", "Leg", "Foot" };

        public override bool DangerousInMelee => true;

        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            Map map = corpse.MapHeld;
            if (map == null) return;

            IntVec3 cell = corpse.PositionHeld;
            Pawn feral = PawnGenerator.GeneratePawn(MCW_DefOf.MinchoWitch_MinchoGolem);
            CompGolemRemains holder = feral.TryGetComp<CompGolemRemains>();
            if (holder == null)
            {
                Log.Error("[MCW] Mincho golem remains component missing; keeping mechanical corpse.");
                feral.Destroy();
                return;
            }

            // 原 pawn 留在自己的尸体里，名字、身份、伤势都不动。
            if (corpse.Spawned) corpse.DeSpawn();
            corpse.holdingOwner?.Remove(corpse);
            if (!holder.GetDirectlyHeldThings().TryAdd(corpse))
            {
                GenPlace.TryPlaceThing(corpse, cell, map, ThingPlaceMode.Near);
                feral.Destroy();
                return;
            }

            feral.ageTracker.AgeBiologicalTicks = 0;
            feral.ageTracker.AgeChronologicalTicks = 0;
            ApplyStability(feral);
            AddRandomFeralInjuries(feral);
            GenSpawn.Spawn(feral, cell, map, WipeMode.VanishOrMoveAside);
            feral.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, forceWake: true);
        }

        private static void ApplyStability(Pawn feral)
        {
            HediffDef def = MCW_DefOf.MinchoWitch_MinchoGolem_NoPain;
            Hediff stability = feral.health.hediffSet.GetFirstHediffOfDef(def);
            if (stability == null)
            {
                stability = HediffMaker.MakeHediff(def, feral);
                feral.health.AddHediff(stability);
            }

            stability.Severity = StabilitySeverity;
        }

        private static void AddRandomFeralInjuries(Pawn pawn)
        {
            // 只打四肢：不动躯干/头部，也不制造断肢或必死伤。
            List<BodyPartRecord> limbs = pawn.health.hediffSet.GetNotMissingParts()
                .Where(IsLimb)
                .InRandomOrder()
                .Take(Rand.RangeInclusive(MinInjuries, MaxInjuries))
                .ToList();

            if (limbs.Count == 0)
            {
                Log.WarningOnce("[MCW] 迷途傀儡没有匹配到四肢部位，未添加伤势（body def 的部位命名可能不合约定）。", 437112);
                return;
            }

            foreach (BodyPartRecord part in limbs)
            {
                Hediff injury = HediffMaker.MakeHediff(MCW_DefOf.Crush, pawn, part);
                injury.Severity = part.def.GetMaxHealth(pawn) * Rand.Range(MinInjuryFraction, MaxInjuryFraction);
                if (!pawn.health.WouldDieAfterAddingHediff(injury)) pawn.health.AddHediff(injury);
            }
        }

        private static bool IsLimb(BodyPartRecord part) =>
            part.depth == BodyPartDepth.Outside &&
            LimbNameEndings.Any(ending => part.def.defName.EndsWith(ending, StringComparison.OrdinalIgnoreCase));
    }


}
