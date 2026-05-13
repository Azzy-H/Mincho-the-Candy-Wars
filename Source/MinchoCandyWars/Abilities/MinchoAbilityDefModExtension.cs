using Verse;

namespace MinchoCandyWars.Abilities
{
    //minchoAbility数据拓展
    public class MinchoAbilityDefModExtension : DefModExtension
    {
        public CandyTypeDef? candyType = null;

        public int requiredMinchoCoreGrade = 0;

        public int requiredMinchoBodyGrade = 0;

        public float requiredMinchoCandyValue = 0f;

        public List<HediffDef> requiredHediffDefs = new List<HediffDef>();

    }
}
