#nullable disable
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

// Keep undecided contacts accessible from the letter stack after closing a window.
public class ContactLetter : ChoiceLetter
{
    public string EventName;
    public int OfferNumber;
    bool Pending => EventName != "MCW_Event_QS1_AllianceContact" ? Story.Current.CanAccept && EventName == Story.Current.OfferEvent && OfferNumber == Story.Current.OfferNumber
        : !StellarisEventFlagManager.HasFlag("MCW_QS1_AllianceDecided");
    public override bool CanShowInLetterStack => base.CanShowInLetterStack && Pending;
    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (ArchivedOnly || !Pending) { yield return Option_Close; yield break; }
            yield return new DiaOption("MinchoCandyWars.Contact.Open".Translate().RawText)
            {
                resolveTree = true,
                action = () => { if (Pending) StellarisEventManager.FireEvent(EventName); }
            };
            yield return Option_Postpone;
        }
    }
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref EventName, "eventName");
        Scribe_Values.Look(ref OfferNumber, "allianceOfferNumber");
    }
    public static void Send(string eventName)
    {
        var def = MCW_DefOf.MCWQ097_ContactLetter;
        string issuer = eventName == Story.Current.OfferEvent && Story.Current.AvailableContact() >= 0 ? Story.DisplayName(Story.Current.AvailableContact()) : Story.DisplayName(0);
        var letter = (ContactLetter)LetterMaker.MakeLetter("MinchoCandyWars.Contact.Title".Translate(issuer).RawText, "MinchoCandyWars.Contact.Body".Translate(issuer).RawText, def);
        letter.EventName = eventName;
        letter.OfferNumber = Story.Current.OfferNumber;
        Find.LetterStack.ReceiveLetter(letter);
    }
}
