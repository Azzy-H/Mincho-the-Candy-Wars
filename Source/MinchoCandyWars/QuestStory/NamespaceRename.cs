using System;
using HarmonyLib;
using Verse;

namespace MinchoCandyWars.QuestStory;

// Scribe stores full type names. Read the old prefix; newly saved classes use this assembly's namespace.
[HarmonyPatch(typeof(GenTypes), nameof(GenTypes.GetTypeInAnyAssembly))]
static class NamespaceRename
{
    static void Prefix(ref string typeName)
    {
        const string oldPrefix = "MCW.QuestStory.";
        if (typeName?.StartsWith(oldPrefix, StringComparison.Ordinal) == true)
            typeName = "MinchoCandyWars.QuestStory." + typeName[oldPrefix.Length..];
    }
}
