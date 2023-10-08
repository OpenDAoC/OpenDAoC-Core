using DOL.Database;

namespace DOL.GS.SkillHandler
{
	[SkillHandler(Abilities.ClimbSpikes)]
	public class ClimbingAbilityHandler : SpellCastingAbilityHandler
	{
		private static int spellid = -1;
		
		public override long Preconditions
		{
			get
			{
				return DEAD | SITTING | MEZZED | STUNNED;
			}
		}
		public override int SpellID
		{
			get
			{
				return spellid;
			}
		}

		public ClimbingAbilityHandler()
		{
			// Graveen: crappy, but not hardcoded. if we except by the ability name ofc...
			// problems are: 
			// 		- matching vs ability name / spell name needed
			//		- spell name is not indexed
			// perhaps a basis to think about, but definitively not the design we want.
			if (spellid == -1)
			{
				spellid=0;
				DbSpell climbSpell = CoreDb<DbSpell>.SelectObject(DB.Column("Name").IsEqualTo(Abilities.ClimbSpikes));
				if (climbSpell != null)
					spellid = climbSpell.SpellID;
			}
		}
	}
}
