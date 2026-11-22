using RimWorld;
using Verse;
namespace MinchoCandyWars.QuestStory;

// Campaign reinforcements and old support squads only; never traders or player pawns.
public static class AllianceBattleLoadout
{
    public static void Apply(Pawn pawn)
    {
        if (!pawn.RaceProps.Humanlike || pawn.equipment == null) return;
        string kind = pawn.kindDef.defName;
        string? replacement = kind switch
        {
            "Mincho_Mercenary" => "MCW_RW_VrocanGatling",
            "MCW_SmartMincho" => "MCW_RW_VrocanGatling",
            "Mincho_Defender" or "Mincho_Knight_Elite" => "MCW_MW_CandyCane",
            _ => pawn.equipment.Primary == null || !pawn.equipment.Primary.def.defName.StartsWith("MCW_") ? "MCW_MW_CandyCane" : null
        };
        if (replacement != null)
        {
            pawn.equipment.DestroyAllEquipment(); // Generated gear only; no player inventory is involved.
            var weaponDef = DefDatabase<ThingDef>.GetNamed(replacement);
            var weapon = (ThingWithComps)ThingMaker.MakeThing(weaponDef, weaponDef.MadeFromStuff ? GenStuff.DefaultStuffFor(weaponDef) : null);
            pawn.equipment.AddEquipment(weapon);
        }
        var primary = pawn.equipment.Primary;
        var quality = primary?.TryGetComp<CompQuality>();
        if (quality != null && quality.Quality < QualityCategory.Good) quality.SetQuality(QualityCategory.Good, ArtGenerationContext.Outsider);
        if (pawn.skills != null)
        {
            var skill = pawn.skills.GetSkill(primary?.def.IsRangedWeapon == true ? SkillDefOf.Shooting : SkillDefOf.Melee);
            if (skill.Level < 12) skill.Level = 12;
        }
        if (kind == "MCW_SmartMincho")
        {
            foreach(var name in new[]{"Mincho_ChocoCrust","Mincho_MercenaryArmor"})
            {
                var armor=(Apparel)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(name));
                pawn.apparel.Wear(armor,dropReplacedApparel:false);
            }
        }
    }
}
