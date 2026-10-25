using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MinchoCandyWars.DeathAction;

public class DeathActionProperties_AdultSlimeDivide : DeathActionProperties
{
    public DeathActionProperties_AdultSlimeDivide()
    {
        workerClass = typeof(DeathActionWorker_AdultSlimeDivide);
    }
}

public class DeathActionWorker_AdultSlimeDivide : DeathActionWorker_RevertDivide
{
    public override void PawnDied(Corpse corpse, Lord prevLord)
    {
        Pawn pawn = corpse.InnerPawn;
        Map map = corpse.MapHeld;
        if (pawn == null || map == null) return;
        if (pawn.ageTracker.Adult)
        {
            // Retain upstream's two offspring, previous-stage age and training/master transfer.
            base.PawnDied(corpse, prevLord);
            return;
        }

        // Juveniles keep upstream death drops and cleanup, without creating offspring.
        IntVec3 position = corpse.PositionHeld;
        FilthMaker.TryMakeFilth(position, map, MCW_DefOf.Mincho_Filth_BloodDef, 5, FilthSourceFlags.None);
        Thing iceCream = ThingMaker.MakeThing(MCW_DefOf.Mincho_Mintchoco);
        iceCream.stackCount = (int)(40 * pawn.BodySize);
        GenPlace.TryPlaceThing(iceCream, position, map, ThingPlaceMode.Near);
        pawn.DropAndForbidEverything();
        corpse.Destroy();
    }
}
