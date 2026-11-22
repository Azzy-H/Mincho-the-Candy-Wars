#nullable disable
using HarmonyLib;
using MinchoCandyWars.StellarisEvent;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.QuestStory;

// Native opaque fill and border; portrait, text, scrolling and options stay intact.
[HarmonyPatch(typeof(StellarisEventWindow), "DrawBackground")]
static class VanillaBackground
{
    static bool Prefix(Rect rect)
    {
        Widgets.DrawWindowBackground(rect);
        return false;
    }
}

