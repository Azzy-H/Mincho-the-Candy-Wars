#nullable disable
using MinchoCandyWars;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MinchoCandyWars.QuestStory;

public static class Invitations
{
    static readonly string[] Candy = ["MechaCandy", "AmberonCandy", "NectarCandy", "MechaCandy", "PrismCandy", "LinglonCandy"];
    static readonly int[][] Levels = [
        [12,19,8,8,8,8,8,19,12,19,19,19], [12,19,16,8,8,8,6,12,6,10,12,10],
        [6,8,8,4,19,14,12,10,8,19,18,16], [16,8,19,14,6,8,4,19,12,10,8,19],
        [12,20,8,10,8,10,10,12,10,10,6,10], [12,6,6,4,10,8,12,10,19,10,19,18]];
    static readonly int[][] Passions = [
        [2,2,0,0,0,0,0,2,2,2,2,2], [1,2,2,0,0,0,0,1,0,1,1,0],
        [0,0,0,0,2,1,1,0,0,2,2,1], [1,0,2,1,0,0,0,2,1,1,0,2],
        [1,2,0,1,0,1,1,1,0,1,0,0], [1,0,0,0,1,0,1,0,2,1,2,2]];
    static readonly string[] Skills = ["Shooting","Melee","Construction","Mining","Cooking","Plants","Animals","Crafting","Artistic","Medicine","Social","Intellectual"];
    static readonly string[][] Clothes = [
        ["Mincho_Knitwear","MCW_QinxinCrown"], ["Mincho_KnightlyTiara","Mincho_KnightlyCloak","Mincho_KnightlyDress","MCW_TowerShield"],
        ["MCW_NurseOutfit","MCW_NurseCap"], ["MCW_MechaArmor","MCW_HoloVisor","MCW_MechaWings"], ["MCW_Qipao"], ["MCW_GorgeousDress","MCW_LinglonTiara"]];
    static readonly string[] Weapons = ["", "MeleeWeapon_Mace", "", "MCW_RW_VrocanGatling", "MCW_MW_CandyCane", ""];
    public static void Open()
    {
        var s = Story.Current;
        if (!s.Victory)
        { StoryUtility.Letter("MinchoCandyWars.Invite.Title".Translate(), "MinchoCandyWars.Invite.Locked".Translate()); return; }
        var options = CharacterData.Names.Select((name, i) =>
        {
            var pawn = StoryCampaign.Chief(i);
            bool available = CanInvite(i);
            return new FloatMenuOption(CharacterData.DisplayName(i) + "MinchoCandyWars.Invite.Affinity".Translate(StoryCampaign.Affinity(i)).RawText + (pawn?.Faction == Faction.OfPlayer ? "MinchoCandyWars.Invite.Joined".Translate().RawText : available ? "" : "MinchoCandyWars.Invite.Unavailable".Translate().RawText), available ? () => Confirm(i) : null);
        }).ToList(); Find.WindowStack.Add(new FloatMenu(options));
    }
    public static bool CanInvite(int role)
    {
        var story = Story.Current;
        if (Find.AnyPlayerHomeMap == null || !StoryCampaign.CanInviteCharacter(role) || !StoryCampaign.CanTalk(role)) return false;

        var pawn = StoryCampaign.Chief(role);
        return pawn == null || (!pawn.Dead && !pawn.Downed && pawn.Faction != Faction.OfPlayer &&
            !pawn.IsCaravanMember() && (pawn.Spawned ? pawn.Map.IsPlayerHome : Find.WorldPawns.Contains(pawn)));
    }

