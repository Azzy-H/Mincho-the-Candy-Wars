using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    [DefOf]
    public static class MCW_ThingDefOf
    {
        public static ThingDef Mincho_ThingDef = null!;
        // 盾牌本体与"举盾/背盾"Hediff：用 DefOf 引用，避免代码里出现 defName 字符串。
        public static ThingDef MCW_TowerShield = null!;
        public static ThingDef MCW_CookiesShield = null!;
        // 复用型灭火器与超低温恶魔菇。
        public static ThingDef MCW_FirefoamPopper = null!;
        public static ThingDef MCW_Frozen_Devilstrand_Plant = null!;
        public static ThingDef MCW_RoboticArm = null!;
        // ── 无尽冰淇淋桶（Anomaly 内容）────────────────────────────────────────
        // 这些 Def 定义在 1.7/Anomaly/Defs 下，只有启用 Anomaly 时才存在。
        // [MayRequireAnomaly] 让 DefOfHelper 在 DLC 未启用时跳过它们（字段保持 null），
        // 因此引用处必须容忍 null —— 不要在这里去掉特性。
        [MayRequireAnomaly] public static ThingDef MCW_EndlessIceCream = null!;
        [MayRequireAnomaly] public static ThingDef MCW_EndlessIceCream_Released = null!;
        // ── 冰淇淋桶载客飞行器（Odyssey 内容）────────────────────────────────
        // Def 在 1.7/Odyssey/Defs 下，只有启用 Odyssey 时才存在。
        [MayRequireOdyssey] public static ThingDef MCW_BucketShuttle = null!;
        public static ThingDef Mincho_Filth_BloodDef = null!;
        public static ThingDef Mincho_Mintchoco = null!;
        // 同名 ThingDef（史莱姆本体），与上面 PawnKindDef 区分
        public static ThingDef MCW_MinchoSlime = null!;
        public static ThingDef Mincho_MintchocoWall = null!;
        public static ThingDef MCW_AmberonPositionShield = null!;
        public static ThingDef MCW_PrismSwordSlowZone = null!;
        public static ThingDef MCW_PrismGlowFlyer = null!;
        // ── CompletionFixes 相关（隐藏糖饰 / 瘫痪枪 / 弹道拖尾 / 糖果手杖）──────────
        public static ThingDef MCW_VrocanGatling_Bullet = null!;
        public static ThingDef MCW_Paralyzer = null!;
        // 随机冰淇淋（琥珀护盾拦截转化用）；这三个由 Mincho_HPF 提供
        public static ThingDef Mincho_BluePolarBear = null!;
        public static ThingDef Mincho_PinkFallInLove = null!;
        // 联动种族本体（AlienRace.ThingDef_AlienRace，defName 就是 MinchoWitch）
        public static ThingDef MinchoWitch = null!;
        // 机械傀儡本体
        public static ThingDef MCW_Mech_Golem = null!;
        public static ThingDef MCW_Beta_NanoShapingFluid = null!;
        public static ThingDef MCW_Bikini = null!;
        public static ThingDef MCW_BikiniBW = null!;
        public static ThingDef MCW_BrownDress = null!;
        public static ThingDef MCW_Chocolate_Console = null!;
        public static ThingDef MCW_CocoaBath = null!;
        public static ThingDef MCW_GorgeousDress = null!;
        public static ThingDef MCW_LinglonTiara = null!;
        public static ThingDef MCW_MechaArmor = null!;
        public static ThingDef MCW_MintChocolateLongsword = null!;
        public static ThingDef MCW_NurseCap = null!;
        public static ThingDef MCW_NurseOutfit = null!;
        public static ThingDef MCW_QinxinCrown = null!;
        public static ThingDef MCW_Qipao = null!;
        public static ThingDef MCWQ101_ContactComputer = null!;
        public static ThingDef MCWQ105_BucketCrashing = null!;
        public static ThingDef MCWQ105_BucketWreck = null!;
        public static ThingDef Mincho_BlocksMintchoco = null!;
        public static ThingDef Mincho_KnightlyTiara = null!;
        public static ThingDef Mincho_MintchocoDonutTiara = null!;
        public static ThingDef HiddenConduit = null!;
        public static ThingDef Hyperweave = null!;
    }
}