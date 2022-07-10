using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Styles;
using System.Collections.Generic;

namespace DOL.GS
{
	public class DrihtenElreden : GameEpicBoss
	{
		public DrihtenElreden() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Drihten Elreden Initializing...");
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
		public static int TauntID = 240;
		public static int TauntClassID = 10;
		public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

		public static int AfterEvadeID = 238;
		public static int AfterEvadeClassID = 10;
		public static Style AfterEvade = SkillBase.GetStyleByID(AfterEvadeID, AfterEvadeClassID);

		public static int EvadeFollowUPID = 242;
		public static int EvadeFollowUPClassID = 10;
		public static Style EvadeFollowUP = SkillBase.GetStyleByID(AfterEvadeID, EvadeFollowUPClassID);

        public override void OnAttackedByEnemy(AttackData ad)
        {
			if (ad != null && ad.AttackResult == eAttackResult.Evaded)
			{
				styleComponent.NextCombatBackupStyle = AfterEvade;
				styleComponent.NextCombatStyle = EvadeFollowUP;
			}
            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad)
        {
			if(ad != null && ad.AttackResult == eAttackResult.HitUnstyled)
            {
				styleComponent.NextCombatBackupStyle = taunt;
				styleComponent.NextCombatStyle = AfterEvade;
            }
			if (ad != null && ad.AttackResult == eAttackResult.HitStyle && ad.Style.ID == 238 && ad.Style.ClassID == 10)
			{
				styleComponent.NextCombatStyle = EvadeFollowUP;
			}
			base.OnAttackEnemy(ad);
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
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			Model = 919;
			Level = (byte)(Util.Random(78, 80));
			Name = "Drihten Elreden";
			Size = 120;

			Strength = 280;
			Dexterity = 150;
			Constitution = 100;
			Quickness = 80;
			Piety = 200;
			Intelligence = 200;
			Charisma = 200;
			Empathy = 400;

			if(!Styles.Contains(taunt))
				Styles.Add(taunt);
			if (!Styles.Contains(AfterEvade))
				Styles.Add(AfterEvade);
			if (!Styles.Contains(EvadeFollowUP))
				Styles.Add(EvadeFollowUP);

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 881, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = eDamageType.Crush;
			ParryChance = 30;
			EvadeChance = 50;

			MaxSpeedBase = 250;
			MaxDistance = 3500;
			TetherRange = 3800;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			DrihtenElredenBrain sbrain = new DrihtenElredenBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class DrihtenElredenBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DrihtenElredenBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool canbringhelp = false;
		List<GameNPC> CallHelp = new List<GameNPC>();
		List<GameNPC> PulledMobs = new List<GameNPC>();
		public void BringHelp()
        {
			if(HasAggro)
            {
				foreach(GameNPC npc in Body.GetNPCsInRadius(2500))
                {
					if (npc == null) continue;
					if(npc.IsAlive && npc.PackageID == "DrihtenBaf" && !CallHelp.Contains(npc) && !PulledMobs.Contains(npc))
						CallHelp.Add(npc);
                }
            }
			if(CallHelp.Count > 0)
            {
				GameNPC friend = CallHelp[Util.Random(0, CallHelp.Count - 1)];
				GameLiving target = Body.TargetObject as GameLiving;
				if(target != null && target.IsAlive && friend != null && friend.Brain is StandardMobBrain brain)
                {
					if (!brain.HasAggro)
					{
						brain.AddToAggroList(target, 100);
						if(CallHelp.Contains(friend))
							CallHelp.Remove(friend);
						if(!PulledMobs.Contains(friend))
							PulledMobs.Add(friend);
					}
                }
			}
        }
		public int PickRandomMob(ECSGameTimer timer)
        {
			if(HasAggro)
				BringHelp();
			canbringhelp = false;
			return 0;
        }
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				canbringhelp=false;
				if (CallHelp.Count > 0)
					CallHelp.Clear();
				if(PulledMobs.Count > 0)
					PulledMobs.Clear();
			}
			if (HasAggro && Body.TargetObject != null)
			{
				if (!Body.effectListComponent.ContainsEffectForEffectType(eEffect.MeleeHasteBuff))
					Body.CastSpell(Boss_Haste_Buff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

				if (canbringhelp==false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickRandomMob), Util.Random(15000, 30000));
					canbringhelp=true;
                }
			}
			base.Think();
		}
		private Spell m_Boss_Haste_Buff;
		private Spell Boss_Haste_Buff
		{
			get
			{
				if (m_Boss_Haste_Buff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 50;
					spell.Duration = 25;
					spell.ClientEffect = 1727;
					spell.Icon = 1727;
					spell.Name = "Alacrity of the Heavenly Host";
					spell.Message2 = "{0} begins attacking faster!";
					spell.Message4 = "{0}'s attacks return to normal.";
					spell.TooltipId = 1727;
					spell.Range = 500;
					spell.Value = 38;
					spell.SpellID = 11888;
					spell.Target = "Self";
					spell.Type = eSpellType.CombatSpeedBuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Boss_Haste_Buff = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_Haste_Buff);
				}
				return m_Boss_Haste_Buff;
			}
		}
	}
}