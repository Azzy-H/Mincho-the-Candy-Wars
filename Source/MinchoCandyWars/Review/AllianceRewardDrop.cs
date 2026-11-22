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
            DropPodUtility.DropThingGroupsNear(__state.Center,__state.Map,Groups(__state.Items),openDelay:110,instaDrop:false,leaveSlag:false,canRoofPunch:false,forbid:false,allowFogged:false);
    }
    public static Exception Finalizer(Exception __exception,Batch __state)
    {
        current=__state.Previous;
        return __exception;
    }
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code=instructions.ToList();
        var collect=AccessTools.Method(typeof(AllianceDelivery),nameof(Collect));
        var parameterTypes=collect.GetParameters().Select(p=>p.ParameterType).ToArray();
        var placements=code.Where(ins=>ins.operand is MethodInfo m
            && m.DeclaringType==typeof(GenPlace) && m.Name==nameof(GenPlace.TryPlaceThing)).ToList();
        // Validate the entire replacement before changing IL. An incompatible overload
        // must retain the original delivery rather than producing an invalid method.
        if(placements.Count!=1 || placements[0].operand is not MethodInfo target
            || !target.IsStatic || target.ReturnType!=collect.ReturnType
            || !target.GetParameters().Select(p=>p.ParameterType).SequenceEqual(parameterTypes))
        {
            Log.Error("[MCW]AllianceDelivery placement signature changed; keeping original delivery");
            return code;
        }
        placements[0].opcode=OpCodes.Call;
        placements[0].operand=collect;
        return code;
    }
}

