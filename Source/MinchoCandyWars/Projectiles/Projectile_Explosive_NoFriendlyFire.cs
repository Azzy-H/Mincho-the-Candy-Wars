using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MinchoCandyWars.Projectiles
{
    public class Projectile_Explosive_NoFriendlyFire : Projectile_Explosive
    {
        protected override void Explode()
        {
            Map map = base.Map;
            Destroy();
            if (def.projectile.explosionEffect != null)
            {
                Effecter effecter = def.projectile.explosionEffect.Spawn();
                if (def.projectile.explosionEffectLifetimeTicks != 0)
                {
                    map.effecterMaintainer.AddEffecterToMaintain(effecter, base.Position.ToVector3().ToIntVec3(), def.projectile.explosionEffectLifetimeTicks);
                }
                else
                {
                    effecter.Trigger(new TargetInfo(base.Position, map), new TargetInfo(base.Position, map));
                    effecter.Cleanup();
                }
            }
            IntVec3 position = base.Position;
            float explosionRadius = def.projectile.explosionRadius;
            DamageDef damageDef = base.DamageDef;
            Thing instigator = launcher;
            int damageAmount = DamageAmount;
            float armorPenetration = ArmorPenetration;
            SoundDef soundExplode = def.projectile.soundExplode;
            ThingDef weapon = equipmentDef;
            ThingDef projectile = def;
            Thing thing = intendedTarget.Thing;
            ThingDef? postExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDef ?? (def.projectile.explosionSpawnsSingleFilth ? null : def.projectile.filth);
            ThingDef? postExplosionSpawnThingDefWater = def.projectile.postExplosionSpawnThingDefWater;
            float postExplosionSpawnChance = def.projectile.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = def.projectile.postExplosionSpawnThingCount;
            GasType? postExplosionGasType = def.projectile.postExplosionGasType;
            ThingDef? preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
            float preExplosionSpawnChance = def.projectile.preExplosionSpawnChance;
            int preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
            bool applyDamageToExplosionCellsNeighbors = def.projectile.applyDamageToExplosionCellsNeighbors;
            float explosionChanceToStartFire = def.projectile.explosionChanceToStartFire;
            bool explosionDamageFalloff = def.projectile.explosionDamageFalloff;
            float? direction = origin.AngleToFlat(destination);
            float expolosionPropagationSpeed = base.DamageDef.expolosionPropagationSpeed;
            float screenShakeFactor = def.projectile.screenShakeFactor;
            bool doExplosionVFX = def.projectile.doExplosionVFX;
            ThingDef? preExplosionSpawnSingleThingDef = def.projectile.preExplosionSpawnSingleThingDef;
            ThingDef? postExplosionSpawnSingleThingDef = def.projectile.postExplosionSpawnSingleThingDef;
            GenExplosion.DoExplosion(position, map, explosionRadius, damageDef, 
                instigator, damageAmount, armorPenetration, soundExplode, weapon, 
                projectile, thing, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, 
                postExplosionGasType, null, 255, applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, 
                preExplosionSpawnChance, preExplosionSpawnThingCount, explosionChanceToStartFire, 
                explosionDamageFalloff, direction, ignoredThings: map.spawnedThings.Where(t=> IsAlly(t,launcher)).ToList(), null, doExplosionVFX, 
                expolosionPropagationSpeed, 0f, doSoundEffects: true, postExplosionSpawnThingDefWater, screenShakeFactor, 
                null, null, postExplosionSpawnSingleThingDef, preExplosionSpawnSingleThingDef);
            if (def.projectile.explosionSpawnsSingleFilth && def.projectile.filth != null && def.projectile.filthCount.TrueMax > 0 && Rand.Chance(def.projectile.filthChance) && !base.Position.Filled(map))
            {
                FilthMaker.TryMakeFilth(base.Position, map, def.projectile.filth, def.projectile.filthCount.RandomInRange);
            }
        }
        public static bool IsAlly(Thing thing, Thing other)
        {
            if(thing.Faction == null || other.Faction == null) return false;
            if(thing.Faction == other.Faction && !thing.HostileTo(other)) return true;
            return false;

        }
    }
}
