using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
	/// <summary>
	/// LiveRealmAbilitiesSpecialization is targeted at Realm Ability Granting through RP's awarding.
	/// This only reads the Realm Abilities Player Field, it must be set elseway...
	/// </summary>
	public class LiveRealmAbilitiesSpecialization : CareerSpecialization
	{
		public LiveRealmAbilitiesSpecialization(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
		
		/// <summary>
		/// Realm Abilities Specialization Only Return Player's Attached Realm Abilities...
		/// </summary>
		/// <param name="living"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		protected override List<Ability> GetAbilitiesForLiving(GameLiving living, int level)
		{
			if (living is GamePlayer)
			{
				GamePlayer player = (GamePlayer)living;
				var list = player.GetRealmAbilities().Cast<Ability>().ToList();
				
				// Add RR5 if player is over RL5
				if (player.RealmLevel >= 40)
				{
					Ability ab = SkillBase.GetClassRR5Ability(((GamePlayer)living).PlayerClass.ID);
				
					if (ab != null)
						list.Add((RealmAbilities.RealmAbility)ab);
				}
				
				return list;
			}
			
			return new List<Ability>();
		}
		
		/// <summary>
		/// No Spell Lines
		/// </summary>
		/// <param name="living"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		protected override List<SpellLine> GetSpellLinesForLiving(GameLiving living, int level)
		{
			return new List<SpellLine>();
		}
		
		/// <summary>
		/// No Spells
		/// </summary>
		/// <param name="living"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		protected override IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(GameLiving living, int level)
		{
			return new Dictionary<SpellLine, List<Skill>>();
		}
		
		/// <summary>
		/// No Styles
		/// </summary>
		/// <param name="living"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		protected override List<DOL.GS.Styles.Style> GetStylesForLiving(GameLiving living, int level)
		{
			return new List<DOL.GS.Styles.Style>();
		}
	}
}