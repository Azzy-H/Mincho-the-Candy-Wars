#nullable disable
using RimWorld;
using Verse;
namespace MinchoCandyWars.QuestStory;
public sealed partial class Story
{
    List<int> Affinities = new();
    void AffinityExpose()
    {
        Scribe_Collections.Look(ref Affinities, "characterAffinity144", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) Affinities ??= new();
    }
    public int Affinity(int role)
    {
        while (Affinities.Count < Names.Length) Affinities.Add(0);
        return Affinities[role];
    }
    public void AddAffinity(int role, int amount)
    {
        _ = Affinity(role);
        Affinities[role] = Math.Clamp(Affinities[role] + amount, 0, 100);
    }
    public bool CanInviteCharacter(int role) => Victory && Affinity(role) >= 100;
}
