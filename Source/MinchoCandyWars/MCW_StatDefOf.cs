using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    [DefOf]
    public static class MCW_StatDefOf
    {
        public static StatDef MCW_WorkSpeedGlobalFactorMech = null!;
        // 盾牌三防：基值写在盾牌 Def 的 equippedStatOffsets，
        // 材质贡献由这三个 StatDef 上的 StatPart_Stuff 计算（见 MCW_ShieldArmorStats.xml）。
        public static StatDef MCW_ShieldArmor_Sharp = null!;
        public static StatDef MCW_ShieldArmor_Blunt = null!;
        public static StatDef MCW_ShieldArmor_Heat = null!;
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
    }
}