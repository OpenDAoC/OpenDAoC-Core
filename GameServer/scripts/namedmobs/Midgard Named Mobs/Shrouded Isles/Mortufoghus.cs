using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Mortufoghus : GameEpicBoss
	{
		public Mortufoghus() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Mortufoghus Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 40000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164200);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			MortufoghusBrain sbrain = new MortufoghusBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
			{
				if (Util.Chance(35) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun) && ad.Target.IsAlive)
					CastSpell(Mortufoghus_stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
		#region Spells
		private Spell m_Mortufoghus_stun;
		private Spell Mortufoghus_stun
		{
			get
			{
				if (m_Mortufoghus_stun == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 2165;
					spell.Icon = 2132;
					spell.TooltipId = 2132;
					spell.Duration = 6;
					spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
					spell.Name = "Stun";
					spell.Range = 400;
					spell.SpellID = 11890;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Stun.ToString();
					m_Mortufoghus_stun = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mortufoghus_stun);
				}
				return m_Mortufoghus_stun;
			}
		}
		#endregion
	}
}
namespace DOL.AI.Brain
{
	public class MortufoghusBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MortufoghusBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsTargetPicked = false;
				RandomTarget = null;
				if (Port_Enemys.Count > 0)
					Port_Enemys.Clear();
			}
			if(HasAggro && Body.TargetObject != null)
            {
				if(!Body.IsCasting && Util.Chance(50))
					Body.CastSpell(MortufoghusDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
				if (IsTargetPicked == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ThrowPlayer), Util.Random(25000, 35000));//timer to port and pick player
					IsTargetPicked = true;
				}
			}
			base.Think();
		}
		#region Throw Player
		List<GamePlayer> Port_Enemys = new List<GamePlayer>();
		public static bool IsTargetPicked = false;
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		public int ThrowPlayer(ECSGameTimer timer)
		{
			if (Body.IsAlive && HasAggro)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
				{
					if (player != null)
					{
						if (player.IsAlive && player.Client.Account.PrivLevel == 1)
						{
							if (!Port_Enemys.Contains(player) && player != Body.TargetObject)
								Port_Enemys.Add(player);
						}
					}
				}
				if (Port_Enemys.Count > 0)
				{
					GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
					RandomTarget = Target;
					if (RandomTarget.IsAlive && RandomTarget != null)
					{
						RandomTarget.MoveTo(Body.CurrentRegionID, Body.X+Util.Random(-1500,1500), Body.Y + Util.Random(-1500, 1500), Body.Z, Body.Heading);
						RandomTarget.TakeDamage(RandomTarget, eDamageType.Falling, RandomTarget.MaxHealth / 5, 0);
						RandomTarget.Out.SendMessage("You take falling damage!", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
						Port_Enemys.Remove(RandomTarget);
					}
				}
				RandomTarget = null;//reset random target to null
				IsTargetPicked = false;
			}
			return 0;
		}
		#endregion
		#region Spells
		private Spell m_MortufoghusDD;
		public Spell MortufoghusDD
		{
			get
			{
				if (m_MortufoghusDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 2;
					spell.RecastDelay = Util.Random(15,25);
					spell.ClientEffect = 14315;
					spell.Icon = 14315;
					spell.Damage = 250;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Name = "Dark Packt";
					spell.Range = 500;
					spell.Radius = 1000;
					spell.SpellID = 11891;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_MortufoghusDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MortufoghusDD);
				}
				return m_MortufoghusDD;
			}
		}
		#endregion
	}
}

