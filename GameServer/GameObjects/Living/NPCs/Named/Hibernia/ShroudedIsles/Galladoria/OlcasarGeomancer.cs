using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS;

#region Olcasar Geomancer
public class OlcasarGeomancer : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public OlcasarGeomancer()
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
        return base.AttackDamage(weapon) * Strength / 100  * ServerProperty.EPICS_DMG_MULTIPLIER;
    }
    public override void OnAttackEnemy(AttackData ad)
    {
        if(ad != null)
        {
            if(Util.Chance(35))
                CastSpell(OGDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.OnAttackEnemy(ad);
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
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 350;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override void Die(GameObject killer)
    {
        foreach (GameNpc npc in GetNPCsInRadius(8000))
        {
            if (npc.Brain is OlcasarAddsBrain)
            {
                npc.RemoveFromWorld();
            }
        }
        base.Die(killer);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164613);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Charisma = npcTemplate.Charisma;
        Empathy = npcTemplate.Empathy;
        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 19, 0, 0, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);

        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;

        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        OlcasarGeomancerBrain sBrain = new OlcasarGeomancerBrain();
        SetOwnBrain(sBrain);
        SaveIntoDatabase();
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;

        npcs = WorldMgr.GetNPCsByNameFromRegion("Olcasar Geomancer", 191, (ERealm) 0);
        if (npcs.Length == 0)
        {
            log.Warn("Olcasar Geomancer not found, creating it...");

            log.Warn("Initializing Olcasar Geomancer...");
            OlcasarGeomancer OG = new OlcasarGeomancer();
            OG.Name = "Olcasar Geomancer";
            OG.Model = 925;
            OG.Realm = 0;
            OG.Level = 77;
            OG.Size = 170;
            OG.CurrentRegionID = 191; //galladoria

            OG.Strength = 500;
            OG.Intelligence = 220;
            OG.Piety = 220;
            OG.Dexterity = 200;
            OG.Constitution = 200;
            OG.Quickness = 125;
            OG.BodyType = 8; //magician
            OG.MeleeDamageType = EDamageType.Slash;
            OG.Faction = FactionMgr.GetFactionByID(96);
            OG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

            OG.X = 39152;
            OG.Y = 36878;
            OG.Z = 14975;
            OG.MaxDistance = 2000;
            OG.MaxSpeedBase = 300;
            OG.Heading = 2033;

            OlcasarGeomancerBrain ubrain = new OlcasarGeomancerBrain();
            ubrain.AggroLevel = 100;
            ubrain.AggroRange = 500;
            OG.SetOwnBrain(ubrain);
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164613);
            OG.LoadTemplate(npcTemplate);
            OG.AddToWorld();
            OG.Brain.Start();
            OG.SaveIntoDatabase();
        }
        else
            log.Warn(
                "Olcasar Geomancer exist ingame, remove it and restart server if you want to add by script code.");
    }
    private Spell m_OGDD;
    private Spell OGDD
    {
        get
        {
            if (m_OGDD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 3;
                spell.ClientEffect = 5089;
                spell.Icon = 5089;
                spell.Name = "Geomancer Strike";
                spell.TooltipId = 5089;
                spell.Range = 500;
                spell.Damage = 350;
                spell.SpellID = 11860;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Matter;
                m_OGDD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGDD);
            }
            return m_OGDD;
        }
    }
}
#endregion Olcasar Geomancer

#region Olcasar Geomancer adds
public class OlcasarAdds : GameNpc
{
    public OlcasarAdds() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 35; // dmg reduction for melee dmg
            case EDamageType.Crush: return 35; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 35; // dmg reduction for melee dmg
            default: return 35; // dmg reduction for rest resists
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
    public override int MaxHealth
    {
        get { return 10000; }
    }

    public override void DropLoot(GameObject killer) //no loot
    {
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override void Die(GameObject killer)
    {
        BroadcastMessage(String.Format("As Olcasar minion falls to the ground, he begins to mutter some strange words and his slain minion rises back from the dead."));
        OlcasarAdds Add = new OlcasarAdds();
        Add.X = killer.X + Util.Random(-50, 80);
        Add.Y = killer.Y + Util.Random(-50, 80);
        Add.Z = killer.Z;
        Add.CurrentRegion = killer.CurrentRegion;
        Add.Heading = killer.Heading;
        Add.AddToWorld();
        base.Die(null); // null to not gain experience
    }
    public override short Strength { get => base.Strength; set => base.Strength = 300; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; } 
    public override bool AddToWorld()
    {
        foreach (GamePlayer ppl in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            if (ppl != null)
            {
                foreach (GameNpc boss in GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (boss != null)
                    {
                        if (boss.IsAlive && boss.Brain is OlcasarGeomancerBrain)
                            ppl.Out.SendSpellEffectAnimation(this, boss, 5126, 0, false, 0x01);
                    }
                }
            }
        }
        Model = 925;
        Name = "geomancer minion";
        RespawnInterval = -1;
        MaxDistance = 0;
        TetherRange = 0;
        Size = (byte) Util.Random(45, 55);
        Level = (byte) Util.Random(62, 66);
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        OlcasarAddsBrain adds = new OlcasarAddsBrain();
        LoadedFromScript = true;
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Olcasar Geomancer adds