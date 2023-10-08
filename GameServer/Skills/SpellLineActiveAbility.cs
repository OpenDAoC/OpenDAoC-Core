using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// Active Spell Line Ability Handler.
	/// Trigger Spell Casting in Described Spell Line using spell available at given Ability Level.
	/// </summary>
	public class SpellLineActiveAbility : SpellLineAbstractAbility
	{
		/// <summary>
		/// Execute Handler
		/// Cast the According Spell
		/// </summary>
		/// <param name="living">Living Executing Ability</param>
		public override void Execute(GameLiving living)
		{
			base.Execute(living);
			
			if (Spell != null && SpellLine != null)
				living.CastSpell(this);
		}
		
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// <param name="dba"></param>
		/// <param name="level"></param>
		public SpellLineActiveAbility(DbAbility dba, int level)
			: base(dba, level)
		{
		}
	}
}
