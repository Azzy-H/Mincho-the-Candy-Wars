using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    [DefOf]
    public static class MCW_DefOf
    {
        // 使用量大的 Def 类型已拆到独立类：MCW_ThingDefOf / MCW_HediffDefOf / MCW_StatDefOf / MCW_EventDefOf

        public static FleshTypeDef Mincho_FleshTypeDef = null!;
        public static ThingCategoryDef Mincho_Food = null!;

        [MayRequireAnomaly] public static NeedDef Chemical_MCW_Mint = null!;
        [MayRequireAnomaly] public static ThoughtDef MCW_MintDelight = null!;
        [MayRequireAnomaly] public static ThoughtDef MCW_MintDisgust = null!;

        public static PawnKindDef MCW_MinchoSlime = null!;

        public static GameConditionDef MCW_MannaHeartCondition = null!;
        public static ThoughtDef MCW_AmberonHeartInspired = null!;
        public static CandyTypeDef NectarCandy = null!;
        public static CandyTypeDef MechaCandy = null!;
        public static CandyTypeDef AmberonCandy = null!;
        public static CandyTypeDef PrismCandy = null!;
        public static FleckDef MCW_Fleck_ThousandBladesSlash = null!;
        public static FleckDef MCW_Fleck_FrostSwordShadowSlash = null!;
        public static FleckDef MCW_Fleck_PrismGlowImpact = null!;
        public static CandyTypeDef LinglonCandy = null!;
        public static GameConditionDef MCW_LinglonAuraCondition = null!;

        public static FactionDef MCW_RecycleCorp_Faction = null!;
        public static FleckDef MCW_CandyTrail047 = null!;

        public static FactionDef MCW_Alliance_Faction = null!;
        public static RulePackDef MCW_AllianceCandyNames = null!;

        [MayRequireIdeology] public static MemeDef Structure_TheisMincho = null!;
        [MayRequireIdeology] public static PreceptDef MCW_IceCreamYield_Boosted = null!;
        public static ThingCategoryDef Mincho_IceCream = null!;

        public static PawnKindDef MinchoWitch_MinchoGolem = null!;

        public static AbilityDef MCW_BoostGolemJump = null!;
        public static HairDef MCW_Hair_Linglon174 = null!;
        public static IncidentDef MCW_GiveQuest_QS1_MinchoMincho = null!;
        public static JobDef MCW_Nuzzle = null!;
        public static JobDef MCW_UseCocoaBath = null!;
        public static LetterDef MCWQ097_ContactLetter = null!;
        public static MinchoCandyWars.QuestStory.HeadquartersBlueprintDef MCW_UserHeadquarters = null!;
        public static PawnKindDef MCW_RecycleCorp_Leader = null!;
        public static PawnKindDef MCW_RecycleCorp_Member = null!;
        public static PawnKindDef MCW_SmartMincho = null!;
        public static QuestScriptDef MCW_QS1_HuntedMincho = null!;
        public static QuestScriptDef MCW_QuestFF1_RecycleHQ = null!;
        public static QuestScriptDef MCWQ100_AllianceSeries = null!;
        public static SitePartDef MCW_RecycleHQ = null!;
        public static StorytellerDef MCW_Qiaoming = null!;
        public static ThoughtDef MCW_CocoaBathRelaxed = null!;
        public static ThoughtDef MCW_NuzzledByMech = null!;
        public static TransportShipDef MCW_BucketShuttle_Ship = null!;

        public static BodyDef Mincho_BodyDef = null!;
        public static BodyTypeDef Mincho_Body_Large_Mintchoco_Humanlike = null!;
        public static PawnKindDef Mincho_Mercenary = null!;
        public static PawnKindDef Mincho_SpaceRefugee = null!;

        public static BodyDef Mech_Light = null!;
        public static BodyPartDef Leg = null!;
        public static BodyPartDef Shoulder = null!;
        public static FleckDef SparkFlash = null!;
        public static MentalStateDef GiveUpExit = null!;
        public static QuestScriptDef Hospitality_Refugee = null!;
        public static SoundDef MeleeHit_Metal_Sharp = null!;
    }
}
