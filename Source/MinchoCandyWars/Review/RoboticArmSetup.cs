using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Review;

/// <summary>
/// 饼干操作臂的设施联动装配。这里**只有装配，没有任何补丁**：
///
///   · 给操作臂挂 <see cref="CompProperties_Facility"/>（8 格 / 需要视线 / 每台工作台一个）
///   · 把它加进所有工作台与研究台的 linkableFacilities，并让放置时显示连接范围
///
/// 温度覆盖范围、以及"连接变化后立刻重算"都在
/// <c>CompCellTemperatureOverride</c> 里实现 —— 那是本模组自己的组件，直接改代码即可；
/// 而 <c>CompFacility</c> 本来就提供 OnLinkAdded / OnLinkRemoved 两个事件，
/// 所以原来那两个 Harmony 补丁（ConnectedScope / RefreshOnConnectionChange）都删掉了。
///
/// 为什么 Comp 必须在启动时添加、不能写进 XML：
/// <c>CompProperties_Facility.ResolveReferences</c> 会反向扫描所有 ThingDef，收集
/// linkableFacilities 里包含本建筑的 Def；而操作臂是在启动时才被写进各工作台的
/// linkableFacilities 的。写在 XML 里会在正常 Def 解析阶段执行，那时反向表还是空的。
/// </summary>
[StaticConstructorOnStartup]
public static class RoboticArmSetup
{
    static RoboticArmSetup()
    {
        ThingDef arm = MCW_DefOf.MCW_RoboticArm;
        if (arm == null)
        {
            return;
        }

        // 三个值都等于 CompProperties_Facility 的字段默认值，写出来只是表明设计意图。
        var facility = new CompProperties_Facility
        {
            maxDistance = 8f,
            requiresLOS = true,
            maxSimultaneous = 1,
        };
        arm.comps.Add(facility);
        AddPlaceWorker(arm);

        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (def.thingClass == null ||
                !(typeof(Building_WorkTable).IsAssignableFrom(def.thingClass) ||
                  typeof(Building_ResearchBench).IsAssignableFrom(def.thingClass)))
            {
                continue;
            }

            CompProperties_AffectedByFacilities affected = def.GetCompProperties<CompProperties_AffectedByFacilities>();
            if (affected == null)
            {
                affected = new CompProperties_AffectedByFacilities();
                def.comps.Add(affected);
            }

            affected.linkableFacilities ??= new List<ThingDef>();
            if (!affected.linkableFacilities.Contains(arm))
            {
                affected.linkableFacilities.Add(arm);
            }

            AddPlaceWorker(def);
        }

        // Def 解析阶段已经过去，手动重建操作臂的反向链接表（linkableBuildings）。
        facility.ResolveReferences(arm);
    }

    /// <summary>放置时画出可连接的设施连线。</summary>
    private static void AddPlaceWorker(ThingDef def)
    {
        def.placeWorkers ??= new List<Type>();
        if (!def.placeWorkers.Contains(typeof(PlaceWorker_ShowFacilitiesConnections)))
        {
            def.placeWorkers.Add(typeof(PlaceWorker_ShowFacilitiesConnections));
        }
    }
}
