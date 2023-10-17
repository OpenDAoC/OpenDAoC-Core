using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS;

public class Nosdoden : GameEpicBoss
{
	protected String[] m_deathAnnounce;
	public Nosdoden() : base() 
	{
		m_deathAnnounce = new String[] { "The earth lurches beneath your feet as {0} staggers and topples to the ground.",
			"A glowing light begins to form on the mound that served as {0}'s lair." };
	}
	#region Custom methods
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}
	protected void ReportNews(GameObject killer)
	{
		int numPlayers = GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Count;
		String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
		NewsMgr.CreateNews(message, killer.Realm, ENewsType.PvE, true);

		if (Properties.GUILD_MERIT_ON_DRAGON_KILL > 0)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player.IsEligibleToGiveMeritPoints)
				{
					GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_DRAGON_KILL);
				}
			}
		}
	}

	/// <summary>
	/// Award dragon kill point for each XP gainer.
	/// </summary>
	/// <returns>The number of people involved in the kill.</returns>
	protected int AwardDragonKillPoint()
	{
		int count = 0;
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
		{
			player.KillsDragon++;
			count++;
		}
		return count;
	}
	public override void Die(GameObject killer)
	{
		// debug
		if (killer == null)
			log.Error("Nosdoden Killed: killer is null!");
		else
			log.Debug("Nosdoden Killed: killer is " + killer.Name + ", attackers:");

		bool canReportNews = true;
		// due to issues with attackers the following code will send a notify to all in area in order to force quest credit
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
		{
			player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

			if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
			{
				if (player.Client.Account.PrivLevel == (int)EPrivLevel.Player)
					canReportNews = false;
			}
		}

		AwardDragonKillPoint();

		base.Die(killer);
		foreach (String message in m_deathAnnounce)
		{
			BroadcastMessage(String.Format(message, Name));
		}
		if (canReportNews)
		{
			ReportNews(killer);
		}
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc != null && npc.IsAlive && npc.Brain is NosdodenGhostAddBrain)
				npc.Die(this);
		}
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc != null && npc.IsAlive && npc.Brain is NosdodenSummonedAddsBrain)
				npc.RemoveFromWorld();
		}
	}
	#endregion
	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Nosdoden Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40; // dmg reduction for melee dmg
			case EDamageType.Crush: return 40; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 40; // dmg reduction for melee dmg
			default: return 70; // dmg reduction for rest resists
		}
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
		get { return 100000; }
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164545);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		Faction = FactionMgr.GetFactionByID(150);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
		RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		NosdodenBrain sbrain = new NosdodenBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public override void EnemyKilled(GameLiving enemy)
    {
		GamePlayer player = enemy as GamePlayer;
		if (enemy is GamePlayer)
		{
			if (player != null)
			{
				NosdodenGhostAdd add = new NosdodenGhostAdd();
				add.Name = "Spirit of " + player.Name;
				add.X = player.X;
				add.Y = player.Y;
				add.Z = player.Z;
				add.Size = (byte)player.Size;
				add.Flags = ENpcFlags.GHOST;
                #region Set mob model
                if (player.Race == (short)ERace.Norseman && player.Gender == EGender.Male)//norse male
					add.Model = (ushort)Util.Random(153, 160);
				if (player.Race == (short)ERace.Norseman && player.Gender == EGender.Female)//norse female
					add.Model = (ushort)Util.Random(161, 168);
				if (player.Race == (short)ERace.Troll && player.Gender == EGender.Male)//troll male
					add.Model = (ushort)Util.Random(137, 144);
				if (player.Race == (short)ERace.Troll && player.Gender == EGender.Female)//troll female
					add.Model = (ushort)Util.Random(145, 152);
				if (player.Race == (short)ERace.Kobold && player.Gender == EGender.Male)//kobolt male
					add.Model = (ushort)Util.Random(169, 176);
				if (player.Race == (short)ERace.Kobold && player.Gender == EGender.Female)//kobolt female
					add.Model = (ushort)Util.Random(177, 184);
				if (player.Race == (short)ERace.Valkyn && player.Gender == EGender.Male)//valkyn male
					add.Model = (ushort)Util.Random(773, 780);
				if (player.Race == (short)ERace.Valkyn && player.Gender == EGender.Female)//valkyn female
					add.Model = (ushort)Util.Random(781, 788);
				if (player.Race == (short)ERace.Dwarf && player.Gender == EGender.Male)//dwarf male
					add.Model = (ushort)Util.Random(185, 192);
				if (player.Race == (short)ERace.Dwarf && player.Gender == EGender.Female)//dwarf female
					add.Model = (ushort)Util.Random(193, 200);
                #endregion
                add.Heading = Heading;
				add.CurrentRegionID = CurrentRegionID;
				add.RespawnInterval = -1;
                #region equiptemplate for mob and styles
                GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();					
				if (player.Inventory.GetItem(EInventorySlot.TorsoArmor) != null)
				{
					DbInventoryItem torso = player.Inventory.GetItem(EInventorySlot.TorsoArmor);
					if(torso != null)
						template.AddNPCEquipment(EInventorySlot.TorsoArmor, torso.Model, torso.Color,0,torso.Extension);//modelID,color,effect,extension
				}
				if (player.Inventory.GetItem(EInventorySlot.ArmsArmor) != null)
				{
					DbInventoryItem arms = player.Inventory.GetItem(EInventorySlot.ArmsArmor);
					if(arms != null)
						template.AddNPCEquipment(EInventorySlot.ArmsArmor, arms.Model, arms.Color);
				}
				if (player.Inventory.GetItem(EInventorySlot.LegsArmor) != null)
				{
					DbInventoryItem legs = player.Inventory.GetItem(EInventorySlot.LegsArmor);
					if(legs != null)
						template.AddNPCEquipment(EInventorySlot.LegsArmor, legs.Model, legs.Color);
				}
				if (player.Inventory.GetItem(EInventorySlot.HeadArmor) != null)
				{
					DbInventoryItem head = player.Inventory.GetItem(EInventorySlot.HeadArmor);
					if(head != null)
						template.AddNPCEquipment(EInventorySlot.HeadArmor, head.Model, head.Color);
				}
				if (player.Inventory.GetItem(EInventorySlot.HandsArmor) != null)
				{
					DbInventoryItem hands = player.Inventory.GetItem(EInventorySlot.HandsArmor);
					if(hands != null)
						template.AddNPCEquipment(EInventorySlot.HandsArmor, hands.Model, hands.Color,0,hands.Extension);
				}
				if (player.Inventory.GetItem(EInventorySlot.FeetArmor) != null)
				{
					DbInventoryItem feet = player.Inventory.GetItem(EInventorySlot.FeetArmor);
					if(feet != null)
						template.AddNPCEquipment(EInventorySlot.FeetArmor, feet.Model, feet.Color,0,feet.Extension);
				}
				if (player.Inventory.GetItem(EInventorySlot.Cloak) != null)
				{
					DbInventoryItem cloak = player.Inventory.GetItem(EInventorySlot.Cloak);
					if(cloak != null)
						template.AddNPCEquipment(EInventorySlot.Cloak, cloak.Model, cloak.Color,0,0,cloak.Emblem);
				}
				if (player.Inventory.GetItem(EInventorySlot.RightHandWeapon) != null)
				{
					DbInventoryItem righthand = player.Inventory.GetItem(EInventorySlot.RightHandWeapon);
					DbInventoryItem lefthand = player.Inventory.GetItem(EInventorySlot.LeftHandWeapon);
					if (righthand != null && lefthand != null)
					{
						template.AddNPCEquipment(EInventorySlot.RightHandWeapon, righthand.Model, righthand.Color, righthand.Effect);
						#region Styles for Warrior and Thane
						if (player.PlayerClass.ID == (int)EPlayerClass.Warrior || player.PlayerClass.ID == (int)EPlayerClass.Thane)
						{
							if (righthand.Object_Type == (int)EObjectType.Axe)
							{
								if (!add.Styles.Contains(NosdodenGhostAddBrain.tauntAxeWarrior))
									add.Styles.Add(NosdodenGhostAddBrain.tauntAxeWarrior);
							}
							if (righthand.Object_Type == (int)EObjectType.Hammer)
							{
								if (!add.Styles.Contains(NosdodenGhostAddBrain.tauntHammerWarrior))
									add.Styles.Add(NosdodenGhostAddBrain.tauntHammerWarrior);
							}
							if (righthand.Object_Type == (int)EObjectType.Sword)
							{
								if (!add.Styles.Contains(NosdodenGhostAddBrain.tauntSwordWarrior))
									add.Styles.Add(NosdodenGhostAddBrain.tauntSwordWarrior);
							}
						}
						#endregion
						#region Styles for Savage
						if (player.PlayerClass.ID == (int)EPlayerClass.Savage)
						{
							if (righthand.Object_Type == (int)EObjectType.HandToHand || lefthand.Object_Type == (int)EObjectType.HandToHand)
							{
								if (!add.Styles.Contains(NosdodenGhostAddBrain.tauntSavage))
									add.Styles.Add(NosdodenGhostAddBrain.tauntSavage);
								if (!add.Styles.Contains(NosdodenGhostAddBrain.BackSavage))
									add.Styles.Add(NosdodenGhostAddBrain.BackSavage);
							}
						}
						#endregion
					}
				}
                if (player.Inventory.GetItem(EInventorySlot.LeftHandWeapon) != null)
				{
					DbInventoryItem lefthand = player.Inventory.GetItem(EInventorySlot.LeftHandWeapon);
					if(lefthand != null)
						template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, lefthand.Model, lefthand.Color, lefthand.Effect);
				}
				if (player.Inventory.GetItem(EInventorySlot.TwoHandWeapon) != null)
                {
					DbInventoryItem twohand = player.Inventory.GetItem(EInventorySlot.TwoHandWeapon);
					DbInventoryItem righthand = player.Inventory.GetItem(EInventorySlot.RightHandWeapon);
					if (twohand != null)
					{
						template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, twohand.Model, twohand.Color, twohand.Effect);
						#region Styles for Savage 2h
						if (player.PlayerClass.ID == (int)EPlayerClass.Savage && righthand == null)
						{
							if (!add.Styles.Contains(NosdodenGhostAddBrain.Taunt2h))
								add.Styles.Add(NosdodenGhostAddBrain.Taunt2h);
						}
						#endregion
						#region Styles for Hunter Spear 2h
						if (player.PlayerClass.ID == (int)EPlayerClass.Hunter && twohand.Object_Type == (int)EObjectType.Spear)
						{
							if (!add.Styles.Contains(NosdodenGhostAddBrain.TauntSpearHunt))
								add.Styles.Add(NosdodenGhostAddBrain.TauntSpearHunt);
							if (!add.Styles.Contains(NosdodenGhostAddBrain.BackSpearHunt))
								add.Styles.Add(NosdodenGhostAddBrain.BackSpearHunt);
						}
						#endregion
					}
                }
                if (player.Inventory.GetItem(EInventorySlot.DistanceWeapon) != null)
				{
					DbInventoryItem distance = player.Inventory.GetItem(EInventorySlot.DistanceWeapon);
					if(distance != null)
						template.AddNPCEquipment(EInventorySlot.DistanceWeapon, distance.Model, distance.Color, distance.Effect);
				}						
				add.Inventory = template.CloseTemplate();
                #endregion
                #region Set mob visible slot
                DbInventoryItem mob_twohand = template.GetItem(EInventorySlot.TwoHandWeapon);
				DbInventoryItem mob_righthand = template.GetItem(EInventorySlot.RightHandWeapon);
				DbInventoryItem mob_lefthand = template.GetItem(EInventorySlot.LeftHandWeapon);
				DbInventoryItem mob_distance = template.GetItem(EInventorySlot.LeftHandWeapon);
				if (mob_lefthand != null && mob_righthand != null)
				{
					if ((mob_righthand.Object_Type == (int)EObjectType.Axe && mob_righthand.Item_Type == Slot.RIGHTHAND) /*axe*/
					|| (mob_righthand.Object_Type == (int)EObjectType.Sword && mob_righthand.Item_Type == Slot.RIGHTHAND) /*sword*/
					|| ((mob_righthand.Object_Type == (int)EObjectType.HandToHand || mob_lefthand.Object_Type == (int)EObjectType.HandToHand) && (mob_righthand.Item_Type == Slot.RIGHTHAND || mob_lefthand.Item_Type == Slot.LEFTHAND)) /*Hand-to-Hand*/
					|| (mob_righthand.Object_Type == (int)EObjectType.Hammer && mob_righthand.Item_Type == Slot.RIGHTHAND) /*hammer*/
					|| (mob_lefthand.Object_Type == (int)EObjectType.LeftAxe) /*left axe*/
					|| (mob_lefthand.Object_Type == (int)EObjectType.Shield)) /*shield*/
					{
						add.SwitchWeapon(EActiveWeaponSlot.Standard);
						add.VisibleActiveWeaponSlots = 16;
					}
				}
				if (mob_twohand != null)
				{
					if (((mob_twohand.Object_Type == (int)EObjectType.Hammer && mob_twohand.Item_Type == Slot.TWOHAND) /*axe2h*/
					|| (mob_twohand.Object_Type == (int)EObjectType.Sword && mob_twohand.Item_Type == Slot.TWOHAND) /*sword2h*/
					|| (mob_twohand.Object_Type == (int)EObjectType.Spear && mob_twohand.Item_Type == Slot.TWOHAND) /*spear*/
					|| (mob_twohand.Object_Type == (int)EObjectType.Staff && mob_twohand.Item_Type == Slot.TWOHAND))) /*Staff*/
					{
						add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
						add.VisibleActiveWeaponSlots = 34;
					}
				}
				if(mob_distance != null && mob_distance.Object_Type == (int)EObjectType.CompositeBow && mob_distance.Item_Type == Slot.RANGED) /*distance*/
                {
					add.SwitchWeapon(EActiveWeaponSlot.Distance);
					add.VisibleActiveWeaponSlots = 51;
				}
				#endregion
				add.PackageID = "NosdodenGhost" + player.PlayerClass.Name;
				add.AddToWorld();
				BroadcastMessage(String.Format("Life essense of " + enemy.Name + " has turned into spirit."));
			}
		}		
        base.EnemyKilled(enemy);
    }
}

