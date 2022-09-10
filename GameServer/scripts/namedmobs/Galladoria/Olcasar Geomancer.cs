using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class OlcasarGeomancer : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OlcasarGeomancer()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
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
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 350;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override void Die(GameObject killer)
        {
            foreach (GameNPC npc in GetNPCsInRadius(8000))
            {
                if (npc.Brain is OGAddsBrain)
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
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 19, 0, 0, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;

            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
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
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Olcasar Geomancer", 191, (eRealm) 0);
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
                OG.MeleeDamageType = eDamageType.Slash;
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
                    DBSpell spell = new DBSpell();
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
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Matter;
                    m_OGDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGDD);
                }
                return m_OGDD;
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class OlcasarGeomancerBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OlcasarGeomancerBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public static bool spawnadds = false;
        private bool RemoveAdds = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                spawnadds = false;
                CanCast2 = false;
                StartCastRoot = false;
                CanCastAoeSnare = false;
                RandomTarget2 = null;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
                    {
                        if (npc.Brain is OGAddsBrain)
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                    RemoveAdds = true;
                }
            }
            if (Body.InCombatInLast(30 * 1000) == false && Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (HasAggro && Body.TargetObject != null)
            {
                RemoveAdds = false;
                if (StartCastRoot == false)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickRandomTarget2), Util.Random(25000, 35000));
                    StartCastRoot = true;
                }
                if(spawnadds ==false)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastEffectBubble), 25000);
                    spawnadds = true;
                }
                if (Util.Chance(15))
                {
                    if (OGDS.TargetHasEffect(Body) == false)
                    {
                        Body.CastSpell(OGDS, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    }
                }
                if(CanCastAoeSnare == false &&  Body.HealthPercent <= 80)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastAoeSnare), 5000);
                    CanCastAoeSnare = true;
                }
            }
            base.Think();
        }
        #region Cast root on random target
        public static bool CanCast2 = false;
        public static bool StartCastRoot = false;
        public static GamePlayer randomtarget2 = null;
        public static GamePlayer RandomTarget2
        {
            get { return randomtarget2; }
            set { randomtarget2 = value; }
        }
        List<GamePlayer> Enemys_To_Root = new List<GamePlayer>();
        public int PickRandomTarget2(ECSGameTimer timer)
        {
            if (HasAggro)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!Enemys_To_Root.Contains(player))
                            {
                                Enemys_To_Root.Add(player);
                            }
                        }
                    }
                }
                if (Enemys_To_Root.Count > 0)
                {
                    if (CanCast2 == false)
                    {
                        GamePlayer Target = (GamePlayer)Enemys_To_Root[Util.Random(0, Enemys_To_Root.Count - 1)];//pick random target from list
                        RandomTarget2 = Target;//set random target to static RandomTarget
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastRoot), 2000);
                        CanCast2 = true;
                    }
                }
            }
            return 0;
        }
        public int CastRoot(ECSGameTimer timer)
        {
            if (HasAggro && RandomTarget2 != null)
            {
                GameLiving oldTarget = Body.TargetObject as GameLiving;//old target
                if (RandomTarget2 != null && RandomTarget2.IsAlive)
                {
                    Body.TargetObject = RandomTarget2;
                    Body.TurnTo(RandomTarget2);
                    Body.CastSpell(OGRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
                if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetRoot), 5000);
            }
            return 0;
        }
        public int ResetRoot(ECSGameTimer timer)
        {
            RandomTarget2 = null;
            CanCast2 = false;
            StartCastRoot = false;
            return 0;
        }
        #endregion
        public int CastEffectBubble(ECSGameTimer timer)
        {
            Body.CastSpell(OGBubbleEffect, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            BroadcastMessage(String.Format("Olcasar tears off a chunk of himself and tosses it to the ground."));
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Spawn), 2000);
            return 0;
        }
        public int Spawn(ECSGameTimer timer)
        {
            if (Body.IsAlive && HasAggro && Body.TargetObject != null)
            {
                OGAdds Add = new OGAdds();
                Add.X = Body.X + Util.Random(-50, 80);
                Add.Y = Body.Y + Util.Random(-50, 80);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.Heading = Body.Heading;
                Add.AddToWorld();             
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetSpawn), Util.Random(45000, 60000));
            }
            return 0;
        }
        public int ResetSpawn(ECSGameTimer timer)
        {
            spawnadds = false;
            return 0;
        }

        public static bool CanCastAoeSnare = false;
        public int CastAoeSnare(ECSGameTimer timer)
        {
            if (Body.IsAlive && HasAggro)
            {
                Body.CastSpell(OGAoeSnare, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetAoeSnare), Util.Random(45000, 60000));
            }
            return 0;
        }
        public int ResetAoeSnare(ECSGameTimer timer)
        {
            CanCastAoeSnare = false;
            return 0;
        }
        #region Spells
        private Spell m_OGDS;
        private Spell OGDS
        {
            get
            {
                if (m_OGDS == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 30;
                    spell.ClientEffect = 57;
                    spell.Icon = 57;
                    spell.Damage = 20;
                    spell.Duration = 30;
                    spell.Name = "Geomancer Damage Shield";
                    spell.TooltipId = 57;
                    spell.SpellID = 11717;
                    spell.Target = "Self";
                    spell.Type = "DamageShield";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Heat;
                    m_OGDS = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGDS);
                }
                return m_OGDS;
            }
        }
        private Spell m_OGRoot;
        private Spell OGRoot
        {
            get
            {
                if (m_OGRoot == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 60;
                    spell.ClientEffect = 5089;
                    spell.Icon = 5089;
                    spell.Duration = 60;
                    spell.Value = 99;
                    spell.Name = "Geomancer Root";
                    spell.TooltipId = 5089;
                    spell.SpellID = 11718;
                    spell.Target = "Enemy";
                    spell.Type = "SpeedDecrease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Matter;
                    m_OGRoot = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGRoot);
                }
                return m_OGRoot;
            }
        }
        private Spell m_OGAoeSnare;
        private Spell OGAoeSnare
        {
            get
            {
                if (m_OGAoeSnare == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 77;
                    spell.Icon = 77;
                    spell.Duration = 60;
                    spell.Value = 60;
                    spell.Radius = 2500;
                    spell.Range = 0;
                    spell.Name = "Olcasar Snare";
                    spell.TooltipId = 77;
                    spell.SpellID = 11862;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.SpeedDecrease.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Matter;
                    m_OGAoeSnare = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGAoeSnare);
                }
                return m_OGAoeSnare;
            }
        }
        private Spell m_OGBubbleEffect;
        private Spell OGBubbleEffect
        {
            get
            {
                if (m_OGBubbleEffect == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 5126;
                    spell.Icon = 5126;
                    spell.Value = 1;
                    spell.Name = "Olcasar Tear";
                    spell.TooltipId = 5126;
                    spell.SpellID = 11861;
                    spell.Target = "Self";
                    spell.Type = "Heal";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    m_OGBubbleEffect = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGBubbleEffect);
                }
                return m_OGBubbleEffect;
            }
        }
        #endregion
    }
}

