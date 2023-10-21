using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS;

#region Icelord Kvasir
public class IcelordKvasir : GameEpicBoss
{
    public IcelordKvasir() : base()
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
            if (damageType == EDamageType.Cold) //take no damage
            {
                this.Health += this.MaxHealth / 5; //heal himself if damage is cold
                BroadcastMessage(String.Format("Icelord Kvasir says, 'aahhhh thank you " + source.Name +" for healing me !'"));
                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
            else //take dmg
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
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
        get { return 100000; }
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override bool AddToWorld()
    {
        foreach (GameNpc npc in GetNPCsInRadius(8000))
        {
            if (npc.Brain is TunnelsBrain)
                npc.RemoveFromWorld();
        }
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162348);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

        IcelordKvasirBrain sbrain = new IcelordKvasirBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
    public override void Die(GameObject killer)
    {
        SpawnAnnouncer();
        if (killer is GamePlayer)
        {
            GamePlayer player = killer as GamePlayer;
            if(player != null)
                BroadcastMessage(String.Format("my kind will avenge me! You won't make out of here alive " + player.PlayerClass.Name+ "!"));
        }
        var prepareMezz = TempProperties.GetProperty<EcsGameTimer>("kvasir_prepareMezz");//cancel message
        if (prepareMezz != null)
        {
            prepareMezz.Stop();
            TempProperties.RemoveProperty("kvasir_prepareMezz");
        }
        base.Die(killer);
    }
    private void SpawnAnnouncer()
    {
        foreach (GameNpc npc in GetNPCsInRadius(8000))
        {
            if (npc.Brain is TunnelsBrain)
                return;
        }
        Tunnels announcer = new Tunnels();
        announcer.X = 21088;
        announcer.Y = 52022;
        announcer.Z = 10880;
        announcer.Heading = 1006;
        announcer.CurrentRegion = CurrentRegion;
        announcer.AddToWorld();
    }
}
#endregion Icelord Kvasir

#region Tunnels Announcer
public class Tunnels : GameNpc
{
    public Tunnels() : base()
    {
    }
    public override int MaxHealth
    {
        get { return 10000; }
    }
    public override bool AddToWorld()
    {
        Model = 665;
        Name = "Tunnels Announce";
        GuildName = "DO NOT REMOVE";
        RespawnInterval = 5000;
        Flags = (ENpcFlags)28;

        Size = 50;
        Level = 50;
        MaxSpeedBase = 0;
        TunnelsBrain.message1 = false;
        TunnelsBrain.message2 = false;

        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

        TunnelsBrain adds = new TunnelsBrain();
        SetOwnBrain(adds);
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }
}
#endregion Tunnels Announcer