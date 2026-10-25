using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Review;

/// <summary>
/// 把饼干操作臂从「相关建造」gizmo 里滤掉。
///
/// 为什么要滤：RoboticArmSetup 把操作臂写进了每个工作台 / 研究台的
/// linkableFacilities，而原版 <see cref="BuildFacilityCommandUtility.BuildFacilityCommands"/>
/// 正是遍历这张表来生成按钮的，于是选中任何工作台都会多出一个「建造饼干操作臂」。
/// 调用方有四处：Building / Blueprint_Build / Frame / Blueprint_Install，
/// 即建筑、蓝图、框架、安装蓝图的选中面板。
///
/// 为什么不能靠 Def 或直接删表：原版没有「不要把此设施显示在相关建造里」的字段；
/// 而工作台侧的 linkableFacilities 同时是链接发现路径
/// （CompAffectedByFacilities.LinkToNearbyFacilities 靠它找已存在的设施），
/// 删掉会导致「先放操作臂、后放工作台」时连不上。所以只能在这条菜单链路上过滤。
///
/// 补丁点的选择：打在 BuildFacilityCommands 而不是更外层的 RelatedBuildCommands，
/// 这样只影响「设施自动生成」的那部分，其它建筑显式配置的
/// building.relatedBuildCommands / relatedTerrain 不会被误伤。
/// </summary>
[HarmonyPatch(typeof(BuildFacilityCommandUtility), nameof(BuildFacilityCommandUtility.BuildFacilityCommands))]
public static class RoboticArmMenu
{
    public static void Postfix(ref IEnumerable<Command> __result) => __result = WithoutArm(__result);

    private static IEnumerable<Command> WithoutArm(IEnumerable<Command> commands)
    {
        foreach (Command command in commands)
        {
            if (command is not Designator_Build build || build.PlacingDef != MCW_DefOf.MCW_RoboticArm)
            {
                yield return command;
            }
        }
    }
}
