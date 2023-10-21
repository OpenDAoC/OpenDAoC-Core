using System;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
	//no shared timer

	[SpellHandler("BrittleGuard")]
	public class BrittleGuardSpell : MasterLevelSpellHandling
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		GameNpc summoned = null;
		GameSpellEffect beffect = null;

		public BrittleGuardSpell(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{

		}

		/// <summary>
		/// called after normal spell cast is completed and effect has to be started
		/// </summary>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			GamePlayer player = Caster as GamePlayer;
			if (player == null)
			{
				return;
			}

			INpcTemplate template = NpcTemplateMgr.GetTemplate(Spell.LifeDrainReturn);
			if (template == null)
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("NPC template {0} not found! Spell: {1}", Spell.LifeDrainReturn, Spell.ToString());
				MessageToCaster("NPC template " + Spell.LifeDrainReturn + " not found!", EChatType.CT_System);
				return;
			}

			Point2D summonloc;
			beffect = CreateSpellEffect(target, Effectiveness);
			{
				summonloc = target.GetPointFromHeading(target.Heading, 64);

				BrittleGuardBrain controlledBrain = new BrittleGuardBrain(player);
				controlledBrain.IsMainPet = false;
				summoned = new GameNpc(template);
				summoned.SetOwnBrain(controlledBrain);
				summoned.X = summonloc.X;
				summoned.Y = summonloc.Y;
				summoned.Z = target.Z;
				summoned.CurrentRegion = target.CurrentRegion;
				summoned.Heading = (ushort)((target.Heading + 2048) % 4096);
				summoned.Realm = target.Realm;
				summoned.CurrentSpeed = 0;
				summoned.Level = 1;
				summoned.Size = 10;
				summoned.AddToWorld();
				controlledBrain.AggressionState = EAggressionState.Passive;
				GameEventMgr.AddHandler(summoned, GameLivingEvent.Dying, new CoreEventHandler(GuardDie));
				beffect.Start(Caster);
			}
		}

		private void GuardDie(CoreEvent e, object sender, EventArgs args)
		{
			GameNpc bguard = sender as GameNpc;
			if (bguard == summoned)
			{
				GameEventMgr.RemoveHandler(summoned, GameLivingEvent.Dying, new CoreEventHandler(GuardDie));
				beffect.Cancel(false);
			}
		}

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		/// <returns>immunity duration in milliseconds</returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if (summoned != null)
			{
				summoned.Health = 0; // to send proper remove packet
				summoned.Delete();
			}

			return base.OnEffectExpires(effect, noMessages);
		}
	}
}