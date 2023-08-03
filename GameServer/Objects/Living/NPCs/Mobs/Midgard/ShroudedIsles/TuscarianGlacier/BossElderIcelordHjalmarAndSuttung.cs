﻿using System;
using System.Threading.Tasks;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;

#region Suttung
namespace DOL.GS
{
    public class BossElderIcelordSuttung : GameEpicBoss
    {
        public BossElderIcelordSuttung() : base()
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

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.ServerProperties.EPICS_DMG_MULTIPLIER;
        }

        public override int AttackRange
        {
            get { return 350; }
            set { }
        }

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
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

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160395);
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
            RespawnInterval = -1; 
            BodyType = (ushort)NpcTemplateMgr.EBodyType.Giant;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 573, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.Standard);
            SuttungBrain.message1 = false;
            SuttungBrain.message2 = false;
            SuttungCount = 1;

            VisibleActiveWeaponSlots = 16;
            SuttungBrain sbrain = new SuttungBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = true; 
            base.AddToWorld();
            return true;
        }
        public static int SuttungCount = 0;
        public override void Die(GameObject killer)
        {
            SuttungCount = 0;
            base.Die(killer);
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (UtilCollection.Chance(15))
            {
                if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
                    CastSpell(SuttungDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.OnAttackEnemy(ad);
        }
        private Spell m_SuttungDisease;
        private Spell SuttungDisease
        {
            get
            {
                if (m_SuttungDisease == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 2;
                    spell.ClientEffect = 731;
                    spell.Icon = 731;
                    spell.Name = "Valnir Mordeth's Plague";
                    spell.Message1 = "You are diseased!";
                    spell.Message2 = "{0} is diseased!";
                    spell.Message3 = "You look healthy.";
                    spell.Message4 = "{0} looks healthy again.";
                    spell.TooltipId = 731;
                    spell.Range = 400;
                    spell.Duration = 60;
                    spell.SpellID = 11928;
                    spell.Target = "Enemy";
                    spell.Type = "Disease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
                    m_SuttungDisease = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SuttungDisease);
                }
                return m_SuttungDisease;
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class SuttungBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SuttungBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 1500;
        }

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
            }
        }

        public static bool IsBerserker = false;

        public int BerserkerPhase(ECSGameTimer timer)
        {
            if (Body.IsAlive && IsBerserker == true && Body.InCombat && HasAggro)
            {
                BroadcastMessage(String.Format(Body.Name + " goes into berserker stance!"));
                Body.Emote(EEmote.MidgardFrenzy);
                Body.Strength = 850;
                Body.MaxSpeedBase = 200; //slow under zerk mode
                Body.Size = 75;
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(EndBerserkerPhase),UtilCollection.Random(10000, 20000)); //10-20s in berserk stance
            }
            return 0;
        }

        public int EndBerserkerPhase(ECSGameTimer timer)
        {
            if (Body.IsAlive)
            {
                BroadcastMessage(String.Format(Body.Name + " berserker stance fades away!"));
                Body.Strength = Body.NPCTemplate.Strength;
                Body.Size = Convert.ToByte(Body.NPCTemplate.Size);
                Body.MaxSpeedBase = Body.NPCTemplate.MaxSpeed;
                IsBerserker = false;
            }

            return 0;
        }

        public static bool message1 = false;
        public static bool message2 = false;
        public static bool AggroText = false;

        public override void Think()
        {
            Point3D point = new Point3D(31088, 53870, 11886);
            if(Body.IsAlive)
            {
                foreach(GamePlayer player in Body.GetPlayersInRadius(8000))
                {
                    if(player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && !message2 && player.IsWithinRadius(point, 400))
                        message2=true;
                }
                if(message2 && !message1)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Announce), 200);
                    message1 = true;
                }
            }
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                AggroText = false;
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }

            if (HasAggro)
            {
                if(!AggroText)
                {
                    BroadcastMessage(String.Format(Body.Name + " says, 'The price of your invading our frozen fortress is death!" +
                    " Death to you and your allies! Your presence here mocks the pacifist philosophy of my opponents on the Council." +
                    " I weep for no council member who has perished!'"));
                    AggroText = true;
                }
                if (IsBerserker == false)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(BerserkerPhase), UtilCollection.Random(20000, 35000));
                    IsBerserker = true;
                }
            }
            if(HasAggro && Body.TargetObject != null)
            {
                if (UtilCollection.Chance(55))
                    Body.CastSpell(IcelordHjalmar_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            }
            base.Think();
        }
        private int Announce(ECSGameTimer timer)
        {
            BroadcastMessage("An otherworldly howling sound suddenly becomes perceptible. The sound quickly grows louder but it is not accompanied by a word. Moments after it begins, the howling sound is gone, replace by the familiar noises of the slowly shifting glacier.");
            return 0;
        }
        private Spell m_IcelordHjalmar_aoe;
        private Spell IcelordHjalmar_aoe
        {
            get
            {
                if (m_IcelordHjalmar_aoe == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 15;
                    spell.ClientEffect = 208;
                    spell.Icon = 208;
                    spell.TooltipId = 208;
                    spell.Damage = 450;
                    spell.Name = "Hjalmar's Ice Blast";
                    spell.Range = 0;
                    spell.Radius = 440;
                    spell.SpellID = 11901;
                    spell.Target = ESpellTarget.Enemy.ToString();
                    spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Cold;
                    m_IcelordHjalmar_aoe = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IcelordHjalmar_aoe);
                }
                return m_IcelordHjalmar_aoe;
            }
        }
    }
}
#endregion

