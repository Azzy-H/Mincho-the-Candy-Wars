using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

/// <summary>
/// 剧情用到的静态角色/任务数据。
///
/// 之前这些常量以 partial 成员的形式散落在 Story 的各个文件里
/// （Conversations.Names/Portraits、Campaign.Speakers/MissionDefs），
/// 被 9 个以上文件以 Story.XXX 的形式引用，导致 Story 职责膨胀。
/// 集中到独立静态类，Story 只保留状态与生命周期。
/// </summary>
public static class CharacterData
{
    /// <summary>六位珉巧角色的默认名（按 role 索引）。</summary>
    public static readonly string[] Names =
        ["沁心糖", "琥珀糖", "甘露糖", "千机糖", "琉璃糖", "玲珑糖"];

    /// <summary>六位角色的立绘路径（按 role 索引）。</summary>
    public static readonly string[] Portraits =
    [
        "Events/Portraits/MCW_Qinxintang_Smile",
        "Events/Portraits/MCW_AmberonCandy",
        "MCWQ/Portraits/NectarClean100",
        "Events/Portraits/MCW_MechaCandy",
        "MCWQ/Portraits/Prism101",
        "Events/Portraits/MCW_LinglonCandy",
    ];

    /// <summary>各阶段任务在发起时使用的说话人（按 kind 索引）。</summary>
    public static readonly string[] Speakers =
    [
        "甘露糖", "琥珀糖", "琉璃糖", "千机糖", "琥珀糖", "沁心糖", "沁心糖",
        "玲珑糖", "琉璃糖", "玲珑糖", "千机糖", "琥珀糖", "甘露糖",
    ];

    /// <summary>各阶段任务对应的 QuestScriptDef（按 kind 索引）。</summary>
    public static readonly string[] MissionDefs =
    [
        Story.GuestDef,
        "MCW_QuestB2_SmartMinchoRescue",
        "MCW_QuestB3_RecycleOutpost",
        "MCW_QuestB4_BetaFluidRuin",
        "MCW_QuestB5_AllianceOutpostDefense",
        "MCW_QuestFF1_RecycleHQ",
        "MCWQ101_Delivery7",
        "MCWQ101_Delivery8",
        "MCWQ101_Remnant",
        "MCWQ103_Delivery10",
        "MCWQ103_Delivery11",
        "MCWQ105_Crash",
        "MCWQ105_Cargo",
    ];

    /// <summary>角色的显示名：优先取翻译键，回退到默认名。</summary>
    public static string DisplayName(int role) =>
        $"MinchoCandyWars.Alliance.Name.{role}".TryTranslate(out var name) ? name.RawText : Names[role];
}
