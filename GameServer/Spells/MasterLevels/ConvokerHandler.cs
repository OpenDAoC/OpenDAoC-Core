using System;
using System.Collections;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using log4net;
using DOL.Database;
using DOL.GS.RealmAbilities;

namespace DOL.GS.Spells
{
	//http://www.camelotherald.com/masterlevels/ma.php?ml=Convoker
	//no shared timer
	#region Convoker-1
	[SpellHandlerAttribute("SummonWood")]
	public class SummonWoodHandler : SummonItemSpellHandler
	{
		public SummonWoodHandler(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{
			DbItemTemplates template = GameServer.Database.FindObjectByKey<DbItemTemplates>("mysticwood_wooden_boards");
			if (template != null)
			{
                items.Add(GameInventoryItem.Create(template));
                foreach (InventoryItem item in items)
                {
                    if (item.IsStackable)
                    {
                        item.Count = 1;
                        item.Weight = item.Count * item.Weight;
                    }
                }
			}
		}
	}
	#endregion

	//no shared timer
	#region Convoker-2
	[SpellHandlerAttribute("PrescienceNode")]
	public class PrescienceNodeHandler : FontSpellHandler
	{
		// constructor
		public PrescienceNodeHandler(GameLiving caster, Spell spell, SpellLine line)
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
			dbs = new DBSpell();
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
	[SpellHandlerAttribute("Prescience")]
	public class PrescienceHandler : SpellHandler
	{
		public override bool IsOverwritable(GameSpellEffect compare)
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

		public PrescienceHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
	#endregion

	//no shared timer
	#region Convoker-3
	[SpellHandlerAttribute("PowerTrap")]
	public class PowerTrapHandler : MineSpellHandler
	{
		// constructor
		public PowerTrapHandler(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{
			//Construct a new mine.
			mine = new GameMine();
			mine.Model = 2590;
			mine.Name = spell.Name;
			mine.Realm = caster.Realm;
			mine.X = caster.X;
			mine.Y = caster.Y;
			mine.Z = caster.Z;
			mine.CurrentRegionID = caster.CurrentRegionID;
			mine.Heading = caster.Heading;
			mine.Owner = (GamePlayer)caster;

			// Construct the mine spell
			dbs = new DBSpell();
			dbs.Name = spell.Name;
			dbs.Icon = 7313;
			dbs.ClientEffect = 7313;
			dbs.Damage = spell.Damage;
			dbs.DamageType = (int)spell.DamageType;
			dbs.Target = "Enemy";
			dbs.Radius = 0;
			dbs.Type = ESpellType.PowerRend.ToString();
			dbs.Value = spell.Value;
			dbs.Duration = spell.ResurrectHealth;
			dbs.Frequency = spell.ResurrectMana;
			dbs.Pulse = 0;
			dbs.PulsePower = 0;
			dbs.LifeDrainReturn = spell.LifeDrainReturn;
			dbs.Power = 0;
			dbs.CastTime = 0;
			dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
			sRadius = 350;
			s = new Spell(dbs, 1);
			sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
			trap = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
		}
	}
	#endregion

	//no shared timer
	#region Convoker-4
	[SpellHandlerAttribute("SpeedWrapWard")]
	public class SpeedWrapWardHandler : FontSpellHandler
	{
		// constructor
		public SpeedWrapWardHandler(GameLiving caster, Spell spell, SpellLine line)
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
			dbs = new DBSpell();
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
	[SpellHandlerAttribute("SpeedWrap")]
	public class SpeedWrapHandler : SpellHandler
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
		public SpeedWrapHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
	#endregion

	//shared timer 1
	#region Convoker-5
	[SpellHandlerAttribute("SummonWarcrystal")]
	public class SummonWarcrystalHandler : SummonItemSpellHandler
	{
		public SummonWarcrystalHandler(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{
			string ammo = "";
			switch (UtilCollection.Random(1, 2))
			{
				case 1:
					ammo = "mystic_ammo_heat";
					break;
				case 2:
					ammo = "mystic_ammo_cold";
					break;
			}
			DbItemTemplates template = GameServer.Database.FindObjectByKey<DbItemTemplates>(ammo);
			if (template != null)
			{
                items.Add(GameInventoryItem.Create(template));
                foreach (InventoryItem item in items)
                {
                    if (item.IsStackable)
                    {
                        item.Count = 1;
                        item.Weight = item.Count * item.Weight;
                    }
                }

			}
		}
	}
	#endregion

