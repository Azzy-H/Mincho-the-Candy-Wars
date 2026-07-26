using System;
using Verse;
using HarmonyLib;
using RimWorld;

namespace MinchoCandyWars.Patch
{
    [StaticConstructorOnStartup]
    public static class MinchoCandyWarsPatch
    {
        public static Harmony harmony;
        static MinchoCandyWarsPatch()
        {
            harmony = new Harmony("MinchoCandyWarsPatch");
            harmony.PatchAll();
            //顺序问题，需要重新处理一遍交叉引用
            StatDef.SetImmutability();
        }
    }
}
