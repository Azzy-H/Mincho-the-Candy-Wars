using Verse;

namespace MinchoCandyWars.Anomaly;

/// <summary>
/// 已解除精神影响的无尽冰淇淋桶。
///
/// 与活跃版是**两个独立的 ThingDef**（营养 0.2 / 1.0 直接写在各自 Def 里），
/// 共用同一个类，只把"食用后致瘾"这一步关掉。
/// </summary>
public class ReleasedBucket : EndlessBucket
{
    protected override bool ShouldIntoxicate(Pawn p) => false;
}
