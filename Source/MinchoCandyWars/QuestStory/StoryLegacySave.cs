#nullable disable
using System.Xml;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

public sealed partial class Story
{
    LegacySave legacySave;

    void ReadLegacySave()
    {
        if (Scribe.mode == LoadSaveMode.LoadingVars &&
            LegacySave.HasData(Scribe.loader.curXmlParent))
        {
            legacySave = new LegacySave();
        }
        legacySave?.Read();
    }

    void RestoreLegacyFlags()
    {
        if (legacySave == null) return;
        legacySave.Restore(this);
        legacySave = null;
    }

    // Read the pre-flag format during load only. The next save writes the
    // existing event flags and leaves the retired fields out.
    sealed class LegacySave
    {
        Pawn First;
        Quest Series;
        int ArrivalAt;
        int DepartAt;
        bool HasAcceptedHospitality;
        bool Started;
        bool ContactReady;
        bool A1Viewed;
        bool CompletedBatch;
        bool FirstTemporary;
        string Result;
        int LastBucketCargo;
        int OfferIssuer;
        int OfferNumber;
        int ActiveIssuer;
        bool AssignedOffer;
        int Offer;
        int ActiveKind;
        int Rejected;
        int Viewed;
        int Succeeded;
        int FinalAt;
        bool Victory;
        bool FinalAttempted;
        bool Migrated101;
        bool KitchenViewed;
        int CompanyDeadline;
        int Chatted;
        bool Appearance103;
        string SketchName;
        int ChatDay;

        public static bool HasData(XmlNode node) =>
            node != null && (
                node["firstMincho"] != null ||
                node["allianceSeries100"] != null ||
                node["arrivalAt"] != null ||
                node["departAt"] != null ||
                node["hasAcceptedHospitality"] != null ||
                node["started"] != null ||
                node["contactReady"] != null ||
                node["a1Viewed"] != null ||
                node["completedBatch"] != null ||
                node["firstTemporary139"] != null ||
                node["lastResult"] != null ||
                node["lastBucketCargo105"] != null ||
                node["offerIssuer103"] != null ||
                node["allianceOfferNumber"] != null ||
                node["activeIssuer103"] != null ||
                node["assignedOffer103"] != null ||
                node["offer101"] != null ||
                node["activeKind101"] != null ||
                node["rejected101"] != null ||
                node["viewed101"] != null ||
                node["succeeded101"] != null ||
                node["finalAt101"] != null ||
                node["victory101"] != null ||
                node["finalAttempted101"] != null ||
                node["migrated101"] != null ||
                node["kitchenViewed101"] != null ||
                node["companyDeadline102"] != null ||
                node["chatted101"] != null ||
                node["appearance103"] != null ||
                node["sketchName101"] != null ||
                node["chatDay140"] != null);

