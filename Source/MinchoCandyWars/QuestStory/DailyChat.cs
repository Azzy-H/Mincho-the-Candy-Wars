#nullable disable
using RimWorld;
using Verse;
namespace MinchoCandyWars.QuestStory;
public sealed partial class Story
{
    // 标量：flag 单源。
    internal int ChatDay
    {
        get => StoryFlags.GetInt("MCW_Story_ChatDay", -1);
        set => StoryFlags.SetInt("MCW_Story_ChatDay", value);
    }

    // 集合：保留字段（需要原地 Clear/Add）。
    internal List<int> ChatUsed = new();
    internal List<int> ChatTopicsRead = new();
    void ChatExpose()
    {
        Scribe_Collections.Look(ref ChatUsed, "chatUsed140", LookMode.Value);
        Scribe_Collections.Look(ref ChatTopicsRead, "chatTopicsRead", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            ChatUsed ??= new();
            ChatTopicsRead ??= new();
        }
    }
    // 聊天逻辑已迁到 StoryCampaign（ChatRemaining / ChatReason / HasReadTopic / TryChat）。
}
