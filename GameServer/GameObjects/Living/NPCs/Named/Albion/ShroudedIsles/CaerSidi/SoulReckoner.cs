using System;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS;

#region Soul Reckoner
public class SoulReckoner : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SoulReckoner()
        : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 40;// dmg reduction for melee dmg
            case EDamageType.Crush: return 40;// dmg reduction for melee dmg
            case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
            default: return 70;// dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperty.EPICS_DMG_MULTIPLIER;
    }

    public override int MaxHealth
    {
        get { return 100000; }
    }

    public override int AttackRange
    {
        get { return 450; }
        set { }
    }

    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
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

    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166369);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;

        MeleeDamageType = EDamageType.Spirit;
        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        ReckonedSoul.SoulCount = 0;
        SoulReckonerBrain adds = new SoulReckonerBrain();
        SetOwnBrain(adds);
        if (CurrentRegionID == 60)
        {
            if (spawn_souls == false)
            {
                SpawnSouls();
                spawn_souls = true;
            }
        }
        else
        {
            if (spawn_souls == false)
            {
                SpawnSouls();
                spawn_souls = true;
            }
        }
        SaveIntoDatabase();
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }

    public static bool spawn_souls = false;

    public void SpawnSouls()
    {
        for (int i = 0; i < Util.Random(4, 6); i++) // Spawn 4-6 souls
        {
            ReckonedSoul Add = new ReckonedSoul();
            Add.X = X + Util.Random(-50, 80);
            Add.Y = Y + Util.Random(-50, 80);
            Add.Z = Z;
            Add.CurrentRegion = CurrentRegion;
            Add.Heading = Heading;
            Add.AddToWorld();
        }
    }

    public override void Die(GameObject killer) //on kill generate orbs
    {
        foreach (GameNpc npc in GetNPCsInRadius(8000))
        {
            if (npc != null && npc.IsAlive)
            {
                if (npc.Brain is ReckonedSoulBrain)
                    npc.RemoveFromWorld();
            }
        }
        spawn_souls = false;
        base.Die(killer);
    }

    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }

    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (ReckonedSoul.SoulCount > 0 || SoulReckonerBrain.InRoom == false) //take no damage
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " brushes off your attack!", EChatType.CT_System,EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
            else //take dmg
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage("The " + Name + " flickers briefly", EChatType.CT_System, EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
    }
    public override void DealDamage(AttackData ad)
    {
        if(ad != null && ad.DamageType == EDamageType.Body)
            Health += ad.Damage;
        base.DealDamage(ad);
    }
    public override void EnemyKilled(GameLiving enemy)
    {
        Health += MaxHealth / 5; //heals if boss kill enemy
        base.EnemyKilled(enemy);
    }
}
#endregion Soul Reckoner

#region Reckoned Soul
public class ReckonedSoul : GameNpc
{
    public ReckonedSoul() : base()
    {
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

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 150;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.15;
    }

    public override int MaxHealth
    {
        get { return 20000; }
    }

    public override void Die(GameObject killer)
    {
        --SoulCount;
        base.Die(killer);
    }
    public override void DropLoot(GameObject killer)
    {
    }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 150; }
    public static int SoulCount = 0;

    public override bool AddToWorld()
    {
        Model = 909;
        MeleeDamageType = EDamageType.Spirit;
        Name = "reckoned soul";
        PackageID = "SoulReckonerBaf";
        RespawnInterval = -1;

        MaxDistance = 2500;
        TetherRange = 3000;
        RoamingRange = 120;
        Size = 100;
        Level = 75;
        MaxSpeedBase = 230;
        Flags = ENpcFlags.GHOST;           

        Faction = FactionMgr.GetFactionByID(64);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
        BodyType = 6;
        Realm = ERealm.None;
        ++SoulCount;

        ReckonedSoulBrain adds = new ReckonedSoulBrain();
        SetOwnBrain(adds);
        LoadedFromScript = true;
        base.AddToWorld();
        return true;
    }
}
#endregion Reckoned Soul