#region Nosdoden Ghost add
public class NosdodenGhostAdd : GameNpc
{
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20; // dmg reduction for melee dmg
			case EDamageType.Crush: return 20; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
			default: return 20; // dmg reduction for rest resists
		}
	}
    public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
    public override void DealDamage(AttackData ad)
    {
		if(ad != null)
        {
			if(PackageID == "NosdodenGhostSpiritmaster")
				Health += ad.Damage / 2;
        }
        base.DealDamage(ad);
    }
    public override void OnAttackedByEnemy(AttackData ad)
    {       
        if (ad != null && ad.AttackResult == EAttackResult.Evaded)
        {
			#region Berserker
			if (PackageID == "NosdodenGhostBerserker")
            {
				styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.tauntBerserker;
				styleComponent.NextCombatStyle = NosdodenGhostAddBrain.AfterEvadeBerserker;
			}
			#endregion
			#region Shadowblade
			if (PackageID == "NosdodenGhostShadowblade")
			{
				styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.AnyTimerSB;
				styleComponent.NextCombatStyle = NosdodenGhostAddBrain.AfterEvadeBerserker;
			}
			#endregion
		}
		base.OnAttackedByEnemy(ad);
    }
    public override void StartAttack(GameObject target)
    {
		if (PackageID == "NosdodenGhostRunemaster" || PackageID == "NosdodenGhostSpiritmaster" || PackageID == "NosdodenGhostBonedancer"
			|| PackageID == "NosdodenGhostHealer" || PackageID == "NosdodenGhostShaman")
			return;
		base.StartAttack(target);
    }
    public override void OnAttackEnemy(AttackData ad)
    {
        #region Berserker
        if (PackageID == "NosdodenGhostBerserker")
		{
			if (ad != null && ad.AttackResult == EAttackResult.HitUnstyled)
			{
				styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.tauntBerserker;
				styleComponent.NextCombatStyle = NosdodenGhostAddBrain.AfterEvadeBerserker;
			}
			if (ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 198 && ad.Style.ClassID == 31)
			{
				styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.tauntBerserker;
				styleComponent.NextCombatStyle = NosdodenGhostAddBrain.EvadeFollowUpBerserker;
			}
		}
		#endregion
		#region Shadowblade
		if (PackageID == "NosdodenGhostShadowblade")
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
				CastSpell(SB_Lifebane, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (ad != null && ad.AttackResult == EAttackResult.HitUnstyled)
			{
				styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.AnyTimerSB;
				styleComponent.NextCombatStyle = NosdodenGhostAddBrain.AnyTimerFollowUpSB;
			}
			if (ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 198 && ad.Style.ClassID == 31)
			{
				styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.AnyTimerSB;
				styleComponent.NextCombatStyle = NosdodenGhostAddBrain.EvadeFollowUpBerserker;
			}
			if (ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 342 && ad.Style.ClassID == 23)
			{
				styleComponent.NextCombatBackupStyle = NosdodenGhostAddBrain.AnyTimerSB;
				styleComponent.NextCombatStyle = NosdodenGhostAddBrain.AnyTimerFollowUpSB;
			}
		}
		#endregion
		base.OnAttackEnemy(ad);
    }
    public override double GetArmorAF(EArmorSlot slot)
	{
		return 250;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.20;
	}
    public override void Die(GameObject killer)
    {
        #region Kill pet Hunter
        if (PackageID == "NosdodenGhostHunter")
		{
			foreach (GameNpc npc in GetNPCsInRadius(5000))
			{
				if (npc != null)
				{
					if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "GhostHunterPet"
						&& npc.Brain is StandardMobBrain brain && !brain.HasAggro)
						npc.Die(npc);
				}
			}
		}
		#endregion
		#region Kill pet Spiritmaster
		if (PackageID == "NosdodenGhostSpiritmaster")
		{
			foreach (GameNpc npc in GetNPCsInRadius(5000))
			{
				if (npc != null)
				{
					if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == Convert.ToString(ObjectID) && npc.Brain is GhostSpiritChampionBrain)
						npc.Die(npc);
				}
			}
		}
		#endregion
		#region Kill pet Bonedancer
		if (PackageID == "NosdodenGhostBonedancer")
		{
			foreach (GameNpc npc in GetNPCsInRadius(5000))
			{
				if (npc != null)
				{
					if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == Convert.ToString(ObjectID) && npc.Brain is GhostSkeletalCommanderBrain)
						npc.Die(npc);
				}
			}
		}
		#endregion
		base.Die(killer);
    }
    public override int MaxHealth
	{
		get { return 8000; }
	}
	public override bool AddToWorld()
	{
		RespawnInterval = -1;
		MaxSpeedBase = 225;
		Level = (byte)Util.Random(62, 66);
		Faction = FactionMgr.GetFactionByID(150);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
		NosdodenGhostAddBrain add = new NosdodenGhostAddBrain();
		SetOwnBrain(add);
		base.AddToWorld();
		return true;
	}
	#region Spells
	#region Spells Shadoblade
	private Spell m_SB_Lifebane;
	private Spell SB_Lifebane
	{
		get
		{
			if (m_SB_Lifebane == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.Duration = 20;
				spell.Frequency = 39;
				spell.ClientEffect = 4099;
				spell.Icon = 4099;
				spell.TooltipId = 4099;
				spell.Damage = 65;
				spell.DamageType = (int)EDamageType.Body;
				spell.Description = "Inflicts damage to the target repeatly over a given time period.";
				spell.Message1 = "You are afflicted with a vicious poison!";
				spell.Message2 = "{0} has been poisoned!";
				spell.Message3 = "The poison has run its course.";
				spell.Message4 = "{0} looks healthy again.";
				spell.Name = "Lifebane";
				spell.Range = 350;
				spell.SpellID = 11876;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DamageOverTime.ToString();
				m_SB_Lifebane = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SB_Lifebane);
			}
			return m_SB_Lifebane;
		}
	}
	#endregion
	#endregion
}
#endregion Nosdoden Ghost add

