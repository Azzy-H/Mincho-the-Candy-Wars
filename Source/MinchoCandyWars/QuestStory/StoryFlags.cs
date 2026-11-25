using System.Collections.Generic;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

/// <summary>
/// 剧情的 flag 化状态层。
///
/// 设计目标：剧情状态不再由 Story 的实例字段持有，而是统一存在
/// StellarisEventFlagManager（GameComponent，随存档持久化）里。
/// Story 只保留 GameComponentTick —— 遍历当前 Quest 调用各 QuestPart
/// 的 Check()/Prepare()，为没有 Tick 的 QuestPart 补上周期驱动。
///
/// 所有 key 统一用 MCW_Story_ 前缀，便于在 flag 控制台里过滤。
/// </summary>
public static class StoryFlags
{
    // ──────────────────── 标量 ────────────────────

    public static int GetInt(string key, int fallback = -1)
    {
        var flag = StellarisEventFlagManager.GetFlag(key);
        return flag != null ? (int)flag.floatValue : fallback;
    }

    public static void SetInt(string key, int value) =>
        StellarisEventFlagManager.SetFlag(key, (float)value);

    public static bool GetBool(string key, bool fallback = false)
    {
        var flag = StellarisEventFlagManager.GetFlag(key);
        return flag != null ? flag.boolValue : fallback;
    }

    public static void SetBool(string key, bool value) =>
        StellarisEventFlagManager.SetFlag(key, value);

    public static string GetString(string key, string fallback = "")
    {
        var flag = StellarisEventFlagManager.GetFlag(key);
        return flag != null ? flag.stringValue : fallback;
    }

    public static void SetString(string key, string value) =>
        StellarisEventFlagManager.SetFlag(key, value);

    // ──────────────────── 引用 ────────────────────

    public static Pawn GetPawn(string key) =>
        StellarisEventFlagManager.GetTrackedThing(key) as Pawn;

    public static void SetPawn(string key, Pawn pawn)
    {
        if (pawn == null)
        {
            StellarisEventFlagManager.RemoveFlag(key);
        }
        else
        {
            StellarisEventFlagManager.SetFlag(key, (Thing)pawn);
        }
    }

    public static Quest GetQuest(string key) =>
        StellarisEventFlagManager.GetTrackedQuest(key);

    public static void SetQuest(string key, Quest quest)
    {
        if (quest == null)
        {
            StellarisEventFlagManager.RemoveFlag(key);
        }
        else
        {
            StellarisEventFlagManager.SetFlag(key, quest);
        }
    }

    /// <summary>
    /// 集合只读视图。注意：返回的是每次新解析的列表，
    /// 对返回值的 Add/Remove 不会写回 —— 集合类状态需保留在 Story 字段里。
    /// </summary>
    public static List<Pawn> GetPawns(string key) =>
        StellarisEventFlagManager.GetTrackedThings(key).ConvertAll(t => t as Pawn);

    public static void SetPawns(string key, List<Pawn> pawns)
    {
        if (pawns == null || pawns.Count == 0)
        {
            StellarisEventFlagManager.RemoveFlag(key);
            return;
        }

        StellarisEventFlagManager.SetFlag(key, pawns.ConvertAll(p => (Thing)p));
    }
}
