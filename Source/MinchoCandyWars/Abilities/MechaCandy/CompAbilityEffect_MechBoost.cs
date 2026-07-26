using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace MinchoCandyWars.Abilities.MechaCandy
{
    internal class CompAbilityEffect_MechBoost : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.TryGetPawn(out Pawn pawn) && pawn.RaceProps.IsMechanoid && pawn.Faction == parent.pawn.Faction)
            {
                return true;
            }
            return false;
        }
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (target.TryGetPawn(out Pawn pawn) && pawn.RaceProps.IsMechanoid && pawn.Faction == parent.pawn.Faction)
            {
                return true;
            }
            return false;
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff_MechBoost hediff_MechBoost = (Hediff_MechBoost)target.Pawn.health.AddHediff(MCW_DefOf.MCW_MechBoost);
            parent.pawn.DeSpawn(DestroyMode.Vanish);
            hediff_MechBoost.GetDirectlyHeldThings().TryAddOrTransfer(parent.pawn);
        }
    }
    [StaticConstructorOnStartup]
    public class Hediff_MechBoost : Hediff, IThingHolder
    {
        private static readonly Texture2D SplitCommand = ContentFinder<Texture2D>.Get("UI/Commands/SplitCaravan");

        private ThingOwner<Thing> _innerContainers = null!;
        IThingHolder IThingHolder.ParentHolder => pawn;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return _innerContainers;
        }

        public override void PostMake()
        {
            base.PostMake();
            _innerContainers = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref _innerContainers, "innerContainers", this);
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            //foreach (var gizmo in base.GetGizmos())
            //{
            //    yield return gizmo;
            //}
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "MinchoCandyWars.MechBoostExitLabel".Translate();
            command_Action.defaultDesc = "MinchoCandyWars.MechBoostExitDesc".Translate();
            command_Action.icon = SplitCommand;
            command_Action.action = delegate
            {
                if (GetDirectlyHeldThings().Count > 0)
                {
                    Exit();
                }
            };
            yield return command_Action;
        }
        public void Exit()
        {
            _innerContainers.TryDropAll(pawn.Position, pawn.Map, ThingPlaceMode.Near);
        }
        public override bool ShouldRemove => !_innerContainers.Any();
    }
}
