using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars
{
    public class MinchoCandyWarsSetting : ModSettings
    {
        public bool isDevMode = false;

        public void Reset()
        {
            isDevMode = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isDevMode, "isDevMode", false);
        }
    }

    public class MinchoCandyWarsMod : Mod
    {
        public MinchoCandyWarsSetting setting;
        private Vector2 scrollPosition = Vector2.zero;
        private float rightViewHeight = 1000f;
        public static MinchoCandyWarsMod Instance { get; private set; } = null!;
        public MinchoCandyWarsMod(ModContentPack content) : base(content)
        {
            setting = GetSettings<MinchoCandyWarsSetting>();
            Instance = this;
        }

        public override string SettingsCategory()
        {
            return "MinchoCandyWarsName".Translate();
        }


        //左侧区域
        private void DrawLeftPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(5f);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(innerRect);


            //开发者模式开关
            if (Prefs.DevMode)
            {
                listing.CheckboxLabeled("DEV Mode", ref setting.isDevMode);
                listing.Gap(12f);
            }

            //重置设置按钮
            if (listing.ButtonText("ResetSettings".Translate()))
            {
                setting.Reset();
            }
            listing.Gap(12f);



            listing.End();
        }

        //右侧滚动区域
        private void DrawRightPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(5f);
            Rect outRect = new Rect(innerRect.x, innerRect.y, innerRect.width, innerRect.height);
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, rightViewHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);





            listing.End();

            if (Event.current.type == EventType.Layout)
            {
                rightViewHeight = listing.CurHeight;
            }

            Widgets.EndScrollView();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect outerRect = inRect.ContractedBy(20f);
            Widgets.DrawMenuSection(outerRect);

            Rect innerRect = outerRect.ContractedBy(10f);

            float leftWidth = innerRect.width * 0.25f;
            float gap = 10f;
            float rightWidth = innerRect.width - leftWidth - gap;

            Rect leftRect = new Rect(innerRect.x, innerRect.y, leftWidth, innerRect.height);
            Rect rightRect = new Rect(innerRect.x + leftWidth + gap, innerRect.y, rightWidth, innerRect.height);

            DrawLeftPanel(leftRect);
            DrawRightPanel(rightRect);

            base.DoSettingsWindowContents(inRect);
        }
    }
}