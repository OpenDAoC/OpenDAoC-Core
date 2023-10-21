using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Styles;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;

namespace Core.GS;

#region Green Knight
public class GreenKnight : GameEpicBoss
{
    public GreenKnight() : base()
    {
    }
    public static int TauntID = 103;
    public static int TauntClassID = 2; //armsman
    public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);
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
        return base.AttackDamage(weapon) * Strength / 100; //more str more dmg will he deal, modify ingame for easier adjust
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
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161621);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        Faction = FactionMgr.GetFactionByID(236); // fellwoods
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(236));
        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 46, 0, 0, 0); //Slot,model,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 48, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 47, 0);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 49, 32, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 50, 32, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 57, 32, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 7, 32, 0, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
        Styles.Add(taunt);
        MaxSpeedBase = 400;

        GreenKnightBrain.walk1 = false;
        GreenKnightBrain.walk2 = false;
        GreenKnightBrain.walk3 = false;
        GreenKnightBrain.walk4 = false;
        GreenKnightBrain.walk5 = false;
        GreenKnightBrain.Pick_healer = false;
        GreenKnightBrain.walk6 = false;
        GreenKnightBrain.IsSpawningTrees = false;
        GreenKnightBrain.walk7 = false;
        GreenKnightBrain.IsWalking = false;
        GreenKnightBrain.walk8 = false;
        GreenKnightBrain.walk9 = false;
        GreenKnightBrain.CanHeal1 = false;
        GreenKnightBrain.CanHeal2 = false;
        GreenKnightBrain.CanHeal3 = false;
        GreenKnightBrain.CanHeal4 = false;
        GreenKnightBrain.PickPortPoint = false;

        Flags = ENpcFlags.PEACE;
        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Slash;
        GreenKnightBrain sbrain = new GreenKnightBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Green Knight", 1, (ERealm) 0);
        if (npcs.Length == 0)
        {
            log.Warn("Green Knight not found, creating it...");

            log.Warn("Initializing Green Knight ...");
            GreenKnight OF = new GreenKnight();
            OF.Name = "Green Knight";
            OF.Model = 334;
            OF.Realm = 0;
            OF.Level = 79;
            OF.Size = 120;
            OF.CurrentRegionID = 1; //albion Forest sauvage
            OF.MeleeDamageType = EDamageType.Slash;
            OF.RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            OF.Faction = FactionMgr.GetFactionByID(236);
            OF.Faction.AddFriendFaction(FactionMgr.GetFactionByID(236));
            OF.BodyType = (ushort) EBodyType.Humanoid;
            OF.MaxSpeedBase = 400;

            OF.X = 592990;
            OF.Y = 418687;
            OF.Z = 5012;
            OF.Heading = 3331;
            GreenKnightBrain ubrain = new GreenKnightBrain();
            OF.SetOwnBrain(ubrain);
            OF.AddToWorld();
            OF.SaveIntoDatabase();
            OF.Brain.Start();
        }
        else
            log.Warn("Green Knight exist ingame, remove it and restart server if you want to add by script code.");
    }
    //This function is the callback function that is called when
    //a player right clicks on the npc
    public override bool Interact(GamePlayer player)
    {
        if (!base.Interact(player))
            return false;
        //Now we turn the npc into the direction of the person it is
        //speaking to.
        TurnTo(player.X, player.Y);
        this.Emote(EEmote.Salute);
        //We send a message to player and make it appear in a popup
        //window. Text inside the [brackets] is clickable in popup
        //windows and will generate a /whis text command!
        player.Out.SendMessage(
            "You are wise to speak with me " + player.PlayerClass.Name +
            "! My forest is a delicate beast that can easily turn against you. " +
            "Should you wake the beast within, I must then rise to [defend it].",
            EChatType.CT_System, EChatLoc.CL_PopupWindow);
        return true;
    }

    //This function is the callback function that is called when
    //someone whispers something to this mob!
    public override bool WhisperReceive(GameLiving source, string str)
    {
        if (!base.WhisperReceive(source, str))
            return false;

        //If the source is no player, we return false
        if (!(source is GamePlayer))
            return false;

        //We cast our source to a GamePlayer object
        GamePlayer t = (GamePlayer) source;

        //Now we turn the npc into the direction of the person it is
        //speaking to.
        TurnTo(t.X, t.Y);

        //We test what the player whispered to the npc and
        //send a reply. The Method SendReply used here is
        //defined later in this class ... read on
        switch (str)
        {
            case "defend it":
            {
                SendReply(t,
                    "Caution will be your guide through the dark places of Sauvage. " +
                    "Tread lightly " + t.PlayerClass.Name + "! I am ever watchful of my home!");
                if (t.IsAlive && t.IsAttackable)
                {
                    Flags = 0;
                    StartAttack(t);
                }
            }
                break;
            case "defend":
            {
                SendReply(t,
                    "Caution will be your guide through the dark places of Sauvage. " +
                    "Tread lightly " + t.PlayerClass.Name + "! I am ever watchful of my home!");
                if (t.IsAlive && t.IsAttackable)
                {
                    Flags = 0;
                    StartAttack(t);
                }
            }
                break;
            default:
                break;
        }
        return true;
    }
    public override void OnAttackEnemy(AttackData ad)
    {
        // 30% chance to proc heat dd
        if (Util.Chance(30))
        {
            //Here boss cast very X s aoe heat dmg, we can adjust it in spellrecast delay
            CastSpell(GreenKnightHeatDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.OnAttackEnemy(ad);
    }
    //This function sends some text to a player and makes it appear
    //in a popup window. We just define it here so we can use it in
    //the WhisperToMe function instead of writing the long text
    //everytime we want to send some reply!
    public void SendReply(GamePlayer target, string msg)
    {
        target.Out.SendMessage(msg,EChatType.CT_System, EChatLoc.CL_PopupWindow);
    }
    #region Heat DD Spell
    private Spell m_HeatDDSpell;
    /// <summary>
    /// Casts Heat dd
    /// </summary>
    public Spell GreenKnightHeatDD
    {
        get
        {
            if (m_HeatDDSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.Power = 0;
                spell.RecastDelay = 2;
                spell.ClientEffect = 360;
                spell.Icon = 360;
                spell.Damage = 250;
                spell.DamageType = (int) EDamageType.Heat;
                spell.Name = "Might of the Forrest";
                spell.Range = 0;
                spell.Radius = 350;
                spell.SpellID = 11755;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Radius = 500;
                spell.EffectGroup = 0;
                m_HeatDDSpell = new Spell(spell, 50);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_HeatDDSpell);
            }
            return m_HeatDDSpell;
        }
    }
    #endregion
}
#endregion Green Knight

#region Green Knight Trees
    public class GreenKnightTree : GameNpc
    {
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int MaxHealth
        {
            //trees got low hp, because they spawn preaty often. Modify here to adjust hp
            get { return 800; }
        }
        public override short Strength { get => base.Strength; set => base.Strength = 150; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override void DropLoot(GameObject killer) //no loot
        {
        }
        public override long ExperienceValue => 0;
        public override bool AddToWorld()
        {
            Model = 97;
            RoamingRange = 250;
            RespawnInterval = -1;
            Size = (byte) Util.Random(90, 135);
            Level = (byte) Util.Random(47, 49); // Trees level
            Name = "rotting downy felwood";
            Faction = FactionMgr.GetFactionByID(236); // fellwoods
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(236));
            PackageID = "GreenKnightAdd";
            MaxSpeedBase = 225;
            GreenKnightTreeBrain treesbrain = new GreenKnightTreeBrain();
            SetOwnBrain(treesbrain);
            treesbrain.AggroLevel = 100;
            treesbrain.AggroRange = 800;
            base.AddToWorld();
            return true;
        }
    }
#endregion Green Knight Trees