	//shared timer 1
	#region Convoker-6
	[SpellHandlerAttribute("Battlewarder")]
	public class BattlewarderHandler : SpellHandler
	{
		private GameNpc warder;
		private GameSpellEffect m_effect;
		/// <summary>
		/// Execute battle warder summon spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}
		public override bool IsOverwritable(GameSpellEffect compare)
		{
			return false;
		}
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			m_effect = effect;
			if (effect.Owner == null || !effect.Owner.IsAlive)
				return;

			if ((effect.Owner is GamePlayer))
			{
				GamePlayer casterPlayer = effect.Owner as GamePlayer;
				if (casterPlayer.GroundTarget != null && casterPlayer.GroundTargetInView)
				{
					GameEventMgr.AddHandler(casterPlayer, GamePlayerEvent.Moving, new CoreEventHandler(PlayerMoves));
					GameEventMgr.AddHandler(warder, GameLivingEvent.Dying, new CoreEventHandler(BattleWarderDie));
					GameEventMgr.AddHandler(casterPlayer, GamePlayerEvent.CastStarting, new CoreEventHandler(PlayerMoves));
					GameEventMgr.AddHandler(casterPlayer, GamePlayerEvent.AttackFinished, new CoreEventHandler(PlayerMoves));
					warder.X = casterPlayer.GroundTarget.X;
					warder.Y = casterPlayer.GroundTarget.Y;
					warder.Z = casterPlayer.GroundTarget.Z;
					warder.AddBrain(new MLBrain());
					warder.AddToWorld();
				}
				else
				{
					MessageToCaster("Your area target is out of range.  Set a closer ground position.", EChatType.CT_SpellResisted);
					effect.Cancel(false);
				}
			}
		}
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if (warder != null)
			{
				GameEventMgr.RemoveHandler(warder, GameLivingEvent.Dying, new CoreEventHandler(BattleWarderDie));
				warder.RemoveBrain(warder.Brain);
				warder.Health = 0;
				warder.Delete();
			}
			if ((effect.Owner is GamePlayer))
			{
				GamePlayer casterPlayer = effect.Owner as GamePlayer;
				GameEventMgr.RemoveHandler(casterPlayer, GamePlayerEvent.Moving, new CoreEventHandler(PlayerMoves));
				GameEventMgr.RemoveHandler(casterPlayer, GamePlayerEvent.CastStarting, new CoreEventHandler(PlayerMoves));
				GameEventMgr.RemoveHandler(casterPlayer, GamePlayerEvent.AttackFinished, new CoreEventHandler(PlayerMoves));
			}
			effect.Owner.EffectList.Remove(effect);
			return base.OnEffectExpires(effect, noMessages);
		}

		// Event : player moves, lose focus
		public void PlayerMoves(CoreEvent e, object sender, EventArgs args)
		{
			GameLiving player = sender as GameLiving;
			if (player == null) return;
			if (e == GamePlayerEvent.Moving)
			{
				MessageToCaster("Your concentration fades", EChatType.CT_SpellExpires);
				OnEffectExpires(m_effect, true);
				return;
			}
		}

		// Event : Battle warder has died
		private void BattleWarderDie(CoreEvent e, object sender, EventArgs args)
		{
			GameNpc kWarder = sender as GameNpc;
			if (kWarder == null) return;
			if (e == GameLivingEvent.Dying)
			{
				MessageToCaster("Your Battle Warder has fallen!", EChatType.CT_SpellExpires);
				OnEffectExpires(m_effect, true);
				return;
			}
		}
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (!base.CheckBeginCast(selectedTarget)) return false;
			if (!(m_caster.GroundTarget != null && m_caster.GroundTargetInView))
			{
				MessageToCaster("Your area target is out of range.  Set a closer ground position.", EChatType.CT_SpellResisted);
				return false;
			}
			return true;

		}
		public BattlewarderHandler(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{
			warder = new GameNpc();
			//Fill the object variables
			warder.CurrentRegion = caster.CurrentRegion;
			warder.Heading = (ushort)((caster.Heading + 2048) % 4096);
			warder.Level = 70;
			warder.Realm = caster.Realm;
			warder.Name = "Battle Warder";
			warder.Model = 993;
			warder.CurrentSpeed = 0;
			warder.MaxSpeedBase = 0;
			warder.GuildName = "";
			warder.Size = 50;
		}
	}
	#endregion

