using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	[SpellHandler(eSpellType.DoomHammer)]
	public class DoomHammerSpellHandler : DirectDamageSpellHandler
	{
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if(Caster.IsDisarmed)
			{
				MessageToCaster("You are disarmed and can't use this spell!",eChatType.CT_SpellResisted);
				return false;
			}
			return base.CheckBeginCast(selectedTarget);
		}
		public override double CalculateDamageBase(GameLiving target) { return Spell.Damage; }
		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(Caster);
			Caster.attackComponent.StopAttack();
			Caster.DisarmedTime = Caster.CurrentRegion.Time + Spell.Duration;

			foreach (GamePlayer visPlayer in Caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				visPlayer.Out.SendCombatAnimation(Caster, target, 0x0000, 0x0000, 408, 0, 0x00, target.HealthPercent);

			if (Spell.ResurrectMana > 0)
			{
				foreach (GamePlayer visPlayer in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					visPlayer.Out.SendSpellEffectAnimation(Caster, target, (ushort) Spell.ResurrectMana, 0, false, 0x01);
			}

			if ((Spell.Duration > 0 && Spell.Target != eSpellTarget.AREA) || Spell.Concentration>0)
				OnDirectEffect(target);
		}
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			//Caster.IsDisarmed=false;
			return base.OnEffectExpires(effect,noMessages);
		}
		public DoomHammerSpellHandler(GameLiving caster,Spell spell,SpellLine line) : base(caster,spell,line) {}
	}
}
