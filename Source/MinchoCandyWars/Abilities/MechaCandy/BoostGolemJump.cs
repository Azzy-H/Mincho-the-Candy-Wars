using RimWorld;
using Verse;
using Mincho_The_Mint_Choco_Slime;
using HarmonyLib;

namespace MinchoCandyWars.Abilities.MechaCandy;

// Use the native ability charge lifecycle instead of the greatsword verb's unconditional cooldown.
public class BoostGolemAbility : Ability
{
    public BoostGolemAbility(Pawn pawn) : base(pawn) { }
    public BoostGolemAbility(Pawn pawn, AbilityDef def) : base(pawn, def) { }
    public void NotifyJumpSucceeded() => PreActivate(null);
}

public class Verb_BoostGolemJump : Verb_JumpAttack
{
    protected override bool TryCastShot()
    {
        if (Ability is not BoostGolemAbility ability || !ability.CanCast ||
            !CasterPawn.health.hediffSet.HasHediff(MCW_HediffDefOf.MCW_MechBoost))
            return false;
        if (!DoJump(CasterPawn, currentTarget, null, verbProps, ability, default,
                VerbProperties_JumpAttack.pawnFlyerOverrideDef))
            return false;
        ability.NotifyJumpSucceeded();
        return true;
    }
}

// The greatsword flyer reads damage from a held weapon. The unarmed golem uses its fists.
[HarmonyPatch(typeof(PawnJumpAttack), "AddDamage")]
static class BoostGolemLanding
{
    static bool Prefix(PawnJumpAttack __instance, Pawn pawn, int radius)
    {
        if (pawn.def != MCW_ThingDefOf.MCW_Mech_Golem ||
            !pawn.health.hediffSet.HasHediff(MCW_HediffDefOf.MCW_MechBoost)) return true;
        float power = pawn.def.tools?.Max(t => t.power) ?? 0;
        IntVec3 center = __instance.DestinationPos.ToIntVec3();
        foreach (var target in GenRadial.RadialDistinctThingsAround(center, __instance.Map, radius, true).ToList())
            if (target is Pawn enemy && enemy.HostileTo(pawn) && GenSight.LineOfSight(center, target.Position, __instance.Map))
                target.TakeDamage(new DamageInfo(DamageDefOf.Stun, power, instigator:pawn));
        return false;
    }
}
