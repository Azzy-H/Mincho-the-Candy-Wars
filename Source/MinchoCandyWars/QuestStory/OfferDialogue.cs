#nullable disable
using Verse;
namespace MinchoCandyWars.QuestStory;

public static class OfferDialogue
{
    public static string Request(int kind, int role, bool repeated, bool victory)
    {
        string name = CharacterData.DisplayName(role);
        string introduction = role == 0 ? kind switch
        {
            1 => "你好，我是沁心糖。先前的接应还没有结束，有几位同伴需要一个能放心歇脚的地方。你们愿意再腾出一些床位吗？她们也愿意帮忙工作。",
            2 => "琥珀糖核实了一个关押点。里面还有一位智慧珉巧，我们需要有人把她平安带出来。先救人，别为追赶溃兵耽误撤离。",
            3 => "琉璃糖发来了回收前哨的情报。这次我们想一起把他们的手往回推一点；援军会按计划接应，请先看清守军与路程。",
            4 => "千机糖找到一处可能留有β型纳米塑形液的遗迹。我们想继续了解自己的身体，请你们带回样本。这项研究无法让失去的同伴复活。",
            5 => "琥珀糖正在守一处遭袭的同盟据点。那里还有暂住的伙伴，请确认你们能否在2天内赶到。我们会继续保持联络。",
            6 => "准备已经完成。这次请与我们一起突入公司通讯总部，摧毁联络电脑。外围有持续增援，目标完成后及时撤离，我们会从四周接应。",
            7 => "安置工作还需要一批巧克力棒。我替大家来问问，你们能不能帮忙凑齐？这次是物资交换，不用出征。",
            8 or 10 => "玲珑糖想为刚安置下来的同伴办一场口味会。这次我替她来联系：能不能准备一批美味曲奇饼，让大家坐下来边尝边聊？",
            12 => "一架珉巧冰淇淋桶飞行器受损，正在寻找迫降地点。公司追兵会赶来，请保护所有乘员，等接应飞行器到来后护送她们登机。",
            13 => "这边有桶来路不明的冰淇淋。包装倒挺漂亮，就是查不到谁做的。你们愿意接过去保管吗？我把运单也一起送来，能查出什么就告诉我们。",
            9 => "琉璃糖发现一处失去总部联络的残留营地。守军有限，没有持续增援。你们愿意协助清除这处威胁吗？",
            _ => "千机糖的装配工作缺一批零部件。我替她来协调物资；请留足自己要用的，再决定是否接下这份委托。"
        } : kind switch
        {
            1 => "有几位同伴需要暂住和照料。我想请你们腾出些床位。她们愿意帮忙，但刚到的时候，先让她们放下行李、好好吃顿饭，好吗？",
            2 => "一位智慧珉巧被关在公司的据点里，坐标已经核实。请先确认她的状况，带她安全离开；别只顾着追溃兵。",
            3 => "新的回收前哨，守军比关押点多。我把守军和接应安排一起发来。先看好退路，等自己人跟上，再一起往里走。",
            4 => "发现一处可能留有β型纳米塑形液的遗迹。带回样本，我来做后续研究。先说清楚：这是研究，不是复活。我们能做的是把结果用在还活着的同伴身上。",
            5 => "紧急求援！一处同盟据点正在被公司袭击。援助期限只有2天，请先看清路程，再决定能不能赶到。我们会继续守。",
            8 or 10 => "新安置下来的伙伴还不太熟，我想办场小小的口味会。先凑100份美味曲奇饼，大家边尝边聊。不是实验样本，也不用去遗迹——这次要认真记住的是彼此喜欢什么。",
            12 => "我们的冰淇淋桶飞行器受损了，乘员需要在你们那里迫降。请挡住追兵，等同盟接应飞行器抵达，再护送所有乘员登机。",
            13 => "有一桶薄荷冰淇淋查不到来历，我们想找人接收保管。运单和桶会一起送过去。愿意帮这个忙吗？",
            9 => "找到一处残留营地。总部断了联络，这支小队还没有散。守军有限，我把位置发来。愿意一起清掉吗？",
            _ => "工作间的装配计划已经画好，缺的是24个零部件。我想请你们帮忙凑齐；装好后先测试，再交给需要的同伴。别把自己维修设备的存量全送过来。"
        };
        int requestKind = kind == 10 ? 8 : kind;
        if (role != 0 && requestKind is 1 or 8 or 11
            && $"MinchoCandyWars.Quest.Request.Issuer.{requestKind}".TryTranslate(out var issuerRequest))
            introduction = issuerRequest.RawText;
        else if ($"MinchoCandyWars.Quest.Request.{requestKind}".TryTranslate(out var translatedRequest))
            introduction = translatedRequest.RawText;

        if (kind == 1)
            introduction += " " + (Story.Current.IsFirstHospitality ? "MinchoCandyWars.Quest.FirstStay" : "MinchoCandyWars.Quest.LaterStay").Translate();

        string text = $"{name}: {introduction}";
        if (repeated) text = "MinchoCandyWars.Quest.Repeat".Translate(name).RawText + "\n\n" + text;
        if (victory) text += "\n\n" + "MinchoCandyWars.Quest.Routine".Translate();

        string goal = ("MinchoCandyWars.Quest.Goal." + kind).Translate().RawText;
        string details = (kind is 7 or 8 or 10 or 11 ? "MinchoCandyWars.Quest.Delivery" : "MinchoCandyWars.Quest.Details").Translate();
        return text + "\n\n" + "MinchoCandyWars.Quest.Objective".Translate(goal) + "\n" + details;
    }
    static string Voice(int role, string category, string[] lines)
    {
        string key = $"MinchoCandyWars.Quest.{category}.{role}";
        string line = key.TryTranslate(out var translated) ? translated.RawText : lines[role];
        return $"{CharacterData.DisplayName(role)}：{line}";
    }
    public static string Rejected(int kind, int role) => Voice(role, "Rejected", [
        "行，那这次我再找找别人。你们先顾好家里，有空再聊。",
        "知道了。别勉强，我会另找接应。",
        "好呀，先忙你们的。需要帮忙也记得告诉我。",
        "收到。我调整安排。",
        "好。这次我另外找人。",
        "诶，真抽不开身呀？好吧，下次可得留点空给我。"]);
    public static string Outcome(int kind, bool success, int role)
    {
        if (!success) return Voice(role, "Failure", [
            "情况我收到了。先清点人数，把伤员照顾好。之后的事，我来协调。",
            "人都撤回来了吗？先告诉我谁还需要接应。",
            "先让我看看伤员，好吗？委托的事等包扎完再谈。",
            "收到。把现存记录留下，回头一起查问题。",
            "先撤回来。我去接应，路上保持联络。",
            "先别一个人闷着。人回来了，我们一起想下一步。"]);
        int detailKind = kind == 8 ? 10 : kind;
        string text = detailKind switch
        {
            10 => "曲奇饼已经收到了。玲珑糖会把新来的同伴也叫来，大家慢慢尝，不用急着说哪种最好。谢谢你们。",
            12 => "接应飞行器已带着乘员离开，谢谢你们让她们平安登机。我们会空投补给，先照顾好刚才帮忙的人吧。",
            13 => "运单已经核对好了，桶就先放在你们那里。有新发现再联络。",
            11 => "零部件已经核对好了，接下来由千机糖装配和测试。谢谢你们，工作间这次不用停着等材料了。",
            _ => null
        };
        if (text != null && $"MinchoCandyWars.Quest.Detail.{detailKind}".TryTranslate(out var translatedDetail))
            text = translatedDetail.RawText;
        return Voice(role, "Success", [
            "收到，事情办妥了！补给这就送过去。等忙完了，记得来坐坐。",
            "确认了。补给已经安排好。……你们也辛苦了，先休息。",
            "收到了，谢谢你们呀。补给随后送到，忙了这么久，先吃点东西吧。",
            "已核对。补给发出后我把运单给你。",
            "办成了！补给在路上。下次碰面，再陪我练一场？",
            "这次干得漂亮嘛！补给给你们留好了，可别说我只会催人。"]) + (text != null ? "\n\n" + text : "");
    }
}


