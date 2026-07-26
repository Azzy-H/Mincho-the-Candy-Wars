using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using MinchoCandyWars.Interface;
using RimWorld;

namespace MinchoCandyWars.Patch.Ability
{
    [HarmonyPatch(typeof(RimWorld.Ability))]
    internal class Patch_Ability
    {
        //插入初始化接口调用
        [HarmonyPostfix]
        [HarmonyPatch(nameof(RimWorld.Ability.Initialize))]
        public static void Postfix_Initialize(RimWorld.Ability __instance)
        {
            if (__instance is IInitalizable initalizable)
            {
                initalizable.Initialize();
            }
        }
    }
}
