using MinchoCandyWars.Abilities;
using MinchoCandyWars.Buff;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Data
{
    // 预加载def数据
    [StaticConstructorOnStartup]
    public static class DefDataPreloading
    {
        public static readonly List<AbilityDef> MinchoCandyAbilityDefs;

        static DefDataPreloading()
        {
            // 预加载MinchoCandyAbility的AbilityDef
            MinchoCandyAbilityDefs = new List<AbilityDef>();
            foreach (var abilityDef in DefDatabase<AbilityDef>.AllDefs)
            {
                // 只保存拥有MinchoAbilityDefModExtension的AbilityDef
                var extension = abilityDef.GetModExtension<MinchoAbilityDefModExtension>();
                if (extension != null)
                {
                    MinchoCandyAbilityDefs.Add(abilityDef);
                }
            }
        }
    }
}
