using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.MechaCandy
{
    public class CompAbilityEffect_MechGestatorAcceleration : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return target.HasThing && target.Thing is Building_MechGestator building_MechGestator && building_MechGestator.ActiveMechBill != null && building_MechGestator.ActiveMechBill.State != FormingState.Gathering && building_MechGestator.ActiveMechBill.State != FormingState.Formed;
        }
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return target.HasThing && target.Thing is Building_MechGestator building_MechGestator && building_MechGestator.ActiveMechBill != null && building_MechGestator.ActiveMechBill.State != FormingState.Gathering && building_MechGestator.ActiveMechBill.State != FormingState.Formed;
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            ((Building_MechGestator)target.Thing).ActiveMechBill.formingTicks = 0;
        }
    }
}