	//no shared timer
	#region Convoker-7
	[SpellHandlerAttribute("DissonanceTrap")]
	public class DissonanceTrapHandler : MineSpellHandler
	{
		// constructor
		public DissonanceTrapHandler(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{
			//Construct a new mine.
			mine = new GameMine();
			mine.Model = 2588;
			mine.Name = spell.Name;
			mine.Realm = caster.Realm;
			mine.X = caster.X;
			mine.Y = caster.Y;
			mine.Z = caster.Z;
			mine.CurrentRegionID = caster.CurrentRegionID;
			mine.Heading = caster.Heading;
			mine.Owner = (GamePlayer)caster;

			// Construct the mine spell
			dbs = new DBSpell();
			dbs.Name = spell.Name;
			dbs.Icon = 7255;
			dbs.ClientEffect = 7255;
			dbs.Damage = spell.Damage;
			dbs.DamageType = (int)spell.DamageType;
			dbs.Target = "Enemy";
			dbs.Radius = 0;
			dbs.Type = ESpellType.DirectDamage.ToString();
			dbs.Value = spell.Value;
			dbs.Duration = spell.ResurrectHealth;
			dbs.Frequency = spell.ResurrectMana;
			dbs.Pulse = 0;
			dbs.PulsePower = 0;
			dbs.LifeDrainReturn = spell.LifeDrainReturn;
			dbs.Power = 0;
			dbs.CastTime = 0;
			dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
			sRadius = 350;
			s = new Spell(dbs, 1);
			sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
			trap = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
		}
	}
	#endregion

	//no shared timer
	#region Convoker-8
	[SpellHandler("BrittleGuard")]
	public class BrittleGuardHandler : MasterLevelHandling
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		GameNpc summoned = null;
		GameSpellEffect beffect = null;
		public BrittleGuardHandler(GameLiving caster, Spell spell, SpellLine line)
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

		/// <summary>
		/// Apply effect on target or do spell action if non duration spell
		/// </summary>
		/// <param name="target">target that gets the effect</param>
		/// <param name="effectiveness">factor from 0..1 (0%-100%)</param>
		public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
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
			beffect = CreateSpellEffect(target, effectiveness);
			{
				summonloc = target.GetPointFromHeading( target.Heading, 64 );

				BrittleBrain controlledBrain = new BrittleBrain(player);
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
			if(bguard==summoned)
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
			if(summoned!=null)
			{
				summoned.Health = 0; // to send proper remove packet
				summoned.Delete();
			}
			return base.OnEffectExpires(effect, noMessages);
		}
	}
	#endregion

	//no shared timer
	#region Convoker-9
	[SpellHandlerAttribute("SummonMastery")]
	public class Convoker9Handler : MasterLevelHandling
		//public class Convoker9Handler : MasterlevelBuffHandling
	{
		private GameNpc m_living;
		private GamePlayer m_player;

		//public override eProperty Property1 { get { return eProperty.MeleeDamage; } }

		public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
		{
			foreach (NfRaJuggernautEffect jg in target.EffectList.GetAllOfType<NfRaJuggernautEffect>())
			{
				if (jg != null)
				{
					MessageToCaster("Your Pet already has an ability of this type active", EChatType.CT_SpellResisted);
					return;
				}
			}

			// Add byNefa 04.02.2011 13:35
			// Check if Necro try to use ML9 Convoker at own Pet
			if  (m_player != null && m_player.CharacterClass.ID == (int)ECharacterClass.Necromancer)
			{ // Caster is a Necro
				NecromancerPet necroPet = target as NecromancerPet;
				if (necroPet == null || necroPet.Owner == m_player)
				{ // Caster is a Nekro and his Target is his Own Pet
					MessageToCaster("You cant use this ability on your own Pet", EChatType.CT_SpellResisted);
					return;
				}
			}
			base.ApplyEffectOnTarget(target, effectiveness);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			m_living = m_player.ControlledBrain.Body;
			m_living.Level += 20;
			m_living.BaseBuffBonusCategory[(int)EProperty.MeleeDamage] += 275;
			m_living.BaseBuffBonusCategory[(int)EProperty.ArmorAbsorption] += 75;
			m_living.Size += 40;
			base.OnEffectStart(effect);
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			m_living.Level -= 20;
			m_living.BaseBuffBonusCategory[(int)EProperty.MeleeDamage] -= 275;
			m_living.BaseBuffBonusCategory[(int)EProperty.ArmorAbsorption] -= 75;
			m_living.Size -= 40;
			return base.OnEffectExpires(effect, noMessages);
		}

		public Convoker9Handler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
			m_player = caster as GamePlayer;
		}
	}
	#endregion


