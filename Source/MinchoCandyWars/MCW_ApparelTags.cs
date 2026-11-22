using System.Collections.Generic;
using RimWorld;

namespace MinchoCandyWars;

/// <summary>
/// 本 mod 用到的服饰 tag。判定一律走 tag 而不是具体 ThingDef，
/// 新增同类服饰只需要在 Def 上打同名 tag。
/// </summary>
public static class MCW_ApparelTags
{
    /// <summary>遮住面部的服饰（面罩类）。</summary>
    public const string FaceCover = "MCW_FaceCover";

    public static bool HasTag(Apparel? apparel, string tag)
    {
        List<string>? tags = apparel?.def?.apparel?.tags;
        if (tags == null) return false;

        for (int i = 0; i < tags.Count; i++)
            if (tags[i] == tag) return true;

        return false;
    }
}
