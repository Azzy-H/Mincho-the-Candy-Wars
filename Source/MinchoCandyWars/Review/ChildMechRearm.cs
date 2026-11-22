using RimWorld;
using Verse;

namespace MinchoCandyWars.Review;

public class CompProperties_ChildMechRearm : CompProperties
{
    public CompProperties_ChildMechRearm() => compClass = typeof(CompChildMechRearm);
}

// These built-in weapons use destroyOnDrop. Restore the native loadout after recovery,
// rather than searching the map for a weapon that was destroyed when the mech fell.
public class CompChildMechRearm : ThingComp
{
    public override void PostSpawnSetup(bool respawningAfterLoad) => RestoreWeapon();
    public override void CompTick() { if (parent.IsHashIntervalTick(250)) RestoreWeapon(); }
    public override void CompTickInterval(int delta) { if (parent.IsHashIntervalTick(250, delta)) RestoreWeapon(); }

    void RestoreWeapon()
    {
        var pawn = (Pawn)parent;
        if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.equipment == null ||
            pawn.equipment.Primary != null || pawn.kindDef.weaponTags.NullOrEmpty()) return;
        PawnWeaponGenerator.TryGenerateWeaponFor(pawn, new PawnGenerationRequest(pawn.kindDef, pawn.Faction));
    }
}