	//no shared timer
	#region Convoker-10
	[SpellHandler("SummonTitan")]
	public class Convoker10Handler : MasterLevelHandling
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private int x, y, z;
		GameNpc summoned = null;
		ECSGameTimer m_growTimer;
		private const int C_GROWTIMER = 2000;
		
		public Convoker10Handler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

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

		/// <summary>
		/// Apply effect on target or do spell action if non duration spell
		/// </summary>
		/// <param name="target">target that gets the effect</param>
		/// <param name="effectiveness">factor from 0..1 (0%-100%)</param>
		public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
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
			GameSpellEffect effect = CreateSpellEffect(target, effectiveness);
			TitanBrain controlledBrain = new TitanBrain(player);
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
			summoned.Flags |= GameNpc.eFlags.PEACE;
			summoned.AddToWorld();
			controlledBrain.AggressionState = EAggressionState.Aggressive;
			effect.Start(summoned);
			m_growTimer = new ECSGameTimer((GameObject)m_caster, new ECSGameTimer.ECSTimerCallback(TitanGrows), C_GROWTIMER);
		}
		
		// Make titan growing, and activate it on completition
		private int TitanGrows(ECSGameTimer timer)
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
			if (Spell.Target.ToLower() == "area")
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
	#endregion
}

#region BrittleBrain
namespace DOL.AI.Brain
{
	public class BrittleBrain : ControlledNpcBrain
	{
		public BrittleBrain(GameLiving owner)
			: base(owner)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");
		}

		public override void FollowOwner()
		{
			Body.StopAttack();
			Body.Follow(Owner, MIN_OWNER_FOLLOW_DIST, MAX_OWNER_FOLLOW_DIST);
		}
	}
}
#endregion

#region Titanbrain

namespace DOL.AI.Brain
{
	public class TitanBrain : ControlledNpcBrain
	{
		private GameLiving m_target;

		public TitanBrain(GameLiving owner)
			: base(owner)
		{
		}

		public GameLiving Target
		{
			get { return m_target; }
			set { m_target = value; }
		}

		#region AI

		public override bool Start()
		{
			if (!base.Start()) return false;
			return true;
		}

		public override bool Stop()
		{
			if (!base.Stop()) return false;
			return true;
		}

		private IList FindTarget()
		{
			ArrayList list = new ArrayList();

			foreach (GamePlayer o in Body.GetPlayersInRadius((ushort)Body.AttackRange))
			{
				GamePlayer p = o as GamePlayer;

				if (GameServer.ServerRules.IsAllowedToAttack(Body, p, true))
					list.Add(p);
			}
			return list;
		}

		public override void Think()
		{
			if (Body.TargetObject is GameNpc)
				Body.TargetObject = null;

			if (Body.attackComponent.AttackState)
				return;

			IList enemies = new ArrayList();
			if (Target == null)
				enemies = FindTarget();
			else if (!Body.IsWithinRadius(Target, Body.AttackRange))
				enemies = FindTarget();
			else if (!Target.IsAlive)
				enemies = FindTarget();
			if (enemies.Count > 0 && Target == null)
			{
				//pick a random target...
				int targetnum = UtilCollection.Random(0, enemies.Count - 1);

				//Choose a random target.
				Target = enemies[targetnum] as GameLiving;
			}
			else if (enemies.Count < 1)
			{
				WalkState = EWalkState.Stay;
				enemies = FindTarget();
			}

			if (Target != null)
			{
				if (!Target.IsAlive)
				{
					Target = null;
				}
				else if (Body.IsWithinRadius(Target, Body.AttackRange))
				{
					Body.TargetObject = Target;
					Goto(Target);
					Body.StartAttack(Target);
				}
				else
				{
					Target = null;
				}
			}
		}
		#endregion
	}
}
#endregion

#region MLBrain
public class MLBrain : GuardBrain
{
	public MLBrain() : base() { }

	public override int AggroRange
	{
		get { return 400; }
	}
	protected override void CheckNPCAggro()
	{
		//Check if we are already attacking, return if yes
		if (Body.attackComponent.AttackState)
			return;

		foreach (GameNpc npc in Body.GetNPCsInRadius((ushort)AggroRange))
		{
			if (AggroTable.ContainsKey(npc))
				continue; // add only new npcs
			if ((npc.Flags & GameNpc.eFlags.FLYING) != 0)
				continue; // let's not try to attack flying mobs
			if (!GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
				continue;
			if (!npc.IsWithinRadius(Body, AggroRange))
				continue;

			if (!(npc.Brain is IControlledBrain || npc is GameGuard))
				continue;

			AddToAggroList(npc, npc.Level << 1);
			return;
		}
	}
}
#endregion