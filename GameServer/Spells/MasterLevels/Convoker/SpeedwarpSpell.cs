using Core.Database;
using Core.GS.Effects;

namespace Core.GS.Spells
{
	//no shared timer

	[SpellHandler("SpeedWrapWard")]
	public class SpeedwarpWardSpell : FontSpellHandler
	{
		// constructor
		public SpeedwarpWardSpell(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{
			ApplyOnCombat = true;
			Friendly = false;

			//Construct a new mine.
			font = new GameFont();
			font.Model = 2586;
			font.Name = spell.Name;
			font.Realm = caster.Realm;
			font.X = caster.X;
			font.Y = caster.Y;
			font.Z = caster.Z;
			font.CurrentRegionID = caster.CurrentRegionID;
			font.Heading = caster.Heading;
			font.Owner = (GamePlayer)caster;

			// Construct the mine spell
			dbs = new DbSpell();
			dbs.Name = spell.Name;
			dbs.Icon = 7237;
			dbs.ClientEffect = 7237;
			dbs.Damage = spell.Damage;
			dbs.DamageType = (int)spell.DamageType;
			dbs.Target = "Enemy";
			dbs.Radius = 0;
			dbs.Type = ESpellType.SpeedWrap.ToString();
			dbs.Value = spell.Value;
			dbs.Duration = spell.ResurrectHealth;
			dbs.Frequency = spell.ResurrectMana;
			dbs.Pulse = 0;
			dbs.PulsePower = 0;
			dbs.LifeDrainReturn = spell.LifeDrainReturn;
			dbs.Power = 0;
			dbs.CastTime = 0;
			dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
			sRadius = 1000;
			dbs.SpellGroup = 9;
			s = new Spell(dbs, 50);
			sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
			heal = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
		}
	}

	[SpellHandler("SpeedWrap")]
	public class SpeedwarpSpell : SpellHandler
	{
		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			if (effect.Owner is GamePlayer)
				((GamePlayer)effect.Owner).Out.SendUpdateMaxSpeed();
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if (effect.Owner is GamePlayer)
				((GamePlayer)effect.Owner).Out.SendUpdateMaxSpeed();
			return base.OnEffectExpires(effect, noMessages);
		}

		public SpeedwarpSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}