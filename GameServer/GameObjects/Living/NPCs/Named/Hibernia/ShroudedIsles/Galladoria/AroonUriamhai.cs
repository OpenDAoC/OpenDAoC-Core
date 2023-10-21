using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS;

#region Aroon the Uriamhai
public class AroonUriamhai : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public AroonUriamhai()
        : base()
    {
    }

    public static bool Aroon_slash = false;
    public static bool Aroon_crush = false;
    public static bool Aroon_thrust = false;
    public static bool Aroon_body = false;
    public static bool Aroon_cold = false;
    public static bool Aroon_energy = false;
    public static bool Aroon_heat = false;
    public static bool Aroon_matter = false;
    public static bool Aroon_spirit = false;

    #region Aroon resist damage checks
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        Point3D spawn = new Point3D(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if ((Aroon_slash == false && Aroon_thrust == false && Aroon_crush == false && Aroon_body == false &&
                Aroon_cold == false && Aroon_energy == false && Aroon_heat == false
                && Aroon_matter == false && Aroon_spirit == false) || !source.IsWithinRadius(spawn, TetherRange))
            {
                if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
                    damageType == EDamageType.Energy || damageType == EDamageType.Heat
                    || damageType == EDamageType.Matter || damageType == EDamageType.Spirit ||
                    damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                    || damageType == EDamageType.Slash)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }

            if ((Aroon_slash == true && Aroon_thrust == false && Aroon_crush == false && Aroon_body == false &&
                Aroon_cold == false && Aroon_energy == false && Aroon_heat == false
                && Aroon_matter == false && Aroon_spirit == false) || !source.IsWithinRadius(spawn, TetherRange))
            {
                if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
                    damageType == EDamageType.Energy || damageType == EDamageType.Heat
                    || damageType == EDamageType.Matter || damageType == EDamageType.Spirit ||
                    damageType == EDamageType.Crush || damageType == EDamageType.Thrust)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }

            if ((Aroon_slash == true && Aroon_thrust == true && Aroon_crush == false && Aroon_body == false &&
                Aroon_cold == false && Aroon_energy == false && Aroon_heat == false
                && Aroon_matter == false && Aroon_spirit == false) || !source.IsWithinRadius(spawn, TetherRange))
            {
                if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
                    damageType == EDamageType.Energy || damageType == EDamageType.Heat
                    || damageType == EDamageType.Matter || damageType == EDamageType.Spirit ||
                    damageType == EDamageType.Crush)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }

            if ((Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == false &&
                Aroon_cold == false && Aroon_energy == false && Aroon_heat == false
                && Aroon_matter == false && Aroon_spirit == false) || !source.IsWithinRadius(spawn, TetherRange))
            {
                if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
                    damageType == EDamageType.Energy || damageType == EDamageType.Heat
                    || damageType == EDamageType.Matter || damageType == EDamageType.Spirit)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }

            if ((Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true &&
                Aroon_cold == false && Aroon_energy == false && Aroon_heat == false
                && Aroon_matter == false && Aroon_spirit == false) || !source.IsWithinRadius(spawn, TetherRange))
            {
                if (damageType == EDamageType.Cold || damageType == EDamageType.Energy ||
                    damageType == EDamageType.Heat
                    || damageType == EDamageType.Matter || damageType == EDamageType.Spirit)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }

            if ((Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true &&
                Aroon_cold == true && Aroon_energy == false && Aroon_heat == false
                && Aroon_matter == false && Aroon_spirit == false) || !source.IsWithinRadius(spawn, TetherRange))
            {
                if (damageType == EDamageType.Energy || damageType == EDamageType.Heat
                || damageType == EDamageType.Matter || damageType == EDamageType.Spirit)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }

            if ((Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true &&
                Aroon_cold == true && Aroon_energy == true && Aroon_heat == false
                && Aroon_matter == false && Aroon_spirit == false) || !source.IsWithinRadius(spawn, TetherRange))
            {
                if (damageType == EDamageType.Heat || damageType == EDamageType.Matter ||
                    damageType == EDamageType.Spirit)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }

            if ((Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true &&
                Aroon_cold == true && Aroon_energy == true && Aroon_heat == true
                && Aroon_matter == false && Aroon_spirit == false) || !source.IsWithinRadius(spawn, TetherRange))
            {
                if (damageType == EDamageType.Matter || damageType == EDamageType.Spirit)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }

            if ((Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true &&
                Aroon_cold == true && Aroon_energy == true && Aroon_heat == true
                && Aroon_matter == true && Aroon_spirit == false) || !source.IsWithinRadius(spawn, TetherRange))
            {
                if (damageType == EDamageType.Spirit)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }

            if ((Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true &&
                Aroon_cold == true && Aroon_energy == true && Aroon_heat == true
                && Aroon_matter == true && Aroon_spirit == true) || !source.IsWithinRadius(spawn, TetherRange))
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
    }
    #endregion Aroon resist damage checks

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }

    public override int MaxHealth
    {
        get { return 300000; }
    }

    public override int AttackRange
    {
        get { return 450; }
        set { }
    }
   /* public override int GetResist(eDamageType damageType)
    {
        switch (damageType)
        {
            case eDamageType.Slash: return 40;// dmg reduction for melee dmg
            case eDamageType.Crush: return 40;// dmg reduction for melee dmg
            case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
            default: return 70;// dmg reduction for rest resists
        }
    }*/
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

    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158075);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Charisma = npcTemplate.Charisma;
        Empathy = npcTemplate.Empathy;
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

        Aroon_slash = false;
        Aroon_thrust = false;
        Aroon_crush = false;
        Aroon_body = false;
        Aroon_cold = false;
        Aroon_energy = false;
        Aroon_heat = false;
        Aroon_matter = false;
        Aroon_spirit = false;

        CorpScaithBrain.switch_target = false;
        SpioradScaithBrain.switch_target = false;
        RopadhScaithBrain.switch_target = false;
        DamhnaScaithBrain.switch_target = false;
        FuinneamgScaithBrain.switch_target = false;
        BruScaithBrain.switch_target = false;
        FuarScaithBrain.switch_target = false;
        TaesScaithBrain.switch_target = false;
        ScorScaithBrain.switch_target = false;

        AroonUriamhaiBrain sBrain = new AroonUriamhaiBrain();
        SetOwnBrain(sBrain);
        AroonUriamhaiBrain.spawn_guardians = false;
        return base.AddToWorld();
    }


    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;

        npcs = WorldMgr.GetNPCsByNameFromRegion("Aroon the Urlamhai", 191, (ERealm) 0);
        if (npcs.Length == 0)
        {
            log.Warn("Aroon not found, creating it...");

            log.Warn("Initializing Aroon the Urlamhai...");
            AroonUriamhai CO = new AroonUriamhai();
            CO.Name = "Aroon the Urlamhai";
            CO.Model = 767;
            CO.Realm = 0;
            CO.Level = 81;
            CO.Size = 175;
            CO.CurrentRegionID = 191; //galladoria

            CO.Strength = 500;
            CO.Intelligence = 220;
            CO.Piety = 220;
            CO.Dexterity = 200;
            CO.Constitution = 200;
            CO.Quickness = 125;
            CO.BodyType = 5;
            CO.MeleeDamageType = EDamageType.Slash;
            CO.Faction = FactionMgr.GetFactionByID(96);
            CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

            CO.X = 51478;
            CO.Y = 43359;
            CO.Z = 10369;
            CO.MaxDistance = 2000;
            CO.TetherRange = 2500;
            CO.MaxSpeedBase = 250;
            CO.Heading = 11;

            AroonUriamhaiBrain ubrain = new AroonUriamhaiBrain();
            ubrain.AggroLevel = 100;
            ubrain.AggroRange = 600;
            CO.SetOwnBrain(ubrain);
            CO.AddToWorld();
            CO.Brain.Start();
            CO.SaveIntoDatabase();
        }
        else
            log.Warn(
                "Aroon the Urlamhai exist ingame, remove it and restart server if you want to add by script code.");
    }
}
#endregion Aroon the Uriamhai