#region Spiritmaster pet
public class GhostSpiritChampion : GameNpc
{
	public override int MaxHealth
	{
		get { return 2500; }
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 15; // dmg reduction for melee dmg
			case EDamageType.Crush: return 15; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 15; // dmg reduction for melee dmg
			default: return 25; // dmg reduction for rest resists
		}
	}
    public override void OnAttackEnemy(AttackData ad)
    {
		if(ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
        {
			if(Util.Chance(25) && (!ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity) || !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun)) && ad.Target.IsAlive)
				CastSpell(SpiritChampion_stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}				
        base.OnAttackEnemy(ad);
    }
    public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override void ReturnToSpawnPoint(short speed)
	{
		return;
	}
	public override short Strength { get => base.Strength; set => base.Strength = 150; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	List<ushort> spirit_champion_models = new List<ushort>()
	{
		153,162,137,146,773,784,169,178,185,194
	};
	public override bool AddToWorld()
	{
		Name = "spirit champion";
		Model = spirit_champion_models[Util.Random(0, spirit_champion_models.Count - 1)];
		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TorsoArmor, 295, 0, 0, 0); //Slot,model,color,effect,extension
		template.AddNPCEquipment(EInventorySlot.ArmsArmor, 297, 0);
		template.AddNPCEquipment(EInventorySlot.LegsArmor, 296, 0);
		template.AddNPCEquipment(EInventorySlot.HandsArmor, 298, 0, 0, 0);
		template.AddNPCEquipment(EInventorySlot.FeetArmor, 299, 0, 0, 0);
		template.AddNPCEquipment(EInventorySlot.HeadArmor, 1216, 0, 0, 0);
		template.AddNPCEquipment(EInventorySlot.Cloak, 677, 0, 0, 0);
		template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 310, 0, 0, 0);
		template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 79, 0, 0, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.Standard);
		VisibleActiveWeaponSlots = 16;
		Size = 50;
		Level = 50;
		MaxSpeedBase = 225;
		BlockChance = 40;
		RespawnInterval = -1;
		Flags ^= ENpcFlags.GHOST;
		Realm = ERealm.None;
		GhostSpiritChampionBrain adds = new GhostSpiritChampionBrain();
		SetOwnBrain(adds);
		base.AddToWorld();
		return true;
	}
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	public override long ExperienceValue => 0;
	private Spell m_SpiritChampion_stun;
	private Spell SpiritChampion_stun
	{
		get
		{
			if (m_SpiritChampion_stun == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 2165;
				spell.Icon = 2132;
				spell.TooltipId = 2132;
				spell.Duration = 4;
				spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
				spell.Name = "Stun";
				spell.Range = 400;
				spell.SpellID = 11884;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Stun.ToString();
				m_SpiritChampion_stun = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SpiritChampion_stun);
			}
			return m_SpiritChampion_stun;
		}
	}
}
#endregion Spiritmaster pet

