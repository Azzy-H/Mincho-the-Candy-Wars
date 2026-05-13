using MinchoCandyWars.Abilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MinchoCandyWars.Gizmos
{
    public class Command_MinchoAbility : Command_Ability
    {
        protected MinchoCandyAbility minchoCandyAbility;

        public Command_MinchoAbility(MinchoCandyAbility ability, Pawn pawn) : base(ability, pawn)
        {
            this.minchoCandyAbility = ability;
        }
        
        public override string TopRightLabel => minchoCandyAbility.MinchoCandyValueConsumeText();
    }
}
