using Core.AI.Brain;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
	//no shared timer
	
	[SpellHandler("SummonTitan")]
	public class CrystalTitanSpell : MasterLevelSpellHandling
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private int x, y, z;
		GameNpc summoned = null;
		EcsGameTimer m_growTimer;
		private const int C_GROWTIMER = 2000;
		
		public CrystalTitanSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if(!CheckCastLocation())
				return false;
			return base.CheckBeginCast(selectedTarget);
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
			GameSpellEffect effect = CreateSpellEffect(target, Effectiveness);
			CrystalTitanBrain controlledBrain = new CrystalTitanBrain(player);
			controlledBrain.IsMainPet = false;
			controlledBrain.WalkState = EWalkState.Stay;
			summoned = new GameNpc(template);
			summoned.SetOwnBrain(controlledBrain);
			//Suncheck:
			//	Is needed, else it can cause error (i.e. /cast-command)
			if (x == 0 || y == 0)
				CheckCastLocation();
			summoned.X = x;
			summoned.Y = y;
			summoned.Z = z;
			summoned.CurrentRegion = player.CurrentRegion;
			summoned.Heading = (ushort)((player.Heading + 2048) % 4096);
			summoned.Realm = player.Realm;
			summoned.CurrentSpeed = 0;
			summoned.Size = 10;
			summoned.Level = 100;
			summoned.Flags |= ENpcFlags.PEACE;
			summoned.AddToWorld();
			controlledBrain.AggressionState = EAggressionState.Aggressive;
			effect.Start(summoned);
			m_growTimer = new EcsGameTimer((GameObject)m_caster, new EcsGameTimer.EcsTimerCallback(TitanGrows), C_GROWTIMER);
		}
		
		// Make titan growing, and activate it on completition
		private int TitanGrows(EcsGameTimer timer)
		{
			if(summoned != null && summoned.Size != 60)
			{
				summoned.Size +=10;
				return C_GROWTIMER;
			}
			else
			{
				summoned.Flags = 0;
				m_growTimer.Stop();
				m_growTimer = null;
			}
			return 0;
		}
		
		private bool CheckCastLocation()
		{
			x = Caster.X;
			y = Caster.Y;
			z = Caster.Z;
			if (Spell.Target == ESpellTarget.AREA)
			{
				if (Caster.GroundTargetInView && Caster.GroundTarget != null)
				{
					x = Caster.GroundTarget.X;
					y = Caster.GroundTarget.Y;
					z = Caster.GroundTarget.Z;
				}
				else
				{
					if (Caster.GroundTarget == null)
					{
						MessageToCaster("You must set a groundtarget!", EChatType.CT_SpellResisted);
						return false;
					}
					else
					{
						MessageToCaster("Your area target is not in view.", EChatType.CT_SpellResisted);
						return false;
					}
				}
			}
			return true;
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
			effect.Owner.Health = 0; // to send proper remove packet
			effect.Owner.Delete();
			return 0;
		}

		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}
	}
}