#region Skeletal Commander
public class GhostSkeletalCommander : GameNpc
{
	public override int MaxHealth
	{
		get { return 2500; }
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 15; // dmg reduction for melee dmg
			case EDamageType.Crush: return 15; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 15; // dmg reduction for melee dmg
			default: return 25; // dmg reduction for rest resists
		}
	}
    public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override void ReturnToSpawnPoint(short speed)
	{
		return;
	}
	public override short Strength { get => base.Strength; set => base.Strength = 150; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override bool AddToWorld()
	{
		Model = 2220;
		Flags = ENpcFlags.GHOST;
		EquipmentTemplateID = "bd_armor";
		Name = "bone commander";
		Size = 60;
		Level = 50;
		RespawnInterval = -1;
		Realm = ERealm.None;
		GhostSkeletalCommanderBrain adds = new GhostSkeletalCommanderBrain();
		SetOwnBrain(adds);
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc.IsAlive && npc.RespawnInterval == -1 && npc.Brain is SkeletalCommanderHealerBrain && npc.PackageID == PackageID)
				npc.Die(npc);
		}
		base.Die(killer);
    }
    public override void DropLoot(GameObject killer) //no loot
	{
	}
    public override long ExperienceValue => 0;
}
#endregion Skeletal Commander

#region Bonemender
public class SkeletalCommanderHealer : GameNpc
{
	public override int MaxHealth
	{
		get { return 1500; }
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 15; // dmg reduction for melee dmg
			case EDamageType.Crush: return 15; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 15; // dmg reduction for melee dmg
			default: return 25; // dmg reduction for rest resists
		}
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 200;
	}
	public override void ReturnToSpawnPoint(short speed)
	{
		return;
	}
	public override void StartAttack(GameObject target)
	{
		if (IsAlive)
			return;
		base.StartAttack(target);
	}	
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.15;
	}
	public override bool AddToWorld()
	{
		Model = 2220;
		Name = "bonemender";
		Size = 45;
		Level = 44;
		RespawnInterval = -1;
		Dexterity = 200;
		Flags ^= ENpcFlags.GHOST;
		Realm = ERealm.None;
		SkeletalCommanderHealerBrain adds = new SkeletalCommanderHealerBrain();
		SetOwnBrain(adds);
		base.AddToWorld();
		return true;
	}
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	public override long ExperienceValue => 0;
}
#endregion Bonemender

#region Nosdoden adds
public class NosdodenSummonedAdds : GameNpc
{
	public override int MaxHealth
	{
		get { return 2500; }
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 15; // dmg reduction for melee dmg
			case EDamageType.Crush: return 15; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 15; // dmg reduction for melee dmg
			default: return 25; // dmg reduction for rest resists
		}
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 200;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.15;
	}
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 80; }
    public override bool AddToWorld()
	{
		Model = 902;
		Name = "essence of fallen";
		Size = 50;
		Level = (byte)Util.Random(60,62);
		RespawnInterval = -1;
		Realm = ERealm.None;
		MaxSpeedBase = 250;
		Faction = FactionMgr.GetFactionByID(150);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
		NosdodenSummonedAddsBrain adds = new NosdodenSummonedAddsBrain();
		SetOwnBrain(adds);
		base.AddToWorld();
		return true;
	}
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	public override long ExperienceValue => 0;
}
#endregion Nosdoden adds