#region Hjalmar
namespace DOL.GS
{
    public class BossElderIcelordHjalmar : GameEpicBoss
    {
        public BossElderIcelordHjalmar() : base()
        {
        }

        public static int TauntID = 292;
        public static int TauntClassID = 44;
        public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

        public static int BackStyleID = 304;
        public static int BackStyleClassID = 44;
        public static Style back_style = SkillBase.GetStyleByID(BackStyleID, BackStyleClassID);

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

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.ServerProperties.EPICS_DMG_MULTIPLIER;
        }

        public override int AttackRange
        {
            get { return 350; }
            set { }
        }

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
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
        public static int HjalmarCount = 0;
        public override void Die(GameObject killer) //on kill generate orbs
        {
            HjalmarCount = 0;
            base.Die(killer);
        }

        public override void OnAttackEnemy(AttackData ad)
        {
            if (ad != null && (ad.AttackResult == EAttackResult.HitStyle || ad.AttackResult == EAttackResult.HitUnstyled))
            {
                if (UtilCollection.Chance(20))
                    SpawnAdds();
            }
            base.OnAttackEnemy(ad);
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160394);
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
            RespawnInterval = -1;
            BodyType = (ushort)NpcTemplateMgr.EBodyType.Giant;
            HjalmarBrain.message1 = false;
            HjalmarBrain.message2 = false;
            HjalmarCount = 1;

            if(!Styles.Contains(taunt))
                Styles.Add(taunt);
            if (!Styles.Contains(back_style))
                Styles.Add(back_style);

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 572, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.TwoHanded);

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = EDamageType.Slash;
            HjalmarBrain sbrain = new HjalmarBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = true;
            base.AddToWorld();
            return true;
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
            }
        }
        public void SpawnAdds()
        {
            BroadcastMessage(Name + " spasms as dark energies swirl around his body!");
            NpcMorkimma npc = new NpcMorkimma();
            npc.X = TargetObject.X + UtilCollection.Random(-100, 100);
            npc.Y = TargetObject.Y + UtilCollection.Random(-100, 100);
            npc.Z = TargetObject.Z;
            npc.RespawnInterval = -1;
            npc.Heading = Heading;
            npc.CurrentRegion = CurrentRegion;
            npc.AddToWorld();
        }
    }
}

