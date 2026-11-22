using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Render
{
    /// <summary>
    /// 糖饰渲染节点的属性：声明「佩戴带这些 tag 的服饰时隐藏糖饰」。
    /// 字段名与语义对齐 HAR 的 AlienRace.ExtendedGraphics.ConditionApparel，
    /// 将来若把糖饰迁成 HAR 的 bodyAddon，这套规则可以原样平移。
    /// </summary>
    public class PawnRenderNodeProperties_CandyDecoration : PawnRenderNodeProperties
    {
        /// <summary>佩戴任一含这些 tag 的服饰时隐藏糖饰。留空表示永不隐藏。</summary>
        public List<string> hiddenUnderApparelTag = new List<string>();

        public bool HiddenUnder(Apparel? apparel)
        {
            if (hiddenUnderApparelTag.Count == 0) return false;

            for (int i = 0; i < hiddenUnderApparelTag.Count; i++)
                if (MCW_ApparelTags.HasTag(apparel, hiddenUnderApparelTag[i])) return true;

            return false;
        }
    }
}