        public void Read()
        {
            if (Scribe.mode is LoadSaveMode.LoadingVars or LoadSaveMode.ResolvingCrossRefs)
            {
                Scribe_References.Look(ref First, "firstMincho");
                Scribe_References.Look(ref Series, "allianceSeries100");
            }
            if (Scribe.mode != LoadSaveMode.LoadingVars) return;

            Scribe_Values.Look(ref ArrivalAt, "arrivalAt", -1);
            Scribe_Values.Look(ref DepartAt, "departAt", -1);
            Scribe_Values.Look(ref HasAcceptedHospitality, "hasAcceptedHospitality");
            Scribe_Values.Look(ref Started, "started");
            Scribe_Values.Look(ref ContactReady, "contactReady");
            Scribe_Values.Look(ref A1Viewed, "a1Viewed");
            Scribe_Values.Look(ref CompletedBatch, "completedBatch");
            Scribe_Values.Look(ref FirstTemporary, "firstTemporary139");
            Scribe_Values.Look(ref Result, "lastResult", "尚未开始");
            Scribe_Values.Look(ref LastBucketCargo, "lastBucketCargo105", -1);
            Scribe_Values.Look(ref OfferIssuer, "offerIssuer103", -1);
            Scribe_Values.Look(ref OfferNumber, "allianceOfferNumber");
            Scribe_Values.Look(ref ActiveIssuer, "activeIssuer103");
            Scribe_Values.Look(ref AssignedOffer, "assignedOffer103");
            Scribe_Values.Look(ref Offer, "offer101", 1);
            Scribe_Values.Look(ref ActiveKind, "activeKind101", 1);
            Scribe_Values.Look(ref Rejected, "rejected101");
            Scribe_Values.Look(ref Viewed, "viewed101");
            Scribe_Values.Look(ref Succeeded, "succeeded101");
            Scribe_Values.Look(ref FinalAt, "finalAt101", -1);
            Scribe_Values.Look(ref Victory, "victory101");
            Scribe_Values.Look(ref FinalAttempted, "finalAttempted101");
            Scribe_Values.Look(ref Migrated101, "migrated101");
            Scribe_Values.Look(ref KitchenViewed, "kitchenViewed101");
            Scribe_Values.Look(ref CompanyDeadline, "companyDeadline102", -1);
            Scribe_Values.Look(ref Chatted, "chatted101");
            Scribe_Values.Look(ref Appearance103, "appearance103");
            Scribe_Values.Look(ref SketchName, "sketchName101");
            Scribe_Values.Look(ref ChatDay, "chatDay140", -1);
        }

        public void Restore(Story story)
        {
            // A current-format flag takes precedence if a save contains both formats.
            if (Missing("First")) story.First = First;
            if (Missing("Series")) story.Series = Series;
            if (Missing("ArrivalAt")) story.ArrivalAt = ArrivalAt;
            if (Missing("DepartAt")) story.DepartAt = DepartAt;
            if (Missing("HasAcceptedHospitality")) story.HasAcceptedHospitality = HasAcceptedHospitality;
            if (Missing("Started")) story.Started = Started;
            if (Missing("ContactReady")) story.ContactReady = ContactReady;
            if (Missing("A1Viewed")) story.A1Viewed = A1Viewed;
            if (Missing("CompletedBatch")) story.CompletedBatch = CompletedBatch;
            if (Missing("FirstTemporary")) story.FirstTemporary = FirstTemporary;
            if (Missing("Result")) story.Result = Result;
            if (Missing("LastBucketCargo")) story.LastBucketCargo = LastBucketCargo;
            if (Missing("OfferIssuer")) story.OfferIssuer = OfferIssuer;
            if (Missing("OfferNumber")) story.OfferNumber = OfferNumber;
            if (Missing("ActiveIssuer")) story.ActiveIssuer = ActiveIssuer;
            if (Missing("AssignedOffer")) story.AssignedOffer = AssignedOffer;
            if (Missing("Offer")) story.Offer = Offer;
            if (Missing("ActiveKind")) story.ActiveKind = ActiveKind;
            if (Missing("Rejected")) story.Rejected = Rejected;
            if (Missing("Viewed")) story.Viewed = Viewed;
            if (Missing("Succeeded")) story.Succeeded = Succeeded;
            if (Missing("FinalAt")) story.FinalAt = FinalAt;
            if (Missing("Victory")) story.Victory = Victory;
            if (Missing("FinalAttempted")) story.FinalAttempted = FinalAttempted;
            if (Missing("Migrated101")) story.Migrated101 = Migrated101;
            if (Missing("KitchenViewed")) story.KitchenViewed = KitchenViewed;
            if (Missing("CompanyDeadline")) story.CompanyDeadline = CompanyDeadline;
            if (Missing("Chatted")) story.Chatted = Chatted;
            if (Missing("Appearance103")) story.Appearance103 = Appearance103;
            if (Missing("SketchName")) story.SketchName = SketchName;
            if (Missing("ChatDay")) story.ChatDay = ChatDay;
        }

        static bool Missing(string flag) =>
            StellarisEventFlagManager.GetFlag("MCW_Story_" + flag) == null;
    }
}
