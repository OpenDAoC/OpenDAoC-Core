using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using System;

namespace DOL.GS
{
	public class Ydenia : GameEpicBoss
	{
		public Ydenia() : base() { }

		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
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
			get { return 20000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168100);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = ServerProperties.Properties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			YdeniaBrain sbrain = new YdeniaBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			var throwPlayer = TempProperties.getProperty<ECSGameTimer>("ydenia_teleport");//cancel teleport
			if (throwPlayer != null)
			{
				throwPlayer.Stop();
				TempProperties.removeProperty("ydenia_teleport");
			}
			base.Die(killer);
        }
		public override void DealDamage(AttackData ad)
		{
			if (ad != null && ad.AttackType == AttackData.eAttackType.Spell && ad.Damage > 0 && ad.DamageType == eDamageType.Body)
			{
				Health += ad.Damage;
			}
			base.DealDamage(ad);
		}
	}
}
namespace DOL.AI.Brain
{
	public class YdeniaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public YdeniaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		private bool canPort = false;
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
			{
				player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
			}
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				canPort = false;
				var throwPlayer = Body.TempProperties.getProperty<ECSGameTimer>("ydenia_teleport");//cancel teleport
				if (throwPlayer != null)
				{
					throwPlayer.Stop();
					Body.TempProperties.removeProperty("ydenia_teleport");
				}
			}
			if (Body.TargetObject != null && HasAggro)
			{
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.Name.ToLower() == "dark seithkona" && npc.Brain is StandardMobBrain brain)
					{
						if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
							brain.AddToAggroList(target, 100);
					}
				}
				if(target != null && target.IsAlive)
                {
					if (Util.Chance(100) && !Body.IsCasting)
					{
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.StrConDebuff))
							Body.CastSpell(Ydenia_SC_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					}
					if (Util.Chance(100) && !Body.IsCasting)
						Body.CastSpell(YdeniaDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				if(Util.Chance(35) && !canPort)
                {
					ECSGameTimer portTimer = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(InitiatePort), Util.Random(25000, 35000));
					Body.TempProperties.setProperty("ydenia_teleport", portTimer);
					canPort = true;
                }				
			}
			base.Think();
		}
		private int InitiatePort(ECSGameTimer timer)
        {
			GameLiving target = Body.TargetObject as GameLiving;
			BroadcastMessage(String.Format("{0} says, \"Feel the power of the Seithkona, fool!\"", Body.Name));
			YdeniaPort(target);
			return 0;
        }
		private void YdeniaPort(GameLiving target)
        {
			if(target != null && target.IsAlive && target is GamePlayer player)
            {
				switch(Util.Random(1,4))
                {
					case 1: 
						player.MoveTo(100, 664713, 896689, 1553, 2373);
						player.TakeDamage(player, eDamageType.Cold, player.MaxHealth / 7, 0);
						foreach (GamePlayer players in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						{
							if (players != null)
								player.Out.SendSpellEffectAnimation(player, player, 4074, 0, false, 1);
						}
						player.Out.SendMessage("Ydenia of the Seithkona throws you into water and you take damage!", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
						break;
					case 2:
						player.MoveTo(100, 667220, 894261, 1543, 692);
						player.TakeDamage(player, eDamageType.Cold, player.MaxHealth / 7, 0);
						foreach (GamePlayer players in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						{
							if (players != null)
								player.Out.SendSpellEffectAnimation(player, player, 4074, 0, false, 1);
						}
						player.Out.SendMessage("Ydenia of the Seithkona throws you into water and you take damage!", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
						break;
					case 3:
						player.MoveTo(100, 665968, 892792, 1561, 235);
						player.TakeDamage(player, eDamageType.Cold, player.MaxHealth / 7, 0);
						foreach (GamePlayer players in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						{
							if (players != null)
								player.Out.SendSpellEffectAnimation(player, player, 4074, 0, false, 1);
						}
						player.Out.SendMessage("Ydenia of the Seithkona throws you into water and you take damage!", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
						break;
					case 4:
						player.MoveTo(100, 663895, 893446, 1554, 3482);
						player.TakeDamage(player, eDamageType.Cold, player.MaxHealth / 7, 0);
						foreach (GamePlayer players in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						{
							if (players != null)
								player.Out.SendSpellEffectAnimation(player, player, 4074, 0, false, 1);
						}
						player.Out.SendMessage("Ydenia of the Seithkona throws you into water and you take damage!", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
						break;
				}
            }
			canPort = false;
        }
		#region Spells
		private Spell m_YdeniaDD;
		private Spell YdeniaDD
		{
			get
			{
				if (m_YdeniaDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.Power = 0;
					spell.RecastDelay = Util.Random(4,6);
					spell.ClientEffect = 9191;
					spell.Icon = 9191;
					spell.Damage = 320;
					spell.DamageType = (int)eDamageType.Body;
					spell.Name = "Lifedrain";
					spell.Range = 1500;
					spell.SpellID = 12010;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_YdeniaDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_YdeniaDD);
				}
				return m_YdeniaDD;
			}
		}
		private Spell m_Ydenia_SC_Debuff;
		private Spell Ydenia_SC_Debuff
		{
			get
			{
				if (m_Ydenia_SC_Debuff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 30;
					spell.Duration = 60;
					spell.ClientEffect = 2767;
					spell.Icon = 2767;
					spell.Name = "Emasculate Strength";
					spell.TooltipId = 2767;
					spell.Range = 1000;
					spell.Value = 66;
					spell.Radius = 400;
					spell.SpellID = 12011;
					spell.Target = "Enemy";
					spell.Type = eSpellType.StrengthConstitutionDebuff.ToString();
					m_Ydenia_SC_Debuff = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Ydenia_SC_Debuff);
				}
				return m_Ydenia_SC_Debuff;
			}
		}
		#endregion
	}
}
