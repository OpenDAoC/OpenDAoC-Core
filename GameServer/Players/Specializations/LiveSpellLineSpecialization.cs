using System.Collections.Generic;
using Core.GS.Styles;

namespace Core.GS
{
	/// <summary>
	/// This is a Live Spell Line Specialization, used for list caster, with baseline spell and specline spell appart
	/// Purely rely on base Specialization implementation only disabling weapon style (not applicable here)
	/// </summary>
	public class LiveSpellLineSpecialization : Specialization
	{
		public LiveSpellLineSpecialization(string keyname, string displayname, ushort icon, int ID)
			: base(keyname, displayname, icon, ID)
		{
		}
		
		/// <summary>
		/// No Styles for Spell Specs.
		/// </summary>
		/// <param name="living"></param>
		/// <returns></returns>
		protected override List<Style> GetStylesForLiving(GameLiving living, int level)
		{
			return new List<Style>();
		}

	}
}