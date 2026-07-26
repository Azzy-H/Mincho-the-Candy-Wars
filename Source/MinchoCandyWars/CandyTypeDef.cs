using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MinchoCandyWars
{
    public class CandyTypeDef : Def
    {
        public List<CandyTypeStage> coreStages = new List<CandyTypeStage>();
        public List<CandyTypeStage> bodyStages = new List<CandyTypeStage>();
        public override IEnumerable<string> ConfigErrors()
        {
            if(coreStages.Count != 5 || bodyStages.Count != 5) yield return $"CandyTypeDef {defName} must have exactly 5 stages for both core and body.";
        }
    }
    public class CandyTypeStage
    {
        //用于实现无法简单通过stat实现的功能，在切换状态时添加hediff，移除由hediff自己维护
        public List<HediffDef>? gainHediffs;
        public List<StatModifier>? statOffsets;
        public List<StatModifier>? statFactors;
    }

}
