using MinchoCandyWars;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.Anomaly;

/// <summary>
/// 把原版"神秘货物"任务的货物换成无尽冰淇淋桶。
///
/// <c>QuestNode_Root_MysteriousCargo</c> 是 abstract，<c>GenerateThing</c> 是它留给子类的
/// 抽象方法 —— 投送延迟、落点、奖励、信件全部复用原版，不需要任何补丁。
/// </summary>
public class MintCargo : QuestNode_Root_MysteriousCargo
{
    protected override Thing GenerateThing(Pawn p) => ThingMaker.MakeThing(MCW_DefOf.MCW_EndlessIceCream);
}
