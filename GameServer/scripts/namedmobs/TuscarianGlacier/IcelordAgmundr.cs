using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Agmundr : GameEpicBoss
    {
        public Agmundr() : base()
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

        public override double GetArmorAF(eArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
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
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162346);
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

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 19, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;

            AgmundrBrain.IsChanged = false;
            AgmundrBrain.IsPulled = false;
            AgmundrBrain sbrain = new AgmundrBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
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
            BroadcastMessage(String.Format("To come this far... only to face a terrible death!"));
            base.Die(killer);
        }
    }
}

namespace DOL.AI.Brain
{
    public class AgmundrBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AgmundrBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 2000;
        }

        public static bool IsPulled = false;
        public static bool IsChanged = false;
        private bool PulledText = false;
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if(!PulledText && Body.TargetObject != null)
            {
                BroadcastMessage(String.Format("My seer's told me that you were coming {0}! Since you posed no threat I haven't asked for reinforcements!", Body.TargetObject.Name));
                PulledText = true;
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void AttackMostWanted()
        {
            if (Util.Chance(15) && Body.TargetObject != null)
                Body.CastSpell(AgmundrDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
            base.AttackMostWanted();
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                IsPulled = false;
                PulledText = false;
            }
            if(HasAggro && Body.TargetObject != null)
            {
                IsChanged = false;//reset IsChanged flag here
                if(IsPulled==false)
                { 
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc == null) continue;
                        if (!npc.IsAlive || npc.PackageID != "AgmundrBaf") continue;
                        AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with CryptLordBaf PackageID
                        SetMobstats();//setting mob stats here
                    }
                }
            }
            else
            {
                if (IsChanged == false)
                {
                    LoadBAFTemplate();
                    IsChanged = true;//to stop mob 'blink' effect
                }
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            base.Think();
        }

        private void SetMobstats()
        {
            foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc == null) continue;
                if (npc.NPCTemplate == null) continue;//check for nontemplated mobs
                if (!npc.IsAlive || npc.PackageID != "AgmundrBaf") continue;
                if (npc.TargetObject != Body.TargetObject) continue;
                npc.MaxDistance = 10000; //set mob distance to make it reach target
                npc.TetherRange = 10000; //set tether to not return to home
                if (!npc.IsWithinRadius(Body.TargetObject, 100))
                {
                    npc.MaxSpeedBase = 300; //speed is is not near to reach target faster
                }
                else
                {
                    npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed; //return speed to normal
                    IsPulled = true;//to stop mob adjusting stats nonstop
                }
            }
        }
        private void LoadBAFTemplate()
        {
            foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc == null) continue;
                if (npc.NPCTemplate == null) continue;//check if mob got npctemplate
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(npc.NPCTemplate.TemplateId);
                if (npcTemplate == null)
                    return;
                if (!npc.IsAlive || npc.PackageID != "AgmundrBaf") continue;
                if (npc.NPCTemplate != null)//check again if got npctemplate
                {
                    npc.LoadTemplate(npcTemplate);
                }
            }
        }
        private Spell m_AgmundrDD;
        private Spell AgmundrDD
        {
            get
            {
                if (m_AgmundrDD != null) return m_AgmundrDD;
                DBSpell spell = new DBSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = Util.Random(10, 15);
                spell.ClientEffect = 228;
                spell.Icon = 208;
                spell.TooltipId = 479;
                spell.Damage = 650;
                spell.Range = 1500;
                spell.Radius = 500;
                spell.SpellID = 11744;
                spell.Target = "Enemy";
                spell.Type = "DirectDamageNoVariance";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) eDamageType.Cold;
                m_AgmundrDD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AgmundrDD);
                return m_AgmundrDD;
            }
        }
    }
}