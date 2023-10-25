using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS;

public class Easmarach : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public Easmarach()
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
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            Point3D spawn = new Point3D(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);
            if (!source.IsWithinRadius(spawn, TetherRange))//dont take any dmg 
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
                        truc.Out.SendMessage(this.Name + " is too far away from waterfall and is immune to your damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
    public override void ReturnToSpawnPoint(short speed)
    {
        speed = 300;
        base.ReturnToSpawnPoint(speed);
    }
    public override void Die(GameObject killer)
    {
        foreach(GamePlayer player in GetPlayersInRadius(10000))
        {
            if(player != null)
            {
                player.Out.SendMessage("With the death of the Easmarach, the current of the falls reduces significantly.", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
            }
        }
        base.Die(killer);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160317);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Charisma = npcTemplate.Charisma;
        Empathy = npcTemplate.Empathy;

        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        EasmarachBrain.restphase = false;
        EasmarachBrain.dontattack = false;

        EasmarachBrain sBrain = new EasmarachBrain();
        SetOwnBrain(sBrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }     
}