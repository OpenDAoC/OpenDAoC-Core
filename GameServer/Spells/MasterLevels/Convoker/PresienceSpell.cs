using Core.Database;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.Spells
{
	//no shared timer

	[SpellHandler("PrescienceNode")]
	public class PrescienceNodeSpell : FontSpellHandler
	{
		// constructor
		public PrescienceNodeSpell(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{
			ApplyOnNPC = false;
			ApplyOnCombat = true;

			//Construct a new font.
			font = new GameFont();
			font.Model = 2584;
			font.Name = spell.Name;
			font.Realm = caster.Realm;
			font.X = caster.X;
			font.Y = caster.Y;
			font.Z = caster.Z;
			font.CurrentRegionID = caster.CurrentRegionID;
			font.Heading = caster.Heading;
			font.Owner = (GamePlayer)caster;

			// Construct the font spell
			dbs = new DbSpell();
			dbs.Name = spell.Name;
			dbs.Icon = 7312;
			dbs.ClientEffect = 7312;
			dbs.Damage = spell.Damage;
			dbs.DamageType = (int)spell.DamageType;
			dbs.Target = "Enemy";
			dbs.Radius = 0;
			dbs.Type = ESpellType.Prescience.ToString();
			dbs.Value = spell.Value;
			dbs.Duration = spell.ResurrectHealth;
			dbs.Frequency = spell.ResurrectMana;
			dbs.Pulse = 0;
			dbs.PulsePower = 0;
			dbs.LifeDrainReturn = spell.LifeDrainReturn;
			dbs.Power = 0;
			dbs.CastTime = 0;
			dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
			sRadius = 2000;
			s = new Spell(dbs, 50);
			sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
			heal = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
		}
	}

	[SpellHandler("Prescience")]
	public class PrescienceSpell : SpellHandler
	{
		public override bool IsOverwritable(EcsGameSpellEffect compare)
		{
			return false;
		}

		public override bool HasPositiveEffect
		{
			get { return false; }
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			return base.OnEffectExpires(effect, noMessages);
		}

		public PrescienceSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}