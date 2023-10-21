using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS;

#region Balor
public class Balor : GameEpicBoss
{
	public Balor() : base() { }
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 30; // dmg reduction for melee dmg
			case EDamageType.Crush: return 30; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 30; // dmg reduction for melee dmg
			default: return 40; // dmg reduction for rest resists
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
		get { return 40000; }
	}
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			if (IsOutOfTetherRange)
			{
				if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
					|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
					|| damageType == EDamageType.Slash)
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GameSummonedPet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
			}
			else//take dmg
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
		}
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
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158225);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		BalorBrain.spawn_eye = false;
		Faction = FactionMgr.GetFactionByID(93);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));
		IsCloakHoodUp = true;

		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 841, 0, 0, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		VisibleActiveWeaponSlots = 34;
		BalorBrain sbrain = new BalorBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}
#endregion Balor

#region Balor's Eye
public class BalorEye : GameNpc
{
	public override int MaxHealth
	{
		get { return 5000; }
	}
    public override void StartAttack(GameObject target)
    {
    }
	public static int EyeCount = 0;
    public override void Die(GameObject killer)
    {
		--EyeCount;
        base.Die(killer);
    }
    public override bool AddToWorld()
	{
		Model = 665;
		Name = "Eye of Balor";
		Strength = 80;
		Dexterity = 200;
		Quickness = 100;
		Constitution = 100;
		RespawnInterval = -1;
		MaxSpeedBase = 0;
		Flags ^= ENpcFlags.FLYING;
		Flags ^= ENpcFlags.CANTTARGET;
		Flags ^= ENpcFlags.DONTSHOWNAME;
		//Flags ^= eFlags.STATUE;

		BalorEyeBrain.PickTarget = false;
		BalorEyeBrain.RandomTarget = null;
		BalorEyeBrain.Cancast = false;
		++EyeCount;
		Size = 20;
		Level = (byte)Util.Random(65, 70);
		Faction = FactionMgr.GetFactionByID(93);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));//minions of balor
		BalorEyeBrain eye = new BalorEyeBrain();
		SetOwnBrain(eye);
		eye.Start();
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(RemoveEye),18200); //mob will be removed after this time
		}
		return success;
	}
	protected int RemoveEye(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			Die(this);
		}
		return 0;
	}
}
#endregion Balor's Eye