using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;

namespace DOL.GS
{
    public class OlcasarGeomancer : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OlcasarGeomancer()
            : base()
        {
        }

        public virtual int OGDifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get
            {
                return 20000;
            }
        }

        public override int AttackRange
        {
            get
            {
                return 450;
            }
            set
            {
            }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 1000;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }
        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164613);
            LoadTemplate(npcTemplate);
            base.AddToWorld();
            return true;
        }
        
        public override void Die(GameObject killer)
        {
            // debug
            log.Debug($"{Name} killed by {killer.Name}");
            
            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,5000);
                }
            }
            DropLoot(killer);
            base.Die(killer);
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Olcasar Geomancer", 191, (eRealm)0);
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
                OG.CurrentRegionID = 191;//galladoria

                OG.Strength = 500;
                OG.Intelligence = 220;
                OG.Piety = 220;
                OG.Dexterity = 200;
                OG.Constitution = 200;
                OG.Quickness = 125;
                OG.BodyType = 8;//magician
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
                log.Warn("Olcasar Geomancer exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class OlcasarGeomancerBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public OlcasarGeomancerBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        private int m_stage = 10;

        /// <summary>
        /// This keeps track of the stage the encounter is in.
        /// </summary>
        public int Stage
        {
            get { return m_stage; }
            set { if (value >= 0 && value <= 10) m_stage = value; }
        }

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        protected virtual int BombTimer(RegionTimer timer)
        {           
            Body.CastSpell(OGBomb, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                Spawn(); // spawn adds
            Body.MaxSpeedBase = 300;
            return 0; 
        }
        public static bool spawnadds = true;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                spawnadds = true;
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is OGAddsBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                this.Body.Health = this.Body.MaxHealth;
                Stage = 10;
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                this.Body.Health = this.Body.MaxHealth;
                Stage = 10;
            }
            else if (Body.HealthPercent == 100 && Stage < 10 && !HasAggro)
                Stage = 10;

            int health = Body.HealthPercent / 10;

            if(Body.InCombat && HasAggro)
            {
                if (Util.Chance(15))
                {
                    PickRandomTarget();
                }
                if (Util.Chance(15))
                {
                    if (OGDS.TargetHasEffect(Body) == false)
                    {
                        Body.CastSpell(OGDS, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    }
                }
            }
            if (Body.TargetObject != null && health < Stage && Body.InCombat)
            {
                switch (health)
                    {
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        {
                            if (!Body.IsCasting)
                            {
                                Body.TurnTo(Body.TargetObject);
                                
                                foreach(GamePlayer ppl in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                {
                                    if (Body.AttackState)
                                    {
                                        Body.StopAttack();
                                        Body.StopFollowing();
                                    }
                                    if (Body.IsMoving)
                                    {
                                        Body.StopMoving();
                                    }
                                    Body.MaxSpeedBase = 0;
                                    BroadcastMessage(String.Format(Body.Name + " calling ice magic to aid him in battle!"));
                                    ppl.Out.SendSpellCastAnimation(Body, 159, 4);//casting bomb effect
                                    new RegionTimer(Body, new RegionTimerCallback(BombTimer), 5000);
                                }
                            }
                        }
                        break;
                    }
                Stage = health;
            }
            base.Think();
        }

        private GameLiving randomtarget;
        private GameLiving RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public void PickRandomTarget()
        {
            ArrayList inRangeLiving = new ArrayList();
            foreach (GameLiving living in Body.GetPlayersInRadius(2000))
            {
                if (living.IsAlive)
                {
                    if (living is GamePlayer || living is GamePet)
                    {
                        if (!inRangeLiving.Contains(living) || inRangeLiving.Contains(living) == false)
                        {
                            inRangeLiving.Add(living);
                        }
                    }
                }
            }
            if (inRangeLiving.Count > 0)
            {
                GameLiving ptarget = ((GameLiving)(inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
                RandomTarget = ptarget;
                if (OGRoot.TargetHasEffect(randomtarget) == false && randomtarget.IsVisibleTo(Body))
                {
                    PrepareToRoot();
                }
            }
        }
        private int CastRoot(RegionTimer timer)
        {
            GameObject oldTarget = Body.TargetObject;
            Body.TargetObject = RandomTarget;
            Body.TurnTo(RandomTarget);
            if (Body.TargetObject != null)
            {
                Body.CastSpell(OGRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                spambroad = false;//to avoid spamming
            }
            RandomTarget = null;
            if (oldTarget != null) Body.TargetObject = oldTarget;
            return 0;
        }
        public static bool spambroad = false;
        private void PrepareToRoot()
        {
            if (spambroad == false)
            {
                new RegionTimer(Body, new RegionTimerCallback(CastRoot), 5000);
                spambroad = true;
            }
        }

        public void Spawn()
        {
            for (int i = 0; i < Util.Random(4,8); i++) // Spawn 4-8 adds
            {
                OGAdds Add = new OGAdds();
                Add.X = Body.X + Util.Random(50, 80);
                Add.Y = Body.Y + Util.Random(50, 80);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.IsWorthReward = false;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
            }
            BroadcastMessage(String.Format("...a piece of Olcasar Geomancer falls from its body, and attacks!"));
        }

        private Spell m_OGBomb;
        private Spell OGBomb
        {
            get
            {
                if (m_OGBomb == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.ClientEffect = 208;
                    spell.Icon = 208;
                    spell.Damage = 450;
                    spell.Duration = 35;
                    spell.Value = 40;
                    spell.Name = "Geomancer Snare";
                    spell.TooltipId = 4445;
                    spell.Radius = 800;
                    spell.SpellID = 11702;
                    spell.Target = "Enemy";
                    spell.Type = "DamageSpeedDecrease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold; 
                    m_OGBomb = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGBomb);
                }
                return m_OGBomb;
            }
        }

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
                    spell.DamageType = (int)eDamageType.Heat;
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
                    spell.DamageType = (int)eDamageType.Matter;
                    m_OGRoot = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OGRoot);
                }
                return m_OGRoot;
            }
        }
    }
}

///////////////////////////////////////adds here///////////////////////

namespace DOL.GS
{
    public class OGAdds : GameNPC
    {
        public OGAdds() : base() { }
        public static GameNPC og_adds = new GameNPC();
        public override int MaxHealth
        {
            get { return 1200; }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            base.Die(null); // null to not gain experience
        }

        public override bool AddToWorld()
        {
            Model = 925;
            Name = "Geomancer's Servant";
            RespawnInterval = -1;
            MaxDistance = 2500;
            TetherRange = 3000;
            Size = (byte)Util.Random(45, 55);
            Level = (byte)Util.Random(60, 65);
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public OGAddsBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 450;
        }

        public override void Think()
        {
            foreach(GamePlayer player in Body.GetPlayersInRadius(2000))
            {
                if(player != null && player.IsAlive)
                {
                    if (player.CharacterClass.ID is 48 or 47 or 42 or 46)//bard,druid,menta,warden
                    {
                        if (Body.TargetObject != player)
                        {
                            Body.TargetObject = player;
                            Body.StartAttack(player);
                        }
                    }
                    else
                    {
                        Body.TargetObject = player;
                        Body.StartAttack(player);
                    }
                }
            }
            base.Think();
        }
    }
}