    static void Confirm(int role)
    {
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("MinchoCandyWars.Invite.Confirm".Translate(CharacterData.DisplayName(role)), () => Join(role)));
    }
    static void Join(int role)
    {
        var s = Story.Current; var map = Find.AnyPlayerHomeMap;
        if (!CanInvite(role)) return;
        var pawn = StoryCampaign.Chief(role);
        if (pawn != null) UpgradeEquipment(pawn);
        if (pawn?.Spawned == true) { pawn.SetFaction(Faction.OfPlayer); NotifyAccepted(role); return; }
        if (pawn == null)
        {
            pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(MCW_DefOf.MCW_SmartMincho, Faction.OfPlayer, forceGenerateNewPawn: true, fixedGender: Gender.Female, fixedBiologicalAge: 30, canGeneratePawnRelations: false));
            StoryCampaign.SetChiefDefaultName(pawn, role);
            Configure(pawn, role); s.Chiefs[role] = pawn; s.ChiefCreated[role] = true;
        }
        else { Find.WorldPawns.RemovePawn(pawn); pawn.SetFaction(Faction.OfPlayer); }
        PodDelivery.Send([pawn], map);
        NotifyAccepted(role);
    }
    public static void Configure(Pawn pawn, int role)
    {
        var core = pawn.GetComp<CompMinchoCore>(); core.Active = true; core.MinchoCoreGrade = 5; core.MinchoBodyGrade = 5;
        core.CurrentCandyType = DefDatabase<CandyTypeDef>.GetNamed(Candy[role]); core.MinchoCandyValue = core.CurrentMaxCandyValue;
        CharacterPersonality.Apply(pawn, role);
        for (int i = 0; i < Skills.Length; i++)
        {
            var skill = pawn.skills.GetSkill(DefDatabase<SkillDef>.GetNamed(Skills[i]));
            skill.Level = Passions[role][i] > 0 && Rand.Chance(0.8f)
                ? Rand.RangeInclusive(16, 20) : Math.Clamp(Levels[role][i] + Rand.RangeInclusive(-2, 2), 0, 20);
            skill.passion = (Passion)Passions[role][i];
        }
        AllianceBelief.Apply(pawn);
        pawn.health.AddHediff(MCW_HediffDefOf.Mincho_SupplementaryCore, pawn.RaceProps.body.corePart);
        if (role != 5)
        {
            var arms = pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def == MCW_DefOf.Shoulder).ToList();
            foreach (var arm in role == 4 ? arms.TakeLast(1) : arms.Take(2)) pawn.health.AddHediff(MCW_HediffDefOf.ArchotechArm, arm);
            if (role is 0 or 4) foreach (var leg in pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def == MCW_DefOf.Leg).Take(2).ToList()) pawn.health.AddHediff(MCW_HediffDefOf.ArchotechLeg, leg);
        }
        if (role == 0) { pawn.health.AddHediff(MCW_HediffDefOf.MCW_QinxintangBlessing); StoryUtility.EnsureLeaderRegeneration(pawn); }
        if (role == 3)
        {
            pawn.health.AddHediff(MCW_HediffDefOf.Mincho_Humanlike_Body_Hediff);
            if (ModsConfig.BiotechActive) pawn.health.AddHediff(HediffDefOf.MechlinkImplant, pawn.health.hediffSet.GetBrain());
        }
        pawn.apparel.DestroyAll(); pawn.equipment.DestroyAllEquipment();
        foreach (string id in Clothes[role])
        {
            var apparel = (Apparel)Make(id, QualityCategory.Legendary);
            pawn.apparel.Wear(apparel, false);
            if (id is "MCW_LinglonTiara" or "MCW_QinxinCrown") pawn.outfits.forcedHandler.SetForced(apparel, true);
        }
        if (Weapons[role] != "") pawn.equipment.AddEquipment((ThingWithComps)Make(Weapons[role], QualityCategory.Legendary));
        Appearance.Apply(pawn, role);
        pawn.skills.Notify_SkillDisablesChanged();
    }
    static void UpgradeEquipment(Pawn pawn)
    {
        foreach (var item in pawn.apparel.WornApparel.Cast<Thing>().Concat(pawn.equipment.AllEquipmentListForReading))
            item.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Outsider);
    }
    static Thing Make(string id, QualityCategory quality)
    {
        var def = DefDatabase<ThingDef>.GetNamed(id);
        var stuff = def.MadeFromStuff ? def.stuffCategories.Contains(StuffCategoryDefOf.Fabric) ? MCW_ThingDefOf.Hyperweave : GenStuff.DefaultStuffFor(def) : null;
        var plasteel = ThingDefOf.Plasteel;
        if (def.MadeFromStuff && plasteel.stuffProps.categories.Any(def.stuffCategories.Contains)) stuff = plasteel;
        var mint = MCW_ThingDefOf.Mincho_BlocksMintchoco;
        if (id == "MeleeWeapon_Mace" && mint.stuffProps.categories.Any(def.stuffCategories.Contains)) stuff = mint;
        var item = ThingMaker.MakeThing(def, stuff);
        if (item is Apparel) { item.SetColor(UnityEngine.Color.clear); item.SetColor(UnityEngine.Color.white); }
        item.TryGetComp<CompQuality>()?.SetQuality(quality, ArtGenerationContext.Outsider);return item;
    }
    static void NotifyAccepted(int role)
    {
        string name = CharacterData.DisplayName(role);
        string reply = $"MinchoCandyWars.Invite.Reply.{role}".Translate();
        StoryUtility.Letter("MinchoCandyWars.Invite.Accepted".Translate(name), $"{name}: {reply}");
    }
}
