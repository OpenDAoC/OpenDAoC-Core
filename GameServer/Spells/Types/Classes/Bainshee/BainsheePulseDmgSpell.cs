using System;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
    [SpellHandler("BainsheePulseDmg")]
	public class BainsheePulseDmgSpell : SpellHandler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public const string FOCUS_WEAK = "FocusSpellHandler.Online";
		/// <summary>
		/// Execute direct damage spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
            if (Spell.Pulse != 0)
            {
                GameEventMgr.AddHandler(Caster, GamePlayerEvent.Moving, new CoreEventHandler(EventAction));
                GameEventMgr.AddHandler(Caster, GamePlayerEvent.Dying, new CoreEventHandler(EventAction));
            }
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}
        public override bool CancelPulsingSpell(GameLiving living, ESpellType spellType)
        {
            lock (living.effectListComponent.ConcentrationEffectsLock)
            {
                for (int i = 0; i < living.effectListComponent.ConcentrationEffects.Count; i++)
                {
					PulsingSpellEffect effect = null; //living.ConcentrationEffects[i] as PulsingSpellEffect;
                    if (effect == null)
                        continue;
                    if (effect.SpellHandler.Spell.SpellType == spellType)
                    {
                        effect.Cancel(false);
                        GameEventMgr.RemoveHandler(Caster, GamePlayerEvent.Moving, new CoreEventHandler(EventAction));
                        GameEventMgr.RemoveHandler(Caster, GamePlayerEvent.Dying, new CoreEventHandler(EventAction));
                        return true;
                    }
                }
            }
            return false;
        }
        public void EventAction(CoreEvent e, object sender, EventArgs args)
        {
            GameLiving player = sender as GameLiving;

            if (player == null) return;
            if (Spell.Pulse != 0 && CancelPulsingSpell(Caster, Spell.SpellType))
            {
                MessageToCaster("You cancel your effect.", EChatType.CT_Spell);
                return;
            }
        }

		#region LOS on Keeps

		public override void OnDirectEffect(GameLiving target)
		{
			if (target == null)
				return;

			if (Spell.Target == ESpellTarget.CONE || (Spell.Target == ESpellTarget.ENEMY && Spell.IsPBAoE))
			{
				GamePlayer player = null;
				if (target is GamePlayer)
					player = target as GamePlayer;
				else
				{
					if (Caster is GamePlayer)
						player = Caster as GamePlayer;
					else if (Caster is GameNpc && (Caster as GameNpc).Brain is IControlledBrain)
					{
						IControlledBrain brain = (Caster as GameNpc).Brain as IControlledBrain;
						player = brain.GetPlayerOwner();
					}
				}
				if (player != null)
					player.Out.SendCheckLOS(Caster, target, new CheckLOSResponse(DealDamageCheckLOS));
				else
					DealDamage(target);
			}
			else DealDamage(target);
		}

		private void DealDamageCheckLOS(GamePlayer player, ushort response, ushort targetOID)
		{
			if (player == null || targetOID == 0)
				return;

			if ((response & 0x100) == 0x100)
			{
				try
				{
					GameLiving target = Caster.CurrentRegion.GetObject(targetOID) as GameLiving;

					if (target != null)
						DealDamage(target);
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error(string.Format("targetOID:{0} caster:{1} exception:{2}", targetOID, Caster, e));
				}
			}
		}

		private void DealDamage(GameLiving target)
		{
			if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

			// calc damage
			AttackData ad = CalculateDamageToTarget(target);
			DamageTarget(ad, true);
			SendDamageMessages(ad);
			target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
		}
		/*
		 * We need to send resist spell los check packets because spell resist is calculated first, and
		 * so you could be inside keep and resist the spell and be interupted when not in view
		 */
		protected override void OnSpellResisted(GameLiving target)
		{
			if (target is GamePlayer && Caster.TempProperties.GetProperty("player_in_keep_property", false))
			{
				GamePlayer player = target as GamePlayer;
				player.Out.SendCheckLOS(Caster, player, new CheckLOSResponse(ResistSpellCheckLOS));
			}
			else SpellResisted(target);
		}

		private void ResistSpellCheckLOS(GamePlayer player, ushort response, ushort targetOID)
		{
			if ((response & 0x100) == 0x100)
			{
				try
				{
					GameLiving target = Caster.CurrentRegion.GetObject(targetOID) as GameLiving;
					if (target != null)
						SpellResisted(target);
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error(string.Format("targetOID:{0} caster:{1} exception:{2}", targetOID, Caster, e));
				}
			}
		}

		private void SpellResisted(GameLiving target)
		{
			base.OnSpellResisted(target);
		}
		#endregion

		// constructor
        public BainsheePulseDmgSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
