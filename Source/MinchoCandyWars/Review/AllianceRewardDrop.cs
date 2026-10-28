using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Misc;

// Reuse original reward selection, stack splitting, payment, quest flags and cooldown.
// Collect only their original ground-placement calls, then launch one pod per resource type.
[HarmonyPatch]
public static class AllianceDelivery
{
    public class Batch
    {
        public Batch Previous;
        public Map Map;
        public IntVec3 Center;
        public List<Thing> Items=new();
    }
    [ThreadStatic] private static Batch current;
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(MinchoCandyWars.Effect_GrantAllianceSubsidy),"Execute");
        yield return AccessTools.Method(typeof(MinchoCandyWars.Effect_BuyAllianceItem),"Execute");
        yield return AccessTools.Method(typeof(MinchoCandyWars.QuestParts.QuestPart_GiveAllianceReward),"GiveItemRewards");
        yield return AccessTools.Method(typeof(MinchoCandyWars.IncidentWorker_RecruitMincho),"GiveReward");
    }
    public static void Prefix(out Batch __state)=>__state=current=new Batch{Previous=current};
    public static bool Collect(Thing thing,IntVec3 center,Map map,ThingPlaceMode mode,Action<Thing,int> placedAction,Predicate<IntVec3> validator,Rot4? rot,int squareSize)
    {
        current.Map=map;current.Center=center;current.Items.Add(thing);
        return true;
    }
    public static List<List<Thing>> Groups(IEnumerable<Thing> items)=>items.GroupBy(t=>t.def).Select(g=>g.ToList()).ToList();
    public static void Postfix(Batch __state)
    {
        if(__state.Items.Count>0)
            DropPodUtility.DropThingGroupsNear(__state.Center,__state.Map,Groups(__state.Items),openDelay:110,leaveSlag:false,canRoofPunch:false,forbid:false,allowFogged:false);
    }
    public static Exception Finalizer(Exception __exception,Batch __state)
    {
        current=__state.Previous;
        return __exception;
    }
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code=instructions.ToList();int hits=0;
        foreach(var ins in code)
            if(ins.operand is MethodInfo m && m.DeclaringType==typeof(GenPlace) && m.Name==nameof(GenPlace.TryPlaceThing))
            {
                if(m.GetParameters().Length!=8 || m.GetParameters()[0].ParameterType!=typeof(Thing))Log.Error("[MCW]AllianceDelivery unexpected placement overload");
                ins.opcode=OpCodes.Call;ins.operand=AccessTools.Method(typeof(AllianceDelivery),nameof(Collect));hits++;
            }
        if(hits!=1) Log.Error("[MCW]AllianceDelivery expected one delivery call, got " + hits);
        return code;
    }
}
