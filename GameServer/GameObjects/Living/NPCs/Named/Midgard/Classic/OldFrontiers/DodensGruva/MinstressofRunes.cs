using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.PacketHandler;

namespace Core.GS
{
	public class MistressRunes : GameEpicBoss
	{
		public MistressRunes() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Mistress of Runes Initializing...");
		}
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(9907);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(779);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(779));

			MistressRunesBrain sbrain = new MistressRunesBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace Core.AI.Brain
{
	public class MistressRunesBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MistressRunesBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if(!CheckProximityAggro())
            {
				FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanCastSpear = false;
				RandomTarget = null;
				CanCast = false;
			}
			if(HasAggro && Body.TargetObject != null)
            {
				if(!CanCastSpear && !Body.IsCasting)
                {
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastSpears), Util.Random(10000, 15000));
					CanCastSpear=true;
                }
				if(!StartCastNS && !Body.IsCasting)
                {
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickRandomTarget), Util.Random(20000, 25000));
					StartCastNS = true;
				}
				if(!Body.IsCasting && CanCast)
                {
					if (RandomTarget != null && RandomTarget.IsAlive)
					{
						if (Body.attackComponent.AttackState && Body.IsCasting)
							Body.attackComponent.StopAttack();
						Body.TargetObject = RandomTarget;					
						if (Body.GetSkillDisabledDuration(NearsightMistress) == 0 && (!Body.IsCasting || Body.IsCasting))
							Body.TurnTo(RandomTarget);
						Body.CastSpell(NearsightMistress, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
					}
				}
			}
			base.Think();
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
			}
		}
        #region Spear
		private bool CanCastSpear = false;
		private int CastSpears(EcsGameTimer timer)
        {
			if (HasAggro && !Body.IsCasting)
			{
				string message = mistress_text[Util.Random(0, mistress_text.Count - 1)];
				BroadcastMessage(message);
				Body.CastSpell(AoESpear, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
			CanCastSpear = false;
			return 0;
        }
		List<string> mistress_text = new List<string>()
		{
			"Mistress of Runes casts a magical flaming spear!",
			"Mistress of Runes drops a flaming spear from above!",
			"Mistress of Runes uses all her might to create a flaming spear.",
			"Mistress of Runes casts a dangerous spell!"
		};
		#endregion
		#region Nearsight
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		public static bool CanCast = false;
		public static bool StartCastNS = false;
		List<GamePlayer> Enemys_To_NS = new List<GamePlayer>();
		private int PickRandomTarget(EcsGameTimer timer)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1
						&& (!player.effectListComponent.ContainsEffectForEffectType(EEffect.Nearsight) 
						&& (!player.effectListComponent.ContainsEffectForEffectType(EEffect.NearsightImmunity)))
						&& !Enemys_To_NS.Contains(player))
					{
						Enemys_To_NS.Add(player);
					}
				}
			}
			if (Enemys_To_NS.Count > 0)
			{
				if (CanCast == false)
				{
					GamePlayer Target = Enemys_To_NS[Util.Random(0, Enemys_To_NS.Count - 1)];//pick random target from list
					RandomTarget = Target;//set random target to static RandomTarget
					BroadcastMessage(RandomTarget.Name + " can no longer see properly in the vicinity!");
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetDot), 2000);
					CanCast = true;
				}
			}
			else
				StartCastNS = false;
			return 0;
		}
		public int ResetDot(EcsGameTimer timer)//reset here so boss can start dot again
		{
			if(RandomTarget != null && Enemys_To_NS.Contains(RandomTarget) && RandomTarget.effectListComponent.ContainsEffectForEffectType(EEffect.Nearsight))
				Enemys_To_NS.Remove(RandomTarget);

			//RandomTarget = null;
			CanCast = false;
			StartCastNS = false;
			return 0;
		}
		#endregion
		private Spell m_NearsightMistressSpell;
		/// <summary>
		/// The Nearsight spell.
		/// </summary>
		private Spell NearsightMistress
		{
			get
			{
				if (m_NearsightMistressSpell == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.Uninterruptible = true;
					spell.CastTime = 2;
					spell.ClientEffect = 2735;
					spell.Icon = 2735;
					spell.TooltipId = 2735;
					spell.Description = "Decreases the target's range of vision by 65%";
					spell.Name = "Mistress's Vision";
					spell.Range = 2300;
					spell.Radius = 0;
					spell.RecastDelay = 0;
					spell.Value = 65;
					spell.Duration = 60;
					spell.Damage = 0;
					spell.DamageType = (int)EDamageType.Cold;
					spell.SpellID = 18920;
					spell.Target = "Enemy";
					spell.Type = ESpellType.Nearsight.ToString();
					spell.Message1 = "You are blinded!";
					spell.Message2 = "{0} is blinded!";
					m_NearsightMistressSpell = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_NearsightMistressSpell);
				}
				return m_NearsightMistressSpell;
			}
		}
		private Spell m_AoESpell;
		/// <summary>
		/// The AoE spell.
		/// </summary>
		private Spell AoESpear
		{
			get
			{
				if (m_AoESpell == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.Uninterruptible = true;
					spell.CastTime = 3;
					spell.ClientEffect = 2958;
					spell.Icon = 2958;
					spell.Damage = 650;
					spell.Name = "Odin's Hatred";
					spell.Range = 1500;
					spell.Radius = 450;
					spell.SpellID = 18921;
					spell.RecastDelay = 0;
					spell.Target = "Enemy";
					spell.Type = ESpellType.DirectDamageNoVariance.ToString();
					spell.MoveCast = false;
					spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
					m_AoESpell = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AoESpell);
				}
				return m_AoESpell;
			}
		}
	}
}