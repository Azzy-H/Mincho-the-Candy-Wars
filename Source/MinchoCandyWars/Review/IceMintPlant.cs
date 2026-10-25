using RimWorld;

namespace MinchoCandyWars.Misc;

// Native plant ticks apply this damage gradually. Other native death causes remain intact.
public class IceMintPlant : Plant
{
    public override float CurrentDyingDamagePerTick =>
        Math.Max(base.CurrentDyingDamagePerTick, Spawned && AmbientTemperature >= 0f ? 0.005f : 0f);
}