namespace DOL.AI.Brain
{
    public class HjalmarBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public HjalmarBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 1500;
        }

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
            }
        }

        public static bool message1 = false;
        public static bool message2 = false;
        public static bool AggroText = false;
        private bool RemoveAdds = false;
        public override void Think()
        {
            Point3D point = new Point3D(31088, 53870, 11886);
            if (Body.IsAlive)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(8000))
                {
                    if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && !message2 && player.IsWithinRadius(point, 400))
                        message2 = true;
                }
                if (message2 && !message1)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Announce), 200);
                    message1 = true;
                }
            }
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160394);
                Body.Strength = npcTemplate.Strength;
                message2 = false;
                if (!RemoveAdds)
                {
                    foreach (GameNpc npc in Body.GetNPCsInRadius(4500))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive)
                            {
                                if (npc.Brain is MorkimmaBrain)
                                    npc.Die(Body);
                            }
                        }
                    }
                    RemoveAdds = true;
                }
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }

            if (HasAggro && Body.TargetObject != null)
            {
                RemoveAdds = false;
                if (message2 == false)
                {
                    BroadcastMessage(Body.Name + " bellows 'I am amazed that you have made it this far! I'm afraid that your journey ends here with all of your death, however, I will show you no mercy!'");
                    BroadcastMessage(String.Format(Body.Name +" says, I have warned the Council that if we do not destroy those who threaten us before they destroy us, we will perish." +
                        " You deserve this fate more than I do. I will not mourn her death beyond the grave!"));
                    message2 = true;
                }

                if (Body.TargetObject != null)
                {
                    float angle = Body.TargetObject.GetAngle(Body);
                    GameLiving living = Body.TargetObject as GameLiving;
                    if (UtilCollection.Chance(100))
                    {
                        if (angle >= 150 && angle < 210)
                        {
                            Body.Strength = 740;
                            Body.styleComponent.NextCombatStyle = BossElderIcelordHjalmar.back_style;
                        }
                        else
                        {
                            Body.Strength = 600;
                            Body.styleComponent.NextCombatStyle = BossElderIcelordHjalmar.taunt;
                        }
                    }
                }
            }
            base.Think();
        }
        private int Announce(ECSGameTimer timer)
        {
            BroadcastMessage("An otherworldly howling sound suddenly becomes perceptible. The sound quickly grows louder but it is not accompanied by a word. Moments after it begins, the howling sound is gone, replace by the familiar noises of the slowly shifting glacier.");
            return 0;
        }
    }
}

///////////////////////////////////////////////////////////Hjalmar adds/////////////////////////////////////////////////////////////////////////////
namespace DOL.GS
{
    public class NpcMorkimma : GameNpc
    {
        public NpcMorkimma() : base()
        {
        }

        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 25; // dmg reduction for melee dmg
                case EDamageType.Crush: return 25; // dmg reduction for melee dmg
                case EDamageType.Thrust: return 25; // dmg reduction for melee dmg
                default: return 25; // dmg reduction for rest resists
            }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 50 * ServerProperties.ServerProperties.EPICS_DMG_MULTIPLIER;
        }

        protected int Show_Effect(ECSGameTimer timer)
        {
            if (IsAlive)
            {
                Parallel.ForEach(GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE), player =>
                {
                    player?.Out.SendSpellEffectAnimation(this, this, 4323, 0, false, 0x01);
                });
                return 3000;
            }

            return 0;
        }
        
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.25;
        }
        public override int MaxHealth
        {
            get { return 1000; }
        }
        public override bool AddToWorld()
        {
            Model = 665;
            Size = 50;
            Strength = 100;
            Quickness = 100;
            Dexterity = 180;
            Constitution = 100;
            MaxSpeedBase = 220;
            Name = "Morkimma";
            Level = (byte)UtilCollection.Random(50, 55);

            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            Realm = ERealm.None;
            RespawnInterval = -1;

            MorkimmaBrain adds = new MorkimmaBrain();
            SetOwnBrain(adds);
            bool success = base.AddToWorld();
            if (success)
            {
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
            }
            return success;
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (UtilCollection.Chance(15))
            {
                if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle) && HealthPercent < 100)
                    CastSpell(MorkimmaHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.OnAttackEnemy(ad);
        }
        private Spell m_MorkimmaHeal;
        private Spell MorkimmaHeal
        {
            get
            {
                if (m_MorkimmaHeal == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 10;
                    spell.ClientEffect = 1340;
                    spell.Icon = 1340;
                    spell.TooltipId = 1340;
                    spell.Value = 200;
                    spell.Name = "Morkimma's Heal";
                    spell.Range = 1500;
                    spell.SpellID = 11930;
                    spell.Target = "Self";
                    spell.Type = ESpellType.Heal.ToString();
                    spell.Uninterruptible = true;
                    m_MorkimmaHeal = new Spell(spell, 50);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MorkimmaHeal);
                }
                return m_MorkimmaHeal;
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class MorkimmaBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MorkimmaBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
            ThinkInterval = 1500;
        }
        public override void Think()
        {
            base.Think();
        }     
    }
}
#endregion

