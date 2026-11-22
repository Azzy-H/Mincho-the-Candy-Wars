#nullable disable
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

// 聊天页面只展示对话，不发送剧情信件，也不反复触发任务事件。
static class ConversationPages
{
    static StellarisEventWindow Window => Find.WindowStack.Windows.OfType<StellarisEventWindow>().Last();
    static void Page(int role, string text, params (string, Action)[] choices) =>
        Window.ShowConversation(Story.DisplayName(role) + "MinchoCandyWars.Invite.Affinity".Translate(Story.Current.Affinity(role)).RawText, text, Story.Portraits[role], choices);
    public static void Hub()
    {
        var choices = Story.Names.Select((name, role) => (name, role)).Where(x => Story.Current.ChatReason(x.role) == null)
            .Select(x => (Story.DisplayName(x.role) + $"（{Story.Current.Affinity(x.role)}/100）", (Action)(() => Topics(x.role)))).ToList();
        choices.Add(("MinchoCandyWars.Chat.ReturnAlliance".Translate().RawText, () => Window.ReturnToEvent()));
        choices.Add(("MinchoCandyWars.Chat.EndCall".Translate().RawText, () => Window.Close()));
        Page(0, "MinchoCandyWars.Chat.Hub".Translate().RawText + "\n\n" + string.Join("\n", Story.Names.Select((name, role) => Story.DisplayName(role) + ": " + (Story.Current.ChatReason(role) ?? "MinchoCandyWars.Chat.Available".Translate(Story.Current.ChatRemaining(role)).RawText))), choices.ToArray());
    }
    public static void Topics(int role)
    {
        if (Story.Current.ChatReason(role) is { } reason)
        { Page(role, reason, ("MinchoCandyWars.Chat.Back".Translate().RawText, Hub)); return; }
        var choices = new List<(string, Action)>();
        foreach (int id in CharacterChats.Today(role, Find.TickManager.TicksGame / 60000))
        {
            if (Story.Current.HasReadTopic(role, id)) continue;
            int topic = id;
            string label = role == 3 && topic == 5
                ? "MinchoCandyWars.Chat.Sketch.Topic".Translate().RawText
                : TopicText(role, topic, "Label", CharacterChats.Topics[role][topic].Label);
            choices.Add((label, () => ReadTopic(role, topic)));
        }
        choices.Add(("MinchoCandyWars.Chat.Switch".Translate().RawText, Hub));
        choices.Add(("MinchoCandyWars.Chat.EndCall".Translate().RawText, () => Window.Close()));
        string greeting = $"MinchoCandyWars.Chat.Greeting.{role}".TryTranslate(out var translated)
            ? $"{Story.DisplayName(role)}: {translated.RawText}"
            : CharacterChats.Greetings[role];
        Page(role, greeting, choices.ToArray());
    }
    static string TopicText(int role, int topic, string field, string fallback) =>
        $"MinchoCandyWars.Chat.Topic.{role}.{topic}.{field}".TryTranslate(out var translated)
            ? translated.RawText
            : fallback;

    static void ReadTopic(int role, int id)
    {
        if (!Story.Current.TryChat(role, id)) { Topics(role); return; }
        if (role == 3 && id == 5) { Sketch(); return; }
        var topic = CharacterChats.Topics[role][id];
        Page(role, TopicText(role, id, "Text", topic.Text), (TopicText(role, id, "Question", topic.Question), () => Page(role, TopicText(role, id, "Reply", topic.Reply),
            ("MinchoCandyWars.Chat.Another".Translate().RawText, () => Topics(role)), ("MinchoCandyWars.Chat.Switch".Translate().RawText, Hub))),
            ("MinchoCandyWars.Chat.Later".Translate().RawText, Hub));
    }
    public static void Reply(int role,int index)
    {
        if (!Story.Current.TryChat(role)) { Topics(role); return; }
        if(role==3 && index==1){Sketch();return;}
        string key = $"MinchoCandyWars.Chat.Legacy.{role}.{index}.Reply";
        string reply = key.TryTranslate(out var translated)
            ? translated.RawText
            : DefDatabase<StellarisEventDef>.GetNamed($"MCWQ101_Chat{role}").options[index].desc + ChatLines.More[role][Math.Min(index, 1)];
        Page(role, reply,
            ("MinchoCandyWars.Chat.Continue".Translate().RawText, () => Topics(role)), ("MinchoCandyWars.Chat.Switch".Translate().RawText, Hub));
    }
    static void Daily(int role)
    {
        if (!Story.Current.TryChat(role)) { Topics(role); return; }
        Page(role, ChatLines.Daily[role], ("这件事后来怎么样了？", () =>
            Page(role, ChatLines.After[role], ("MinchoCandyWars.Chat.Another".Translate().RawText, () => Topics(role)), ("MinchoCandyWars.Chat.Switch".Translate().RawText, Hub))),
            ("谢谢你跟我说这些。", () => Topics(role)));
    }
    static void Sketch()
    {
        const string prefix = "MinchoCandyWars.Chat.Sketch.";
        var story = Story.Current;

        void Name(string savedName, string replyKey)
        {
            story.SketchName = savedName;
            Page(3, (prefix + replyKey).Translate().RawText,
                ("MinchoCandyWars.Chat.Continue".Translate().RawText, () => Topics(3)));
        }

        string savedName = story.SketchName;
        string displayName = savedName switch
        {
            "小推车" => (prefix + "CartName").Translate().RawText,
            "慢一点" => (prefix + "SlowName").Translate().RawText,
            _ => savedName
        };
        string text = savedName == null
            ? (prefix + "Intro").Translate().RawText
            : (prefix + "Recall").Translate(displayName).RawText;
        Page(3, $"{Story.DisplayName(3)}: {text}",
            ((prefix + "Cart").Translate().RawText, () => Name("小推车", "CartReply")),
            ((prefix + "Slow").Translate().RawText, () => Name("慢一点", "SlowReply")),
            ((prefix + "Later").Translate().RawText, () => Topics(3)));
    }
}
public class DeliveryAvailable : StellarisEventTrigger
{
    public override bool CanTrigger() => Find.QuestManager.QuestsListForReading.Any(q =>
        q.id == Story.Current.State.ActiveQuest && q.State == QuestState.Ongoing &&
        q.PartsListForReading.OfType<DeliveryPart>().Any(p => Find.TickManager.TicksGame < p.Deadline));
}

public class ContactAvailable : StellarisEventTrigger
{
    public string operation;
    public override bool CanTrigger() => operation switch
    {
        "invite" => Enumerable.Range(0, Story.Names.Length).Any(Invitations.CanInvite),
        "gift" => StellarisEventFlagManager.HasFlag("MCW_QS1_AllianceJoined") && Relations.Number < 100,
        _ => false
    };
}