#region Slash Guardian (Corp Scaith)
public class CorpScaith : GameNpc
{
    public CorpScaith() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
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

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }

    public override int MaxHealth
    {
        get { return 30000; }
    }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override void DropLoot(GameObject killer) //no loot
    {
    }

    public override void Die(GameObject killer) //slash resist
    {
        AroonUriamhai.Aroon_slash = true;
        base.Die(null); // null to not gain experience
    }

    public override bool AddToWorld()
    {
        CorpScaithBrain.Message1 = false;
        Model = (ushort) Util.Random(889, 890);
        Name = "Corp Scaith";
        RespawnInterval = -1;
        Strength = 350;
        Dexterity = 200;
        Quickness = 125;
        MaxDistance = 2500;
        TetherRange = 3000;
        Size = 155;
        Level = 77;
        MaxSpeedBase = 220;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        CorpScaithBrain adds = new CorpScaithBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Slash Guardian (Corp Scaith)

#region Thrust Guardian (Spiorad Scaith)
public class SpioradScaith : GameNpc //thrust resist
{
    public SpioradScaith() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }

    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }

    public override int MaxHealth
    {
        get { return 30000; }
    }

    public override void DropLoot(GameObject killer) //no loot
    {
    }

    public override void Die(GameObject killer)
    {
        AroonUriamhai.Aroon_thrust = true;
        base.Die(null); // null to not gain experience
    }

    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (AroonUriamhai.Aroon_slash)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
            else
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                        EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
    }

    public override bool AddToWorld()
    {
        SpioradScaithBrain.Message2 = false;
        Model = (ushort) Util.Random(889, 890);
        Name = "Spiorad Scaith";
        RespawnInterval = -1;
        Strength = 350;
        Dexterity = 200;
        Quickness = 125;
        MaxDistance = 2500;
        TetherRange = 3000;
        Size = 155;
        Level = 77;
        MaxSpeedBase = 220;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        SpioradScaithBrain adds = new SpioradScaithBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Thrust Guardian (Spiorad Scaith)

#region Crush Guardian (Ropadh Scaith)
public class RopadhScaith : GameNpc //crush resist
{
    public RopadhScaith() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }

    public override int MaxHealth
    {
        get { return 30000; }
    }

    public override void DropLoot(GameObject killer) //no loot
    {
    }

    public override void Die(GameObject killer)
    {
        AroonUriamhai.Aroon_crush = true;
        base.Die(null); // null to not gain experience
    }

    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (AroonUriamhai.Aroon_slash && AroonUriamhai.Aroon_thrust)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
            else
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                        EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
    }

    public override bool AddToWorld()
    {
        RopadhScaithBrain.Message3 = false;
        Model = (ushort) Util.Random(889, 890);
        Name = "Ropadh Scaith";
        RespawnInterval = -1;
        Strength = 350;
        Dexterity = 200;
        Quickness = 125;
        MaxDistance = 2500;
        TetherRange = 3000;
        Size = 155;
        Level = 77;
        MaxSpeedBase = 220;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        RopadhScaithBrain adds = new RopadhScaithBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Crush Guardian (Ropadh Scaith)

#region Body Guardian (Damhna Scaith)
public class DamhnaScaith : GameNpc //Body resist
{
    public DamhnaScaith() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }

    public override int MaxHealth
    {
        get { return 30000; }
    }

    public override void DropLoot(GameObject killer) //no loot
    {
    }

    public override void Die(GameObject killer)
    {
        AroonUriamhai.Aroon_body = true;
        base.Die(null); // null to not gain experience
    }

    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (AroonUriamhai.Aroon_slash && AroonUriamhai.Aroon_thrust && AroonUriamhai.Aroon_crush)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
            else
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                        EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
    }

    public override bool AddToWorld()
    {
        DamhnaScaithBrain.Message4 = false;
        Model = (ushort) Util.Random(889, 890);
        Name = "Damhna Scaith";
        RespawnInterval = -1;
        Strength = 350;
        Dexterity = 200;
        Quickness = 125;
        MaxDistance = 2500;
        TetherRange = 3000;
        Size = 155;
        Level = 77;
        MaxSpeedBase = 220;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        DamhnaScaithBrain adds = new DamhnaScaithBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Body Guardian (Damhna Scaith)

#region Cold Guardian (Fuinneamg Scaith)
public class FuinneamgScaith : GameNpc //Cold resist
{
    public FuinneamgScaith() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }

    public override int MaxHealth
    {
        get { return 30000; }
    }

    public override void DropLoot(GameObject killer) //no loot
    {
    }

    public override void Die(GameObject killer)
    {
        AroonUriamhai.Aroon_cold = true;
        base.Die(null); // null to not gain experience
    }

    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (AroonUriamhai.Aroon_slash && AroonUriamhai.Aroon_thrust && AroonUriamhai.Aroon_crush &&
                AroonUriamhai.Aroon_body)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
            else
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                        EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
    }

    public override bool AddToWorld()
    {
        FuinneamgScaithBrain.Message5 = false;
        Model = (ushort) Util.Random(889, 890);
        Name = "Fuinneamg Scaith";
        Strength = 350;
        Dexterity = 200;
        Quickness = 125;
        RespawnInterval = -1;
        MaxDistance = 2500;
        TetherRange = 3000;
        Size = 155;
        Level = 77;
        MaxSpeedBase = 220;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        FuinneamgScaithBrain adds = new FuinneamgScaithBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Cold Guardian (Fuinneamg Scaith)

#region Energy Guardian (Bru Scaith)
public class BruScaith : GameNpc //Energy resist
{
    public BruScaith() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }

    public override int MaxHealth
    {
        get { return 30000; }
    }

    public override void DropLoot(GameObject killer) //no loot
    {
    }

    public override void Die(GameObject killer)
    {
        AroonUriamhai.Aroon_energy = true;
        base.Die(null); // null to not gain experience
    }

    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (AroonUriamhai.Aroon_slash && AroonUriamhai.Aroon_thrust && AroonUriamhai.Aroon_crush &&
                AroonUriamhai.Aroon_body && AroonUriamhai.Aroon_cold)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
            else
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                        EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
    }

    public override bool AddToWorld()
    {
        BruScaithBrain.Message6 = false;
        Model = (ushort) Util.Random(889, 890);
        Name = "Bru Scaith";
        RespawnInterval = -1;
        Strength = 350;
        Dexterity = 200;
        Quickness = 125;
        MaxDistance = 2500;
        TetherRange = 3000;
        Size = 155;
        Level = 77;
        MaxSpeedBase = 220;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        BruScaithBrain adds = new BruScaithBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Energy Guardian (Bru Scaith)

#region Heat Guardian (Fuar Scaith)
public class FuarScaith : GameNpc //Heat resist
{
    public FuarScaith() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }

    public override int MaxHealth
    {
        get { return 30000; }
    }

    public override void DropLoot(GameObject killer) //no loot
    {
    }

    public override void Die(GameObject killer)
    {
        AroonUriamhai.Aroon_heat = true;
        base.Die(null); // null to not gain experience
    }

    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (AroonUriamhai.Aroon_slash && AroonUriamhai.Aroon_thrust && AroonUriamhai.Aroon_crush &&
                AroonUriamhai.Aroon_body && AroonUriamhai.Aroon_cold && AroonUriamhai.Aroon_energy)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
            else
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                        EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
    }

    public override bool AddToWorld()
    {
        FuarScaithBrain.Message7 = false;
        Model = (ushort) Util.Random(889, 890);
        Name = "Fuar Scaith";
        RespawnInterval = -1;
        Strength = 350;
        Dexterity = 200;
        Quickness = 125;
        MaxDistance = 2500;
        TetherRange = 3000;
        Size = 155;
        Level = 77;
        MaxSpeedBase = 220;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        FuarScaithBrain adds = new FuarScaithBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Heat Guardian (Fuar Scaith)

#region Matter Guardian (Taes Scaith)
public class TaesScaith : GameNpc //Matter resist
{
    public TaesScaith() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }

    public override int MaxHealth
    {
        get { return 30000; }
    }

    public override void DropLoot(GameObject killer) //no loot
    {
    }

    public override void Die(GameObject killer)
    {
        AroonUriamhai.Aroon_matter = true;
        base.Die(null); // null to not gain experience
    }

    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (AroonUriamhai.Aroon_slash && AroonUriamhai.Aroon_thrust && AroonUriamhai.Aroon_crush &&
                AroonUriamhai.Aroon_body && AroonUriamhai.Aroon_cold && AroonUriamhai.Aroon_energy
                && AroonUriamhai.Aroon_heat)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
            else
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                        EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
    }

    public override bool AddToWorld()
    {
        TaesScaithBrain.Message8 = false;
        Model = (ushort) Util.Random(889, 890);
        Name = "Taes Scaith";
        RespawnInterval = -1;
        Strength = 350;
        Dexterity = 200;
        Quickness = 125;
        MaxDistance = 2500;
        TetherRange = 3000;
        Size = 155;
        Level = 77;
        MaxSpeedBase = 220;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        TaesScaithBrain adds = new TaesScaithBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Matter Guardian (Taes Scaith)

#region Spirit Guardian (Scor Scaith)
public class ScorScaith : GameNpc //Spirit resist
{
    public ScorScaith() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }

    public override int MaxHealth
    {
        get { return 30000; }
    }

    public override void DropLoot(GameObject killer) //no loot
    {
    }

    public override void Die(GameObject killer)
    {
        AroonUriamhai.Aroon_spirit = true;
        base.Die(null); // null to not gain experience
    }

    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (AroonUriamhai.Aroon_slash && AroonUriamhai.Aroon_thrust && AroonUriamhai.Aroon_crush &&
                AroonUriamhai.Aroon_body && AroonUriamhai.Aroon_cold && AroonUriamhai.Aroon_energy
                && AroonUriamhai.Aroon_heat && AroonUriamhai.Aroon_matter)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
            else
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to this damage!", EChatType.CT_System,
                        EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
    }

    public override bool AddToWorld()
    {
        ScorScaithBrain.Message9 = false;
        Model = (ushort) Util.Random(889, 890);
        Name = "Scor Scaith";
        RespawnInterval = -1;
        Strength = 350;
        Dexterity = 200;
        Quickness = 125;
        MaxDistance = 2500;
        TetherRange = 3000;
        Size = 155;
        Level = 77;
        MaxSpeedBase = 220;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        ScorScaithBrain adds = new ScorScaithBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Spirit Guardian (Scor Scaith)