///////////////////////////////////////adds here///////////////////////

namespace DOL.GS
{
    public class OGAdds : GameNPC
    {
        public OGAdds() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 35; // dmg reduction for melee dmg
                case eDamageType.Crush: return 35; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 35; // dmg reduction for melee dmg
                default: return 35; // dmg reduction for rest resists
            }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
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
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override void Die(GameObject killer)
        {
            BroadcastMessage(String.Format("As Olcasar minion falls to the ground, he begins to mutter some strange words and his slain minion rises back from the dead."));
            OGAdds Add = new OGAdds();
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
                    foreach (GameNPC boss in GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE))
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
            Realm = eRealm.None;
            OGAddsBrain adds = new OGAddsBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class OGAddsBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OGAddsBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
        }
        public override void Think()
        {
            if(HasAggro && Body.TargetObject != null)
            {
                GameLiving target = Body.TargetObject as GameLiving;
                if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity) && target != null && target.IsAlive)
                {
                    Body.CastSpell(addstun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
            }
            foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
            {
                if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
                {
                    if (player.CharacterClass.ID is 48 or 47 or 42 or 46) //bard,druid,menta,warden
                    {
                        if (Body.TargetObject != player)
                        {
                            AddToAggroList(player, 200);
                        }
                    }
                    else
                    {
                        Body.TargetObject = player;
                        AddToAggroList(player, 200);
                    }
                }
            }
            base.Think();
        }
        private Spell m_addstun;
        private Spell addstun
        {
            get
            {
                if (m_addstun == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 2132;
                    spell.Icon = 2132;
                    spell.Duration = 9;
                    spell.Range = 500;
                    spell.Name = "Stun";
                    spell.Description = "Stuns the target for 9 seconds.";
                    spell.Message1 = "You cannot move!";
                    spell.Message2 = "{0} cannot seem to move!";
                    spell.Message3 = "You recover from the stun.";
                    spell.Message4 = "{0} recovers from the stun.";
                    spell.TooltipId = 2132;
                    spell.SpellID = 11864;
                    spell.Target = "Enemy";
                    spell.Type = "StyleStun";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Body;
                    m_addstun = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_addstun);
                }
                return m_addstun;
            }
        }
    }
}