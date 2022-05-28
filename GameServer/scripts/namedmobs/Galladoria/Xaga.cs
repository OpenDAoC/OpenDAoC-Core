using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Xaga : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Xaga()
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
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (this.IsOutOfTetherRange)//dont take any dmg if is too far away from spawn point
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
        public override int MaxHealth
        {
            get { return 200000; }
        }
        public override int AttackRange
        {
            get { return 450; }
            set { }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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
        public void SpawnTineBeatha()
        {
            if (Tine.TineCount == 0)
            {
                Tine tine = new Tine();
                tine.X = 27211;
                tine.Y = 54902;
                tine.Z = 13213;
                tine.CurrentRegion = CurrentRegion;
                tine.Heading = 2157;
                tine.RespawnInterval = -1;
                tine.AddToWorld();
            }
            if (Beatha.BeathaCount == 0)
            {
                Beatha beatha = new Beatha();
                beatha.X = 27614;
                beatha.Y = 54866;
                beatha.Z = 13213;
                beatha.CurrentRegion = CurrentRegion;
                beatha.Heading = 2038;
                beatha.RespawnInterval = -1;
                beatha.AddToWorld();
            }
        }
        public static bool spawn_lights = false;
        public override void Die(GameObject killer)
        {
            foreach(GameNPC lights in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
            {
                if(lights != null)
                {
                    if(lights.IsAlive && (lights.Brain is TineBrain || lights.Brain is BeathaBrain))
                        lights.Die(lights);
                }
            }
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168075);
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
            XagaBrain sBrain = new XagaBrain();
            SetOwnBrain(sBrain);
            SaveIntoDatabase();
            LoadedFromScript = false;
            spawn_lights = false;
            bool success = base.AddToWorld();
            if (success)
            {
                if (spawn_lights == false)
                {
                    SpawnTineBeatha();
                    spawn_lights = true;
                }
            }
            return success;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Xaga", 191, (eRealm) 0);
            if (npcs.Length == 0)
            {
                log.Warn("Xaga not found, creating it...");

                log.Warn("Initializing Xaga...");
                Xaga SB = new Xaga();
                SB.Name = "Xaga";
                SB.Model = 917;
                SB.Realm = 0;
                SB.Level = 81;
                SB.Size = 250;
                SB.CurrentRegionID = 191; //galladoria

                SB.Strength = 260;
                SB.Intelligence = 220;
                SB.Piety = 220;
                SB.Dexterity = 200;
                SB.Constitution = 200;
                SB.Quickness = 125;
                SB.BodyType = 5;
                SB.MeleeDamageType = eDamageType.Slash;
                SB.Faction = FactionMgr.GetFactionByID(96);
                SB.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

                SB.X = 27397;
                SB.Y = 54975;
                SB.Z = 12949;
                SB.MaxDistance = 2000;
                SB.TetherRange = 2500;
                SB.MaxSpeedBase = 300;
                SB.Heading = 2013;

                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168075);
                SB.LoadTemplate(npcTemplate);

                XagaBrain ubrain = new XagaBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 500;
                SB.SetOwnBrain(ubrain);

                SB.AddToWorld();
                SB.Brain.Start();
                SB.SaveIntoDatabase();
            }
            else
                log.Warn("Xaga exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class XagaBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public XagaBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                foreach (GameNPC mob_c in Body.GetNPCsInRadius(4000, false))
                {
                    if (mob_c != null)
                    {
                        if (mob_c?.Brain is BeathaBrain brain1 && mob_c.IsAlive && brain1.HasAggro)
                            brain1.ClearAggroList();
                        if (mob_c?.Brain is TineBrain brain2 && mob_c.IsAlive && brain2.HasAggro)
                            brain2.ClearAggroList();
                    }
                }
            }
            if (HasAggro && Body.InCombat)
            {
            }
            base.Think();
        }
        
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (Body.IsAlive)
            {
                foreach (GameNPC mob_c in Body.GetNPCsInRadius(4000, false))
                {
                    if (mob_c != null)
                    {
                        if (mob_c?.Brain is BeathaBrain brain1 && mob_c.IsAlive && !brain1.HasAggro)
                            AddAggroListTo(brain1);
                        if (mob_c?.Brain is TineBrain brain2 && mob_c.IsAlive && !brain2.HasAggro)
                            AddAggroListTo(brain2);
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }
    }
}
////////////////////////////////////////////////Beatha/////////////////////////////////////////////
#region Beatha
namespace DOL.GS
{
    public class Beatha : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Beatha()
            : base()
        {
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override void WalkToSpawn() //dont walk to spawn
        {
            if (IsAlive)
                return;
            base.WalkToSpawn();
        }
        public override void DealDamage(AttackData ad)
        {
            if (ad != null)
            {
                foreach (GameNPC xaga in GetNPCsInRadius(8000))
                {
                    if (xaga != null)
                    {
                        if (xaga.IsAlive && xaga.Brain is XagaBrain)
                            xaga.Health += ad.Damage*2;//dmg heals xaga
                    }
                }
            }
            base.DealDamage(ad);
        }
        public override int MaxHealth
        {
            get { return 50000; }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 300;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public static int BeathaCount = 0;
        public override void Die(GameObject killer)
        {
            --BeathaCount;
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158330);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Charisma = npcTemplate.Charisma;
            Empathy = npcTemplate.Empathy;
            Flags = eFlags.FLYING;
            BeathaBrain.path4 = false;
            BeathaBrain.path1 = false;
            BeathaBrain.path2 = false;
            BeathaBrain.path3 = false;

            AbilityBonus[(int)eProperty.Resist_Body] = 60;
            AbilityBonus[(int)eProperty.Resist_Heat] = -20;//weak to heat
            AbilityBonus[(int)eProperty.Resist_Cold] = 99;//resi to cold
            AbilityBonus[(int)eProperty.Resist_Matter] = 60;
            AbilityBonus[(int)eProperty.Resist_Energy] = 60;
            AbilityBonus[(int)eProperty.Resist_Spirit] = 60;
            AbilityBonus[(int)eProperty.Resist_Slash] = 40;
            AbilityBonus[(int)eProperty.Resist_Crush] = 40;
            AbilityBonus[(int)eProperty.Resist_Thrust] = 40;

            ++BeathaCount;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BeathaBrain sBrain = new BeathaBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class BeathaBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BeathaBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }       
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (Body.IsAlive)
            {
                foreach (GameNPC mob_c in Body.GetNPCsInRadius(4000))
                {
                    if (mob_c != null)
                    {
                        if (mob_c?.Brain is XagaBrain brain1 && mob_c.IsAlive && mob_c.IsAvailable && !brain1.HasAggro)
                            AddAggroListTo(brain1);
                        if (mob_c?.Brain is TineBrain brain2 && mob_c.IsAlive && mob_c.IsAvailable && !brain2.HasAggro)
                            AddAggroListTo(brain2);
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }
        public static bool path1 = false;
        public static bool path2 = false;
        public static bool path3 = false;
        public static bool path4 = false;
        public override void Think()
        {
            if(Body.IsAlive)
            {
                Point3D point1 = new Point3D(27572,54473,13213);
                Point3D point2 = new Point3D(27183, 54530, 13213);
                Point3D point3 = new Point3D(27213, 55106, 13213);
                Point3D point4 = new Point3D(27581, 55079, 13213);
                if (!Body.IsWithinRadius(point1, 20) && path1 == false)
                {
                    Body.WalkTo(point1, 250);
                }
                else
                {
                    path1 = true;
                    path4 = false;
                    if (!Body.IsWithinRadius(point2, 20) && path1 == true && path2 == false)
                    {
                        Body.WalkTo(point2, 250);
                    }
                    else
                    {
                        path2 = true;
                        if (!Body.IsWithinRadius(point3, 20) && path1 == true && path2 == true && path3 == false)
                        {
                            Body.WalkTo(point3, 250);
                        }
                        else
                        {
                            path3 = true;
                            if (!Body.IsWithinRadius(point4, 20) && path1 == true && path2 == true && path3 == true && path4 == false)
                            {
                                Body.WalkTo(point4, 250);
                            }
                            else
                            {
                                path4 = true;
                                path1 = false;
                                path2 = false;
                                path3 = false;
                            }
                        }
                    }
                }
            }
            if(!HasAggressionTable())
            {
                Body.Health = Body.MaxHealth;
            }
            if (HasAggro && Body.IsAlive)
            {
                GameLiving target = Body.TargetObject as GameLiving;
                if (target != null)
                {
                    Body.SetGroundTarget(target.X, target.Y, target.Z);
                    Body.CastSpell(BeathaAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
                }
            }
            base.Think();
        }
        private Spell m_BeathaAoe;
        private Spell BeathaAoe
        {
            get
            {
                if (m_BeathaAoe == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 8;
                    spell.ClientEffect = 4568;
                    spell.Icon = 4568;
                    spell.Damage = 450;
                    spell.Name = "Beatha's Void";
                    spell.TooltipId = 4568;
                    spell.Range = 3000;
                    spell.Radius = 450;
                    spell.SpellID = 11707;
                    spell.Target = "Area";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Cold;
                    m_BeathaAoe = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BeathaAoe);
                }
                return m_BeathaAoe;
            }
        }
    }
}
#endregion
/////////////////////Tine///////////////
#region Tine
namespace DOL.GS
{
    public class Tine : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Tine()
            : base()
        {
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override void WalkToSpawn() //dont walk to spawn
        {
            if (IsAlive)
                return;
            base.WalkToSpawn();
        }
        public override int MaxHealth
        {
            get { return 50000; }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 300;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public static int TineCount = 0;
        public override void Die(GameObject killer)
        {
            --TineCount;
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167084);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Charisma = npcTemplate.Charisma;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            Flags = eFlags.FLYING;
            TineBrain.path4_2 = false;
            TineBrain.path1_2 = false;
            TineBrain.path2_2 = false;
            TineBrain.path3_2 = false;

            AbilityBonus[(int)eProperty.Resist_Body] = 60;
            AbilityBonus[(int)eProperty.Resist_Heat] = 99;//resi to heat
            AbilityBonus[(int)eProperty.Resist_Cold] = -20;//weak to cold
            AbilityBonus[(int)eProperty.Resist_Matter] = 60;
            AbilityBonus[(int)eProperty.Resist_Energy] = 60;
            AbilityBonus[(int)eProperty.Resist_Spirit] = 60;
            AbilityBonus[(int)eProperty.Resist_Slash] = 40;
            AbilityBonus[(int)eProperty.Resist_Crush] = 40;
            AbilityBonus[(int)eProperty.Resist_Thrust] = 40;

            ++TineCount;
            TineBrain sBrain = new TineBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class TineBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TineBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (Body.IsAlive)
            {
                foreach (GameNPC mob_c in Body.GetNPCsInRadius(4000))
                {
                    if (mob_c != null)
                    {
                        if (mob_c?.Brain is XagaBrain brain1 && mob_c.IsAlive && mob_c.IsAvailable && !brain1.HasAggro)
                            AddAggroListTo(brain1);
                        if (mob_c?.Brain is BeathaBrain brain2 && mob_c.IsAlive && mob_c.IsAvailable && !brain2.HasAggro)
                            AddAggroListTo(brain2);
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }
        public static bool path1_2 = false;
        public static bool path2_2 = false;
        public static bool path3_2 = false;
        public static bool path4_2 = false;
        public override void Think()
        {
            if (Body.IsAlive)
            {
                Point3D point1 = new Point3D(27168, 54598, 13213);
                Point3D point2 = new Point3D(27597, 54579, 13213);
                Point3D point3 = new Point3D(27606, 55086, 13213);
                Point3D point4 = new Point3D(27208, 55133, 13213);
                if (!Body.IsWithinRadius(point1, 20) && path1_2 == false)
                {
                    Body.WalkTo(point1, 250);
                }
                else
                {
                    path1_2 = true;
                    path4_2 = false;
                    if (!Body.IsWithinRadius(point2, 20) && path1_2 == true && path2_2 == false)
                    {
                        Body.WalkTo(point2, 250);
                    }
                    else
                    {
                        path2_2 = true;
                        if (!Body.IsWithinRadius(point3, 20) && path1_2 == true && path2_2 == true && path3_2 == false)
                        {
                            Body.WalkTo(point3, 250);
                        }
                        else
                        {
                            path3_2 = true;
                            if (!Body.IsWithinRadius(point4, 20) && path1_2 == true && path2_2 == true && path3_2 == true && path4_2 == false)
                            {
                                Body.WalkTo(point4, 250);
                            }
                            else
                            {
                                path4_2 = true;
                                path1_2 = false;
                                path2_2 = false;
                                path3_2 = false;
                            }
                        }
                    }
                }
            }
            if (!HasAggressionTable())
            {
                Body.Health = Body.MaxHealth;
            }
            if (HasAggro && Body.IsAlive)
            {
                GameLiving target = Body.TargetObject as GameLiving;
                if (target != null)
                {
                    Body.SetGroundTarget(target.X, target.Y, target.Z);
                    Body.CastSpell(TineAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
                }
            }
            base.Think();
        }
        private Spell m_TineAoe;
        private Spell TineAoe
        {
            get
            {
                if (m_TineAoe == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 8;
                    spell.ClientEffect = 4227;
                    spell.Icon = 4227;
                    spell.Damage = 450;
                    spell.Name = "Tine's Fire";
                    spell.TooltipId = 4227;
                    spell.Range = 3000;
                    spell.Radius = 450;
                    spell.SpellID = 11708;
                    spell.Target = "Area";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Heat;
                    m_TineAoe = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TineAoe);
                }
                return m_TineAoe;
            }
        }
    }
}
#endregion