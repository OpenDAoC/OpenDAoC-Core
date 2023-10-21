using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.ServerProperties;
using Core.GS.Styles;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;

namespace Core.GS
{
    public class QueenKula : GameEpicBoss
    {
        public QueenKula() : base() { }
        public static int TauntID = 178;
        public static int TauntClassID = 23;
        public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);
        #region Award Epic Encounter Kill
        protected void ReportNews(GameObject killer)
        {
            int numPlayers = AwardEpicEncounterKillPoint();
            String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
            NewsMgr.CreateNews(message, killer.Realm, ENewsType.PvE, true);

            if (Properties.GUILD_MERIT_ON_DRAGON_KILL > 0)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player.IsEligibleToGiveMeritPoints)
                    {
                        GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_DRAGON_KILL);
                    }
                }
            }
        }
        protected int AwardEpicEncounterKillPoint()
        {
            int count = 0;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.KillsEpicBoss++;
                player.Achieve(AchievementUtil.AchievementName.Epic_Boss_Kills);
                count++;
            }
            return count;
        }
        #endregion
        #region Resists & TakeDamage()
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
                if (IsOutOfTetherRange)
                {
                    if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
                        || damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                        || damageType == EDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GameSummonedPet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                }
                else
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);


                    foreach (GameNpc npc in this.GetNPCsInRadius(5000))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive)
                            {
                                if (npc.Brain is KingTuscarBrain && npc.HealthPercent < 100)
                                {
                                    npc.Health += damageAmount + criticalAmount;
                                    if (truc != null)
                                        truc.Out.SendMessage("Your damage is healing King Tuscar!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
                                }
                            }
                        }
                    }
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        #endregion
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
            get { return 300000; }
        }
        #region BroadcastMessage & Die()
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
            }
        }
        public static int QueenKulaCount = 0;
        public override void Die(GameObject killer)//on kill generate orbs
        {
            if(KingTuscar.KingTuscarCount > 0)
                BroadcastMessage(String.Format("As the Queen Kula dies, King Tuscar scream in rage and gather more strength!"));

            --QueenKulaCount;
            bool canReportNews = true;
            // due to issues with attackers the following code will send a notify to all in area in order to force quest credit
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

                if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
                {
                    if (player.Client.Account.PrivLevel == (int)EPrivLevel.Player)
                        canReportNews = false;
                }
            }
            if (canReportNews)
            {
                ReportNews(killer);
            }
            base.Die(killer);
        }
        #endregion
        #region AddToWorld
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165083); 
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
            RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
            BodyType = (ushort)EBodyType.Giant;
            if (!Styles.Contains(taunt))
                Styles.Add(taunt);
            ++QueenKulaCount;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 316, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.Standard);
            QueenKulaBrain.IsTargetPicked = false;
            QueenKulaBrain.message1 = false;
            QueenKulaBrain.IsPulled1 = false;

            VisibleActiveWeaponSlots = 16;
            MeleeDamageType = EDamageType.Slash;
            QueenKulaBrain sbrain = new QueenKulaBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false;//load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
        #endregion

        public override void OnAttackedByEnemy(AttackData ad)// on Boss actions
        {
            if (ad != null && ad.Damage > 0 && ad.Attacker != null && ad.Attacker.IsAlive && ad.Attacker is GamePlayer)
            {
                if(KingTuscar.KingTuscarCount == 0 || HealthPercent <= 50)
                {
                    if (Util.Chance(50))
                        CastSpell(Cold_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
                if (KingTuscar.KingTuscarCount > 0 || HealthPercent > 50)
                    if (Util.Chance(10))
                    CastSpell(Cold_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.OnAttackedByEnemy(ad);
        }
        #region Spells
        private Spell m_Cold_DD;
        private Spell Cold_DD
        {
            get
            {
                if (m_Cold_DD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 2;
                    spell.ClientEffect = 4075;
                    spell.Icon = 4075;
                    spell.TooltipId = 4075;
                    spell.Damage = 400;
                    spell.Name = "Thor's Might";
                    spell.Range = 2500;
                    spell.SpellID = 11892;
                    spell.Target = ESpellTarget.ENEMY.ToString();
                    spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Energy;
                    m_Cold_DD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Cold_DD);
                }
                return m_Cold_DD;
            }
        }
        #endregion
    }
}