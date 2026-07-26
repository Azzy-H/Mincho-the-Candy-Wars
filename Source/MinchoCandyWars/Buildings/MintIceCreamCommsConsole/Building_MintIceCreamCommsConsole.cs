using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;


namespace MinchoCandyWars.Buildings
{
    /// <summary>
    /// 薄荷冰淇通讯台：同时拥有轨道贸易信标（Building_OrbitalTradeBeacon）和通讯台（Building_CommsConsole）的功能。
    /// 继承自 Building_OrbitalTradeBeacon 以保证贸易系统的兼容性，
    /// 通讯台交互逻辑通过自定义 FloatMenu 和 Harmony 补丁实现。
    /// </summary>
    public class Building_MintIceCreamCommsConsole : Building_OrbitalTradeBeacon
    {
        private CompPowerTrader ?powerComp;

        public bool CanUseCommsNow
        {
            get
            {
                if (base.Spawned && base.Map.gameConditionManager.ElectricityDisabled(base.Map))
                {
                    return false;
                }
                if (powerComp != null)
                {
                    return powerComp.PowerOn;
                }
                return true;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.BuildOrbitalTradeBeacon, OpportunityType.GoodToKnow);
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.OpeningComms, OpportunityType.GoodToKnow);
            if (CanUseCommsNow)
            {
                LongEventHandler.ExecuteWhenFinished(AnnounceTradeShips);
            }
        }

        public IEnumerable<ICommunicable> GetCommTargets(Pawn myPawn)
        {
            return myPawn.Map.passingShipManager.passingShips.Cast<ICommunicable>()
                .Concat(Find.FactionManager.AllFactionsVisibleInViewOrder
                    .Where((Faction f) => !f.temporary && !f.IsPlayer)
                    .Cast<ICommunicable>());
        }

        public void GiveUseCommsJob(Pawn negotiator, ICommunicable target)
        {
            Job job = JobMaker.MakeJob(JobDefOf.UseCommsConsole, this);
            job.commTarget = target;
            negotiator.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.OpeningComms, KnowledgeAmount.Total);
        }

        private FloatMenuOption GetFailureReason(Pawn myPawn)
        {
            if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Some))
            {
                return new FloatMenuOption("CannotUseNoPath".Translate(), null);
            }
            if (base.Spawned && base.Map.gameConditionManager.ElectricityDisabled(base.Map))
            {
                return new FloatMenuOption("CannotUseSolarFlare".Translate(), null);
            }
            if (powerComp != null && !powerComp.PowerOn)
            {
                return new FloatMenuOption("CannotUseNoPower".Translate(), null);
            }
            if (!myPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
            {
                return new FloatMenuOption("CannotUseReason".Translate(
                    "IncapableOfCapacity".Translate(PawnCapacityDefOf.Talking.label, myPawn.Named("PAWN"))), null);
            }
            if (!GetCommTargets(myPawn).Any())
            {
                return new FloatMenuOption("CannotUseReason".Translate("NoCommsTarget".Translate()), null);
            }
            if (!CanUseCommsNow)
            {
                Log.Error(myPawn?.ToString() + " could not use comm console for unknown reason.");
                return new FloatMenuOption("Cannot use now", null);
            }
            return null!;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            FloatMenuOption failureReason = GetFailureReason(myPawn);
            if (failureReason != null)
            {
                yield return failureReason;
                yield break;
            }

            foreach (ICommunicable commTarget in GetCommTargets(myPawn))
            {
                FloatMenuOption option = CreateCommFloatMenuOption(commTarget, myPawn);
                if (option != null)
                {
                    yield return option;
                }
            }

            foreach (FloatMenuOption gizmoOption in base.GetFloatMenuOptions(myPawn))
            {
                yield return gizmoOption;
            }
        }

        /// <summary>
        /// 为通讯目标（派系/商船/路过船只）创建 FloatMenu，等价于原版 CommFloatMenuOption
        /// </summary>
        private FloatMenuOption CreateCommFloatMenuOption(ICommunicable commTarget, Pawn negotiator)
        {
            if (commTarget is Faction faction)
            {
                return CreateFactionFloatMenuOption(faction, negotiator);
            }
            if (commTarget is PassingShip passingShip)
            {
                return CreatePassingShipFloatMenuOption(passingShip, negotiator);
            }
            return null!;
        }

        private FloatMenuOption CreateFactionFloatMenuOption(Faction faction, Pawn negotiator)
        {
            if (faction.IsPlayer)
            {
                return null!;
            }

            string label = "CallOnRadio".Translate(faction.GetCallLabel());
            label = label + " (" + faction.PlayerRelationKind.GetLabelCap() + ", " +
                    faction.PlayerGoodwill.ToStringWithSign() + ")";

            if (faction.leader == null || (faction.leader.Spawned && (faction.leader.Downed || faction.leader.IsPrisoner || !faction.leader.Awake() || faction.leader.InMentalState)))
            {
                string reason = (faction.leader == null)
                    ? "LeaderUnavailableNoLeader".Translate()
                    : "LeaderUnavailable".Translate(faction.leader.LabelShort, faction.leader);
                return new FloatMenuOption(label + " (" + reason + ")", null,
                    faction.def.FactionIcon, faction.Color);
            }

            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, delegate
                {
                    GiveUseCommsJob(negotiator, faction);
                }, faction.def.FactionIcon, faction.Color, MenuOptionPriority.InitiateSocial),
                negotiator, this);
        }

        private FloatMenuOption CreatePassingShipFloatMenuOption(PassingShip ship, Pawn negotiator)
        {
            if (ship is TradeShip tradeShip)
            {
                if (!tradeShip.CanTradeNow)
                {
                    return new FloatMenuOption("CannotTrade".Translate() + ": " + tradeShip.def.label, null);
                }
                string label = "TradeWith".Translate(tradeShip.name, tradeShip.def.label);
                return FloatMenuUtility.DecoratePrioritizedTask(
                    new FloatMenuOption(label, delegate
                    {
                        GiveUseCommsJob(negotiator, tradeShip);
                    }, MenuOptionPriority.InitiateSocial),
                    negotiator, this);
            }

            // 非贸易路过船只的通用通讯选项
            string genericLabel = "CallOnRadio".Translate(ship.GetCallLabel());
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(genericLabel, delegate
                {
                    GiveUseCommsJob(negotiator, ship);
                }, MenuOptionPriority.InitiateSocial),
                negotiator, this);
        }

        private void AnnounceTradeShips()
        {
            foreach (TradeShip item in from s in base.Map.passingShipManager.passingShips.OfType<TradeShip>()
                     where !s.WasAnnounced
                     select s)
            {
                TaggedString baseLetterText = "TraderArrival".Translate(
                    item.name, item.def.label,
                    (item.Faction == null)
                        ? "TraderArrivalNoFaction".Translate()
                        : "TraderArrivalFromFaction".Translate(item.Faction.Named("FACTION")));
                IncidentParms incidentParms = new IncidentParms();
                incidentParms.target = base.Map;
                incidentParms.traderKind = item.TraderKind;
                IncidentWorker.SendIncidentLetter(item.def.LabelCap, baseLetterText, LetterDefOf.PositiveEvent,
                    incidentParms, LookTargets.Invalid, null);
                item.WasAnnounced = true;
            }
        }
    }
}
