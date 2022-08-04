using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Styles;
using DOL.GS;

namespace DOL.GS
{
	public class WrathOfMordred : GameEpicBoss
	{
		public WrathOfMordred() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Wrath of Mordred Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 30;// dmg reduction for melee dmg
				case eDamageType.Crush: return 30;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 30;// dmg reduction for melee dmg
				default: return 40;// dmg reduction for rest resists
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
		public static Style Taunt2h = SkillBase.GetStyleByID(103, 2);//2h style taunt
		public static Style AfterParry = SkillBase.GetStyleByID(108, 2); // after parry
		public static Style ParryFollowUP = SkillBase.GetStyleByID(112, 2);//parry followup
		public static Style Side2H = SkillBase.GetStyleByID(107, 2);//side
		public static Style SideFollowUP = SkillBase.GetStyleByID(114, 2);
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(13039);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(18);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(18));
			if (!Styles.Contains(Taunt2h))
				Styles.Add(Taunt2h);
			if (!Styles.Contains(AfterParry))
				Styles.Add(AfterParry);
			if (!Styles.Contains(ParryFollowUP))
				Styles.Add(ParryFollowUP);
			if (!Styles.Contains(Side2H))
				Styles.Add(Side2H);
			if (!Styles.Contains(SideFollowUP))
				Styles.Add(SideFollowUP);

			WrathOfMordredBrain sbrain = new WrathOfMordredBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void OnAttackedByEnemy(AttackData ad)
        {
			if(ad != null && ad.AttackResult == eAttackResult.Parried)
            {
				styleComponent.NextCombatBackupStyle = AfterParry;//boss parried so prepare after parry style backup style
				styleComponent.NextCombatStyle = ParryFollowUP;//main style after parry followup
			}
            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad)
        {
			if (ad != null && ad.AttackResult == eAttackResult.HitUnstyled)
			{
				styleComponent.NextCombatBackupStyle = Taunt2h;//boss hit unstyled so taunt
				styleComponent.NextCombatStyle = AfterParry;
			}
			if (ad.AttackResult == eAttackResult.HitStyle && ad.Style.ID == 108 && ad.Style.ClassID == 2)
            {
				styleComponent.NextCombatBackupStyle = Taunt2h;
				styleComponent.NextCombatStyle = ParryFollowUP;
			}
			if (Util.Chance(15))//cast nasty heat proc
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
				{
					CastSpell(HeatProc, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.OnAttackEnemy(ad);
        }
		private Spell m_HeatProc;
		private Spell HeatProc
		{
			get
			{
				if (m_HeatProc == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 4051;
					spell.Icon = 4051;
					spell.TooltipId = 4051;
					spell.Damage = 600;
					spell.Name = "Heat Proc";
					spell.Range = 350;
					spell.Radius = 300;
					spell.SpellID = 11895;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_HeatProc = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_HeatProc);
				}
				return m_HeatProc;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class WrathOfMordredBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WrathOfMordredBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool CanWalk = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanWalk = false;
			}
			if (Body.IsAlive && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					GameLiving living = Body.TargetObject as GameLiving;
					float angle = Body.TargetObject.GetAngle(Body);
					if ((angle >= 45 && angle < 150) || (angle >= 210 && angle < 315))//side
					{
						Body.styleComponent.NextCombatBackupStyle = WrathOfMordred.Side2H;
						Body.styleComponent.NextCombatStyle = WrathOfMordred.SideFollowUP;
					}
					if (!living.effectListComponent.ContainsEffectForEffectType(eEffect.Stun))
					{
						if (CanWalk == false && Body.SwingTimeLeft <= 800)
						{
							Body.styleComponent.NextCombatStyle = null;
							Body.styleComponent.NextCombatBackupStyle = null;
							Body.styleComponent.NextCombatBackupStyle = WrathOfMordred.Side2H;
							Body.styleComponent.NextCombatStyle = WrathOfMordred.SideFollowUP;
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(WalkSide), 300);
							CanWalk = true;
						}
					}
				}
			}
			base.Think();
		}
		private int WalkSide(ECSGameTimer timer)
		{
			if (Body.IsAlive && HasAggro && Body.TargetObject != null && Body.IsWithinRadius(Body.TargetObject, Body.AttackRange))
			{
				if (Body.TargetObject is GameLiving)
				{
					GameLiving living = Body.TargetObject as GameLiving;
					float angle = living.GetAngle(Body);
					Point2D positionalPoint;
					positionalPoint = living.GetPointFromHeading((ushort)(living.Heading + (90 * (4096.0 / 360.0))), 65);
					//Body.WalkTo(positionalPoint.X, positionalPoint.Y, living.Z, 280);
					Body.X = positionalPoint.X;
					Body.Y = positionalPoint.Y;
					Body.Z = living.Z;
					Body.Heading = 1250;
				}
			}
			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetWalkSide), Util.Random(15000,25000));
			return 0;
		}
		private int ResetWalkSide(ECSGameTimer timer)
        {
			CanWalk = false;
			return 0;
        }
	}
}

