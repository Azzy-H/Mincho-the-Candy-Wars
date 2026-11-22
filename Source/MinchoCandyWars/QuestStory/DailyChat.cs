#nullable disable
using RimWorld;
using Verse;
namespace MinchoCandyWars.QuestStory;
public sealed partial class Story
{
    int ChatDay = -1;
    List<int> ChatUsed = new();
    List<int> ChatTopicsRead = new();
    void ChatExpose()
    {
        Scribe_Values.Look(ref ChatDay, "chatDay140", -1);
        Scribe_Collections.Look(ref ChatUsed, "chatUsed140", LookMode.Value);
        Scribe_Collections.Look(ref ChatTopicsRead, "chatTopicsRead", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            ChatUsed ??= new();
            ChatTopicsRead ??= new();
        }
    }
    void RefreshChat()
    {
        int day = Find.TickManager.TicksGame / 60000;
        if (day != ChatDay) { ChatDay = day; ChatUsed.Clear(); ChatTopicsRead.Clear(); }
        while (ChatUsed.Count < Names.Length) ChatUsed.Add(0);
    }
    public int ChatRemaining(int role) { RefreshChat(); return Math.Max(0, 3 - ChatUsed[role]); }
    public string ChatReason(int role)
    {
        RefreshChat();
        if (!CanTalk(role)) return "MinchoCandyWars.Chat.Unavailable".Translate().RawText;
        if ((ChatDay + role) % Names.Length >= 4) return "MinchoCandyWars.Chat.Away".Translate().RawText;
        return ChatRemaining(role) == 0 ? "MinchoCandyWars.Chat.DailyLimit".Translate().RawText : null;
    }
    public bool HasReadTopic(int role, int topic)
    {
        RefreshChat();
        return ChatTopicsRead.Contains(role * 100 + topic);
    }
    public bool TryChat(int role, int? topic = null)
    {
        if (ChatReason(role) != null || (topic.HasValue && HasReadTopic(role, topic.Value))) return false;
        if (ChatUsed[role] == 0) AddAffinity(role, 1);
        ChatUsed[role]++;
        if (topic.HasValue) ChatTopicsRead.Add(role * 100 + topic.Value);
        return true;
    }
}
