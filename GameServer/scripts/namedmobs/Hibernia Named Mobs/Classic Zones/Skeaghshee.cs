using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;

namespace DOL.GS
{
	public class Skeaghshee : GameEpicBoss
	{
		public Skeaghshee() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Skeaghshee Initializing...");
		}
		//he is immune to any magic dmg
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
					damageType == eDamageType.Energy || damageType == eDamageType.Heat
					|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit)
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GamePet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(Name + " is immune to magic damage!", eChatType.CT_System,eChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
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
			get { return 30000; }
		}
		public static int TauntID = 247;
		public static int TauntClassID = 44;
		public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

		public static int BehindID = 256;
		public static int BehindClassID = 44;
		public static Style behind = SkillBase.GetStyleByID(BehindID, BehindClassID);

		public static int BehindFollowUpID = 259;
		public static int BehindFollowUpClassID = 44;
		public static Style behindFollowUp = SkillBase.GetStyleByID(BehindFollowUpID, BehindFollowUpClassID);

		public static int AfterParryID = 246;
		public static int AfterParryClassID = 44;
		public static Style afterParry = SkillBase.GetStyleByID(AfterParryID, AfterParryClassID);

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166165);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			LoadEquipmentTemplateFromDatabase("d39e7d76-c7f3-4f79-a074-1eb441e83271");
			if (!Styles.Contains(taunt))
				Styles.Add(taunt);
			if (!Styles.Contains(behind))
				Styles.Add(behind);
			if (!Styles.Contains(behindFollowUp))
				Styles.Add(behindFollowUp);
			if (!Styles.Contains(afterParry))
				Styles.Add(afterParry);

			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			SkeaghsheeBrain sbrain = new SkeaghsheeBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void OnAttackedByEnemy(AttackData ad)
        {
			if (ad != null)
            {
				if(ad.AttackResult == eAttackResult.Parried)
                {
					styleComponent.NextCombatStyle = afterParry;
					styleComponent.NextCombatBackupStyle = taunt;
                }
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad)
        {
			if(ad != null)
            {
				if (ad.AttackResult == eAttackResult.HitStyle && ad.Style.ID == 259 && ad.Style.ClassID == 44)
				{
					styleComponent.NextCombatStyle = behindFollowUp;
					styleComponent.NextCombatBackupStyle = taunt;
				}
			}
            base.OnAttackEnemy(ad);
        }
    }
}
namespace DOL.AI.Brain
{
	public class SkeaghsheeBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SkeaghsheeBrain() : base()
		{
			AggroLevel = 0;//he is neutral
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
				INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166165);
				Body.ParryChance = npcTemplate.ParryChance;
			}
			if(Body.TargetObject != null && HasAggro)
            {
				float angle = Body.TargetObject.GetAngle(Body);
				if (angle >= 160 && angle <= 200)
                {
					Body.styleComponent.NextCombatStyle = Skeaghshee.behind;//do backstyle when angle allow it
					Body.styleComponent.NextCombatBackupStyle = Skeaghshee.behindFollowUp;
				}
				else
                {
					Body.ParryChance = 15;
					Body.styleComponent.NextCombatStyle = Skeaghshee.afterParry;//do backstyle when angle allow it
					Body.styleComponent.NextCombatBackupStyle = Skeaghshee.taunt;
				}
			}
			base.Think();
		}
	}
}