#region Hjalmar and Suttung Controller
namespace DOL.GS
{
    public class HjalmarSuttungController : GameNpc
    {
        public HjalmarSuttungController() : base()
        {
        }
        public override bool IsVisibleToPlayers => true;
        public override bool AddToWorld()
        {
            Name = "HjalmarSuttung Controller";
            GuildName = "DO NOT REMOVE";
            Level = 50;
            Model = 665;
            RespawnInterval = 5000;
            Flags = (GameNpc.eFlags)28;
            SpawnBoss();

            HjalmarSuttungControllerBrain sbrain = new HjalmarSuttungControllerBrain();
            SetOwnBrain(sbrain);
            base.AddToWorld();
            return true;
        }
        private void SpawnBoss()
        {
            switch (UtilCollection.Random(1, 2))
            {
                case 1: SpawnSuttung(); break;
                case 2: SpawnHjalmar(); break;
            }
        }
        private void SpawnSuttung()
        {
            if (BossElderIcelordSuttung.SuttungCount == 0)
            {
                BossElderIcelordSuttung boss = new BossElderIcelordSuttung();
                boss.X = 32055;
                boss.Y = 54253;
                boss.Z = 11883;
                boss.Heading = 2084;
                boss.CurrentRegion = CurrentRegion;
                boss.AddToWorld();
                HjalmarSuttungControllerBrain.Spawn_Boss = false;
            }
        }
        private void SpawnHjalmar()
        {
            if (BossElderIcelordHjalmar.HjalmarCount == 0)
            {
                BossElderIcelordHjalmar boss = new BossElderIcelordHjalmar();
                boss.X = 32079;
                boss.Y = 53415;
                boss.Z = 11885;
                boss.Heading = 21;
                boss.CurrentRegion = CurrentRegion;
                boss.AddToWorld();
                HjalmarSuttungControllerBrain.Spawn_Boss = false;
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class HjalmarSuttungControllerBrain : APlayerVicinityBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public HjalmarSuttungControllerBrain()
            : base()
        {
            ThinkInterval = 1000;
        }
        public static bool Spawn_Boss = false;
        public override void Think()
        {
            int respawn = GS.ServerProperties.ServerProperties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;
            if (Body.IsAlive)
            {
                if (BossElderIcelordSuttung.SuttungCount == 1 || BossElderIcelordHjalmar.HjalmarCount == 1)//one of them is up
                {
                    //log.Warn("Suttung or Hjalmar is around");
                }
                if(BossElderIcelordSuttung.SuttungCount == 0 && BossElderIcelordHjalmar.HjalmarCount == 0)//noone of them is up
                {
                    if (!Spawn_Boss)
                    {
                        //log.Warn("Trying to respawn Suttung or Hjalmar");
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnBoss), respawn);
                        Spawn_Boss = true;
                    }
                }
            }
        }

        public override void KillFSM()
        {
            
        }

        private int SpawnBoss(ECSGameTimer timer)
        {
            switch(UtilCollection.Random(1,2))
            {
                case 1: SpawnSuttung(); break;
                case 2: SpawnHjalmar(); break;
            }
            return 0;
        }
        private void SpawnSuttung()
        {
            if (BossElderIcelordSuttung.SuttungCount == 0)
            {
                BossElderIcelordSuttung boss = new BossElderIcelordSuttung();
                boss.X = 32055;
                boss.Y = 54253;
                boss.Z = 11883;
                boss.Heading = 2084;
                boss.CurrentRegion = Body.CurrentRegion;
                boss.AddToWorld();
                Spawn_Boss = false;
            }
        }
        private void SpawnHjalmar()
        {
            if (BossElderIcelordHjalmar.HjalmarCount == 0)
            {
                BossElderIcelordHjalmar boss = new BossElderIcelordHjalmar();
                boss.X = 32079;
                boss.Y = 53415;
                boss.Z = 11885;
                boss.Heading = 21;
                boss.CurrentRegion = Body.CurrentRegion;
                boss.AddToWorld();
                Spawn_Boss = false;
            }
        }
    }
}
#endregion