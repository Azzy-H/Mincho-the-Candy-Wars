using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    [DefOf]
    public static class MCW_DefOf
    {
        public static ThingDef Mincho_ThingDef = null!;

        public static HediffDef MCW_ProductionQualityBoost = null!;

        public static StatDef MCW_WorkSpeedGlobalFactorMech = null!;

        // 盾牌三防：基值写在盾牌 Def 的 equippedStatOffsets，
        // 材质贡献由这三个 StatDef 上的 StatPart_Stuff 计算（见 MCW_ShieldArmorStats.xml）。
        public static StatDef MCW_ShieldArmor_Sharp = null!;
        public static StatDef MCW_ShieldArmor_Blunt = null!;
        public static StatDef MCW_ShieldArmor_Heat = null!;

        // 盾牌本体与"举盾/背盾"Hediff：用 DefOf 引用，避免代码里出现 defName 字符串。
        public static ThingDef MCW_TowerShield = null!;
        public static ThingDef MCW_CookiesShield = null!;
        public static HediffDef MCW_TowerShield_DraftedHediff = null!;
        public static HediffDef MCW_CookiesShield_DraftedHediff = null!;

        // 复用型灭火器与超低温恶魔菇。
        public static ThingDef MCW_FirefoamPopper = null!;
        public static ThingDef MCW_Frozen_Devilstrand_Plant = null!;
        public static ThingDef MCW_RoboticArm = null!;

        // 珉巧种族（来自前置 mod，硬依赖）。
        public static FleshTypeDef Mincho_FleshTypeDef = null!;
        public static ThingCategoryDef Mincho_Food = null!;

        // ── 无尽冰淇淋桶（Anomaly 内容）────────────────────────────────────────
        // 这些 Def 定义在 1.7/Anomaly/Defs 下，只有启用 Anomaly 时才存在。
        // [MayRequireAnomaly] 让 DefOfHelper 在 DLC 未启用时跳过它们（字段保持 null），
        // 因此引用处必须容忍 null —— 不要在这里去掉特性。
        [MayRequireAnomaly] public static ThingDef MCW_EndlessIceCream = null!;
        [MayRequireAnomaly] public static ThingDef MCW_EndlessIceCream_Released = null!;
        [MayRequireAnomaly] public static NeedDef Chemical_MCW_Mint = null!;
        [MayRequireAnomaly] public static HediffDef MCW_MintAddiction = null!;
        [MayRequireAnomaly] public static HediffDef MCW_MintDaze = null!;
        [MayRequireAnomaly] public static HediffDef MCW_MintFreedom = null!;
        [MayRequireAnomaly] public static ThoughtDef MCW_MintDelight = null!;
        [MayRequireAnomaly] public static ThoughtDef MCW_MintDisgust = null!;

        // ── 冰淇淋桶载客飞行器（Odyssey 内容）────────────────────────────────
        // Def 在 1.7/Odyssey/Defs 下，只有启用 Odyssey 时才存在。
        [MayRequireOdyssey] public static ThingDef MCW_BucketShuttle = null!;

        //public static StatDef MCW_AnimalsLearningFactor = null!;
        public static StatDef MCW_ArtisticLearningFactor = null!;
        public static StatDef MCW_ConstructionLearningFactor = null!;
        public static StatDef MCW_CookingLearningFactor = null!;
        public static StatDef MCW_CraftingLearningFactor = null!;
        public static StatDef MCW_IntellectualLearningFactor = null!;
        public static StatDef MCW_MedicalLearningFactor = null!;
        public static StatDef MCW_MeleeLearningFactor = null!;
        public static StatDef MCW_MiningLearningFactor = null!;
        public static StatDef MCW_PlantsLearningFactor = null!;
        public static StatDef MCW_ShootingLearningFactor = null!;
        public static StatDef MCW_SocialLearningFactor = null!;
        public static HediffDef MCW_MechBoost = null!;
        public static ThingDef Mincho_Filth_BloodDef = null!;
        public static ThingDef Mincho_Mintchoco = null!;
        public static PawnKindDef MCW_MinchoSlime = null!;

        // Nectar Candy
        public static HediffDef MCW_MannaHeart = null!;
        public static GameConditionDef MCW_MannaHeartCondition = null!;
        public static HediffDef MCW_MannaConnection = null!;
        public static HediffDef MCW_MinchoTentacle = null!;
        public static HediffDef MCW_MannaFairy = null!;
        public static HediffDef MCW_MannaFairyBuff = null!;
        // Amberon Candy
        public static HediffDef MCW_AmberonShield = null!;
        public static HediffDef MCW_SplashedIceCream = null!;
        public static HediffDef MCW_AmberonHeart = null!;
        public static HediffDef MCW_AmberonSlow = null!;
        public static HediffDef MCW_AmberonHeartMood = null!;
        public static ThingDef Mincho_MintchocoWall = null!;
        public static ThingDef MCW_AmberonPositionShield = null!;
        public static CandyTypeDef NectarCandy = null!;
        public static CandyTypeDef MechaCandy = null!;
        public static CandyTypeDef AmberonCandy = null!;
        // Prism Candy
        public static CandyTypeDef PrismCandy = null!;
        public static HediffDef MCW_PrismGlowMoveSpeed = null!;
        public static HediffDef MCW_PrismTerrainIgnore = null!;
        public static HediffDef MCW_PrismSwordSlowHediff = null!;
        public static ThingDef MCW_PrismSwordSlowZone = null!;
        public static ThingDef MCW_PrismGlowFlyer = null!;
        // 琉璃糖流派技能特效
        public static FleckDef MCW_Fleck_ThousandBladesSlash = null!;
        public static FleckDef MCW_Fleck_FrostSwordShadowSlash = null!;
        public static FleckDef MCW_Fleck_PrismGlowImpact = null!;
        // Linglon Candy
        public static CandyTypeDef LinglonCandy = null!;
        public static HediffDef MCW_LinglonAllFocus = null!;
        public static HediffDef MCW_LinglonAura = null!;
        public static HediffDef MCW_LinglonAuraDebuff = null!;
        public static HediffDef MCW_LinglonAmoebaBoon = null!;
        public static HediffDef MCW_LinglonAmoebaBoonBuff = null!;
        public static GameConditionDef MCW_LinglonAuraCondition = null!;

        public static HediffDef MCW_CandyChangeExhaustion = null!;
        public static HediffDef MCW_Intel = null!;
        //public static HediffDef MCW_SnowBoost = null!;

        // ── CompletionFixes 相关（隐藏糖饰 / 瘫痪枪 / 弹道拖尾 / 糖果手杖）──────────
        public static ThingDef MCW_HoloVisor = null!;
        public static FactionDef MCW_RecycleCorp_Faction = null!;
        public static ThingDef MCW_VrocanGatling_Bullet = null!;
        public static ThingDef MCW_MW_CandyCane = null!;
        public static ThingDef MCW_Paralyzer = null!;
        public static HediffDef MCW_Paralyzed047 = null!;
        public static FleckDef MCW_CandyTrail047 = null!;
        // 随机冰淇淋（琥珀护盾拦截转化用）；这三个由 Mincho_HPF 提供
        public static ThingDef Mincho_BluePolarBear = null!;
        public static ThingDef Mincho_PinkFallInLove = null!;

        // ── 联盟派系与名字池 ────────────────────────────────────────────────
        public static FactionDef MCW_Alliance_Faction = null!;
        public static RulePackDef MCW_AllianceCandyNames = null!;

        // ── 冰淇淋产量信条（Ideology 内容）──────────────────────────────────
        [MayRequireIdeology] public static MemeDef Structure_TheisMincho = null!;
        [MayRequireIdeology] public static PreceptDef MCW_IceCreamYield_Boosted = null!;
        // 冰淇淋分类由 Mincho_HPF 提供，与 DLC 无关
        public static ThingCategoryDef Mincho_IceCream = null!;

        // ── 机械傀儡（MinchoWitch，About.xml 里的硬依赖）──────────────────────
        public static PawnKindDef MinchoWitch_MinchoGolem = null!;
        public static HediffDef MinchoWitch_MinchoGolem_NoPain = null!;
        // 联动种族本体（AlienRace.ThingDef_AlienRace，defName 就是 MinchoWitch）
        public static ThingDef MinchoWitch = null!;
        // 机械傀儡本体
        public static ThingDef MCW_Mech_Golem = null!;
        // 原版的 Crush 没有出现在 HediffDefOf 里，这里补一个。
        public static HediffDef Crush = null!;
    }
}