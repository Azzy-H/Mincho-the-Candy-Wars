#nullable disable
using HarmonyLib;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

public sealed partial class Story
{
    // 标量：flag 单源（flag 自带持久化）。
    public int CompanyDeadline
    {
        get => StoryFlags.GetInt("MCW_Story_CompanyDeadline", -1);
        set => StoryFlags.SetInt("MCW_Story_CompanyDeadline", value);
    }

    void CompletionExpose() { }
}
