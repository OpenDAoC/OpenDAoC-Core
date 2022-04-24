using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Conservator : GameEpicBoss
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Conservator()
            : base()
        {
        }
        public virtual int COifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 60; // dmg reduction for melee dmg
                case eDamageType.Crush: return 60; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 60; // dmg reduction for melee dmg
                default: return 90; // dmg reduction for rest resists
            }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                Point3D spawn = new Point3D(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);
                if (!source.IsWithinRadius(spawn,800))//dont take any dmg 
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(this.Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
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
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }    
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159351);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Charisma = npcTemplate.Charisma;
            Empathy = npcTemplate.Empathy;
            ConservatorBrain.spampoison = false;
            ConservatorBrain.spamaoe = false;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

            ConservatorBrain sBrain = new ConservatorBrain();
            SetOwnBrain(sBrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
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
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 800;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Conservator", 191, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Conservator not found, creating it...");

                log.Warn("Initializing Conservator...");
                Conservator CO = new Conservator();
                CO.Name = "Conservator";
                CO.Model = 817;
                CO.Realm = 0;
                CO.Level = 77;
                CO.Size = 250;
                CO.CurrentRegionID = 191;//galladoria

                CO.Strength = 500;
                CO.Intelligence = 220;
                CO.Piety = 220;
                CO.Dexterity = 200;
                CO.Constitution = 200;
                CO.Quickness = 125;
                CO.BodyType = 5;
                CO.MeleeDamageType = eDamageType.Slash;
                CO.Faction = FactionMgr.GetFactionByID(96);
                CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

                CO.X = 31297;
                CO.Y = 41040;
                CO.Z = 13473;
                CO.MaxDistance = 2000;
                CO.TetherRange = 2500;
                CO.MaxSpeedBase = 300;
                CO.Heading = 409;

                ConservatorBrain ubrain = new ConservatorBrain();
                CO.SetOwnBrain(ubrain);
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159351);
                CO.LoadTemplate(npcTemplate);
                CO.AddToWorld();
                CO.Brain.Start();
                CO.SaveIntoDatabase();
            }
            else
                log.Warn("Conservator exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}
namespace DOL.AI.Brain
{
    public class ConservatorBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ConservatorBrain()
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
        protected virtual int PoisonTimer(ECSGameTimer timer)
        {
            if (Body.TargetObject != null)
            {
                Body.CastSpell(COPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                spampoison = false;
            }
            return 0;
        }
        protected virtual int AoeTimer(ECSGameTimer timer)//1st timer to spam broadcast before real spell
        {
            if (Body.TargetObject != null)
            {
                BroadcastMessage(String.Format(Body.Name + " gathers energy from the water..."));
                if (spamaoe == true)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(RealAoe), 5000);//5s
                }
            }
            return 0;
        }
        protected virtual int RealAoe(ECSGameTimer timer)//real timer to cast spell and reset check
        {
            if (Body.TargetObject != null)
            {
                Body.CastSpell(COaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                spamaoe = false;
            }
            return 0;
        }
        public static bool spampoison = false;
        public static bool spamaoe = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                spamaoe = false;
                spampoison = false;
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159351);
                Body.MaxSpeedBase = npcTemplate.MaxSpeed;
            }          
            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
                spamaoe = false;
                spampoison = false;
            }
            if(Body.IsOutOfTetherRange)
            {
                Body.StopFollowing();
                Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
                GameLiving target = Body.TargetObject as GameLiving;
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159351);
                if (target != null)
                {
                    if (!target.IsWithinRadius(spawn, 800))
                    {
                        Body.MaxSpeedBase = 0;
                    }
                    else
                        Body.MaxSpeedBase = npcTemplate.MaxSpeed;
                }
            }
            if (HasAggro && Body.InCombat)
            {
                if (Body.TargetObject != null)
                {
                    if (spampoison == false)
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
                        {
                            Body.TurnTo(Body.TargetObject);
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PoisonTimer), 5000);
                            spampoison = true;
                        }
                    }
                    if (spamaoe == false)
                    {
                        Body.TurnTo(Body.TargetObject);
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(AoeTimer), Util.Random(15000, 20000));//15s to avoid being it too often called
                        spamaoe = true;
                    }
                }
            }
            base.Think();
        }

        public Spell m_co_poison;
        public Spell COPoison
        {
            get
            {
                if (m_co_poison == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 40;
                    spell.ClientEffect = 4445;
                    spell.Icon = 4445;
                    spell.Damage = 45;
                    spell.Name = "Essense of World Soul";
                    spell.Description = "Inflicts powerfull magic damage to the target, then target dies in painfull agony";
                    spell.Message1 = "You are wracked with pain!";
                    spell.Message2 = "{0} is wracked with pain!";
                    spell.Message3 = "You look healthy again.";
                    spell.Message4 = "{0} looks healthy again.";
                    spell.TooltipId = 4445;
                    spell.Range = 1800;
                    spell.Duration = 40;
                    spell.Frequency = 10; 
                    spell.SpellID = 11703;
                    spell.Target = "Enemy";
                    spell.Type = "DamageOverTime";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
                    m_co_poison = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_co_poison);
                }
                return m_co_poison;
            }
        }

        public Spell m_co_aoe;
        public Spell COaoe
        {
            get
            {
                if (m_co_aoe == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.ClientEffect = 3510;
                    spell.Icon = 3510;
                    spell.TooltipId = 3510;
                    spell.Damage = 550;
                    spell.Range = 1800;
                    spell.Radius = 1200;
                    spell.SpellID = 11704;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    m_co_aoe = new Spell(spell, 70);                   
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_co_aoe);
                }
                return m_co_aoe;
            }
        }
    }
}