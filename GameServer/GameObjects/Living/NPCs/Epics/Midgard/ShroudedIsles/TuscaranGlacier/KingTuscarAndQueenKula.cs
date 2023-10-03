﻿using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.Styles;

namespace DOL.GS
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
            NewsMgr.CreateNews(message, killer.Realm, eNewsType.PvE, true);

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
                player.Achieve(AchievementUtils.AchievementNames.Epic_Boss_Kills);
                count++;
            }
            return count;
        }
        #endregion
        #region Resists & TakeDamage()
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
            if (source is GamePlayer || source is GameSummonedPet)
            {
                if (IsOutOfTetherRange)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GameSummonedPet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
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


                    foreach (GameNPC npc in this.GetNPCsInRadius(5000))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive)
                            {
                                if (npc.Brain is KingTuscarBrain && npc.HealthPercent < 100)
                                {
                                    npc.Health += damageAmount + criticalAmount;
                                    if (truc != null)
                                        truc.Out.SendMessage("Your damage is healing King Tuscar!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
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
            get { return 300000; }
        }
        #region BroadcastMessage & Die()
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
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
                    if (player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
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
            BodyType = (ushort)NpcTemplateMgr.eBodyType.Giant;
            if (!Styles.Contains(taunt))
                Styles.Add(taunt);
            ++QueenKulaCount;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 316, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.Standard);
            QueenKulaBrain.IsTargetPicked = false;
            QueenKulaBrain.message1 = false;
            QueenKulaBrain.IsPulled1 = false;

            VisibleActiveWeaponSlots = 16;
            MeleeDamageType = eDamageType.Slash;
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
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Energy;
                    m_Cold_DD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Cold_DD);
                }
                return m_Cold_DD;
            }
        }
        #endregion
    }
}
namespace DOL.AI.Brain
{
    public class QueenKulaBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public QueenKulaBrain()
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
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        #region Teleport Player & PlayerInCenter()
        public static GamePlayer randomtarget = null;
        public static GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public static bool IsTargetPicked = false;
        List<GamePlayer> Port_Enemys = new List<GamePlayer>();
        public int PickPlayer(ECSGameTimer timer)
        {
            if (Body.IsAlive)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!Port_Enemys.Contains(player))
                            {
                                if (!player.effectListComponent.ContainsEffectForEffectType(eEffect.Mez) && !player.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff)
                                    && (!player.effectListComponent.ContainsEffectForEffectType(eEffect.MezImmunity) || !player.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity))
                                    && player != Body.TargetObject)
                                {
                                    Port_Enemys.Add(player);
                                }
                            }
                        }
                    }
                }
                if (Port_Enemys.Count == 0)
                {
                    RandomTarget = null;//reset random target to null
                    IsTargetPicked = false;
                }
                else
                {
                    if (Port_Enemys.Count > 0)
                    {
                        GamePlayer Target = (GamePlayer)Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
                        RandomTarget = Target;
                        if (RandomTarget.IsAlive && RandomTarget != null)
                        {
                            RandomTarget.MoveTo(160, 34128, 56095, 11898, 2124);
                            Port_Enemys.Remove(RandomTarget);
                            RandomTarget = null;//reset random target to null
                            IsTargetPicked = false;
                        }
                    }
                }
            }
            return 0;
        }
        public static bool message1 = false;
        public void PlayerInCenter()
        {
            Point3D FrostPoint = new Point3D();
            FrostPoint.X = 34128; FrostPoint.Y = 56095; FrostPoint.Z = 11989;
            foreach(GamePlayer player in Body.GetPlayersInRadius(8000))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.IsWithinRadius(FrostPoint, 300))
                    {
                        GameLiving oldTarget = Body.TargetObject as GameLiving;//old target
                        Body.TargetObject = player;//set target to randomly picked
                        switch (Util.Random(1, 2))
                        {
                            case 1:
                                {//check here if target is not already mezzed or rotted or got mezzimmunity
                                    if (!player.effectListComponent.ContainsEffectForEffectType(eEffect.Mez) && !player.effectListComponent.ContainsEffectForEffectType(eEffect.MezImmunity)
                                    && !player.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff))
                                    {
                                        Body.CastSpell(Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//cast mezz
                                    }
                                }
                                break;
                            case 2:
                                {//check here if target is not mezzed already or rooted or got snare immunity
                                    if (!player.effectListComponent.ContainsEffectForEffectType(eEffect.Mez) && !player.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff) 
                                    && !player.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity))
                                    {
                                        Body.CastSpell(Root, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//cast root
                                    }
                                }
                                break;
                        }
                        if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
                        Body.StartAttack(oldTarget);//start attack old target
                    }
                }
            }
        }
        #endregion
        #region OnAttackedByEnemy()
        public static bool IsPulled1 = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (HasAggro && Body.TargetObject != null)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (npc.IsAlive && npc.Brain is KingTuscarBrain brain)
                        {
                            if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
                                brain.AddToAggroList(target, 10);
                        }
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }
        #endregion
        #region Think()
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165083);
                Body.Strength = npcTemplate.Strength;
                IsTargetPicked =false;
                IsPulled1 = false;
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
                message1 = false;
            }
            if (Body.TargetObject != null && HasAggro)
            {
                PlayerInCenter();//method that check if player enter to frozen circle
                if (message1 == false)
                {
                    BroadcastMessage(String.Format("Queen Kula grins maliciously, 'So you got past all my Hrimthursa Guardians!" +
                        " These Hrimthursa are useless and arrogant! I'm going to show you what I've been wanting to teach you for a long time." +
                        " The merciless who are not afraid of death will survive in this brutal world! I am merciless I'm not afraid of death!'"));
                    message1 = true;
                }
                if (IsTargetPicked == false)
                {
                    if (KingTuscar.KingTuscarCount == 1)
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickPlayer), Util.Random(15000, 25000));//timer to port and pick player
                    }
                    else if(KingTuscar.KingTuscarCount == 0)
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickPlayer), Util.Random(8000, 12000));//timer to port and pick player
                    }
                    IsTargetPicked = true;
                }
                if(Body.TargetObject != null)
                {
                    if (Body.TargetObject is GamePlayer)
                    {
                        GamePlayer player = Body.TargetObject as GamePlayer;
                        if (player.effectListComponent.ContainsEffectForEffectType(eEffect.Mez))
                        {
                            RemoveFromAggroList(player);
                        }
                        if (player.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff))
                        {
                            RemoveFromAggroList(player);
                        }
                    }
                    if (KingTuscar.KingTuscarCount == 1)
                    {
                        Body.Strength = 350;//if king is up it will deal less dmg
                    }
                    if (KingTuscar.KingTuscarCount == 0 || Body.HealthPercent <= 50)
                    {
                        Body.Strength = 500;//king is dead so more dmg
                    }
                    Body.styleComponent.NextCombatStyle = QueenKula.taunt;
                }
            }
            base.Think();
        }
        #endregion
        #region Spells
        protected Spell m_mezSpell;
        protected Spell Mezz
        {
            get
            {
                if (m_mezSpell == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 5;
                    spell.ClientEffect = 3308;
                    spell.Icon = 3308;
                    spell.Name = "Mesmerize";
                    spell.Description = "Target is mesmerized and cannot move or take any other action for the duration of the spell. If the target suffers any damage or other negative effect the spell will break.";
                    spell.TooltipId = 3308;
                    spell.Range = 1500;
                    spell.SpellID = 11750;
                    spell.Duration = 80;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = "Mesmerize";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Spirit; //Spirit DMG Type
                    m_mezSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_mezSpell);
                }
                return m_mezSpell;
            }
        }
        protected Spell m_RootSpell;
        protected Spell Root
        {
            get
            {
                if (m_RootSpell == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 5;
                    spell.ClientEffect = 177;
                    spell.Icon = 177;
                    spell.Name = "Ice Touch";
                    spell.Description = "Target is rooted in place for the spell's duration.";
                    spell.TooltipId = 177;
                    spell.Value = 99;
                    spell.Range = 1500;
                    spell.SpellID = 11751;
                    spell.Duration = 80;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.SpeedDecrease.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Spirit; //Spirit DMG Type
                    m_RootSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RootSpell);
                }
                return m_RootSpell;
            }
        }
        #endregion
    }
}
///////////////////////////////////////////////////////////////////King Tuscar////////////////////////////////////////////////////////////
namespace DOL.GS
{
    public class KingTuscar : GameEpicBoss
    {
        public KingTuscar() : base() { }
        #region Styles declaration
        public static int TauntID = 167;
        public static int TauntClassID = 22;//warrior
        public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

        public static int AfterParryID = 173;
        public static int AfterParryClassID = 22;
        public static Style after_parry = SkillBase.GetStyleByID(AfterParryID, AfterParryClassID);

        public static int ParryFollowupID = 175;
        public static int ParryFollowupClassID = 22;
        public static Style parry_followup = SkillBase.GetStyleByID(ParryFollowupID, ParryFollowupClassID);

        public static int AfterBlockID = 302;
        public static int AfterBlockClassID = 44;
        public static Style after_block = SkillBase.GetStyleByID(AfterBlockID, AfterBlockClassID);
        #endregion
        #region Resists and TakeDamage()
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
            if (source is GamePlayer || source is GameSummonedPet)
            {
                if (IsOutOfTetherRange)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GameSummonedPet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
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
                    
                    foreach (GameNPC npc in GetNPCsInRadius(5000))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive)
                            {
                                if (npc.Brain is QueenKulaBrain && npc.HealthPercent < 100)
                                {
                                    npc.Health += damageAmount + criticalAmount;
                                    if (truc != null)
                                        truc.Out.SendMessage("Your damage is healing Queen Kula!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
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
            get { return 300000; }
        }
        public static int KingTuscarCount = 0;
        public override void Die(GameObject killer)//on kill generate orbs
        {
            --KingTuscarCount;
            base.Die(killer);
        }
        #region Styles
        public override void OnAttackedByEnemy(AttackData ad)// on Boss actions
        {
            if(ad != null && ad.AttackResult == eAttackResult.Parried)
            {
                styleComponent.NextCombatBackupStyle = after_parry;//boss parried so prepare after parry style backup style
                styleComponent.NextCombatStyle = parry_followup;//main style after parry followup
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad)//on enemy actions
        {
            if (ad != null && ad.AttackResult == eAttackResult.HitStyle)
            {
                styleComponent.NextCombatBackupStyle = taunt;//taunt as backup style
                styleComponent.NextCombatStyle = parry_followup;//after parry style as main
            }
            if (ad != null && ad.AttackResult == eAttackResult.HitUnstyled)
            {
                styleComponent.NextCombatStyle = taunt;//boss hit unstyled so taunt
            }
            if (ad != null && ad.AttackResult == eAttackResult.Blocked)
            {
                styleComponent.NextCombatStyle = after_block;//target blocked boss attack so use after block style
                if(Util.Chance(50))
                    CastSpell(Hammers_aoe2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//aoe mjolnirs after style big dmg
            }
            if (ad != null && ad.AttackResult == eAttackResult.Parried)
            {
                if (Util.Chance(50))
                    CastSpell(Thunder_aoe2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//aoe mjolnirs after style big dmg
            }
            if (QueenKula.QueenKulaCount == 0 || (HealthPercent <= 50 && KingTuscarBrain.TuscarRage==true))
            {
                if (ad.AttackResult == eAttackResult.HitStyle && ad.Style.ID == 175 && ad.Style.ClassID == 22)
                {
                    CastSpell(Hammers_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//aoe mjolnirs after style big dmg
                }
                if (ad.AttackResult == eAttackResult.HitStyle && ad.Style.ID == 302 && ad.Style.ClassID == 44)
                {
                    CastSpell(Thunder_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//aoe lightining after style medium dmg
                }
                if (ad.AttackResult == eAttackResult.HitStyle && ad.Style.ID == 173 && ad.Style.ClassID == 22)
                {
                    CastSpell(Bleed, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//bleed after style low dot bleed dmg
                }
            }
            base.OnAttackEnemy(ad);
        }
        #endregion
        #region AddToWorld
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162909);
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
            BodyType = (ushort)NpcTemplateMgr.eBodyType.Giant;
            if(!Styles.Contains(taunt))
                Styles.Add(taunt);
            if (!Styles.Contains(after_parry))
                Styles.Add(after_parry);
            if (!Styles.Contains(parry_followup))
                Styles.Add(parry_followup);
            if (!Styles.Contains(after_block))
                Styles.Add(after_block);
            ++KingTuscarCount;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 575, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            KingTuscarBrain.message2 = false;
            KingTuscarBrain.TuscarRage = false;
            KingTuscarBrain.IsPulled2 = false;

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            KingTuscarBrain sbrain = new KingTuscarBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false;//load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }       
        #endregion
        #region Spells
        private Spell m_Hammers_aoe;
        private Spell Hammers_aoe
        {
            get
            {
                if (m_Hammers_aoe == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 3;
                    spell.ClientEffect = 3541;
                    spell.Icon = 3541;
                    spell.TooltipId = 3541;
                    spell.Damage = 600;
                    spell.Name = "Mjolnir's Fury";
                    spell.Radius = 500;
                    spell.Range = 350;
                    spell.SpellID = 11752;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Energy;
                    m_Hammers_aoe = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hammers_aoe);
                }
                return m_Hammers_aoe;
            }
        }
        private Spell m_Thunder_aoe;
        private Spell Thunder_aoe
        {
            get
            {
                if (m_Thunder_aoe == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 2;
                    spell.ClientEffect = 3528;
                    spell.Icon = 3528;
                    spell.TooltipId = 3528;
                    spell.Damage = 350;
                    spell.Name = "Thor's Might";
                    spell.Radius = 500;
                    spell.Range = 350;
                    spell.SpellID = 11753;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Energy;
                    m_Thunder_aoe = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Thunder_aoe);
                }
                return m_Thunder_aoe;
            }
        }
        private Spell m_Hammers_aoe2;
        private Spell Hammers_aoe2
        {
            get
            {
                if (m_Hammers_aoe2 == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 5;
                    spell.ClientEffect = 3541;
                    spell.Icon = 3541;
                    spell.TooltipId = 3541;
                    spell.Damage = 500;
                    spell.Name = "Mjolnir's Fury";
                    spell.Radius = 500;
                    spell.Range = 350;
                    spell.SpellID = 11890;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Energy;
                    m_Hammers_aoe2 = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hammers_aoe2);
                }
                return m_Hammers_aoe2;
            }
        }
        private Spell m_Thunder_aoe2;
        private Spell Thunder_aoe2
        {
            get
            {
                if (m_Thunder_aoe2 == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 5;
                    spell.ClientEffect = 3528;
                    spell.Icon = 3528;
                    spell.TooltipId = 3528;
                    spell.Damage = 400;
                    spell.Name = "Thor's Might";
                    spell.Radius = 500;
                    spell.Range = 350;
                    spell.SpellID = 11891;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Energy;
                    m_Thunder_aoe2 = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Thunder_aoe2);
                }
                return m_Thunder_aoe2;
            }
        }
        private Spell m_Bleed;
        private Spell Bleed
        {
            get
            {
                if (m_Bleed == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 2;
                    spell.ClientEffect = 2130;
                    spell.Icon = 3411;
                    spell.TooltipId = 3411;
                    spell.Damage = 65;
                    spell.Name = "Scar of Gods";
                    spell.Description = "Does 65 damage to a target every 3 seconds for 36 seconds.";
                    spell.Message1 = "You are bleeding! ";
                    spell.Message2 = "{0} is bleeding! ";
                    spell.Duration = 36;
                    spell.Frequency = 30;
                    spell.Range = 350;
                    spell.SpellID = 11754;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.StyleBleeding.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Body;
                    m_Bleed = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bleed);
                }
                return m_Bleed;
            }
        }
        #endregion
    }
}
namespace DOL.AI.Brain
{
    public class KingTuscarBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public KingTuscarBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 1500;
        }
        #region BroadcastMessage & OnAttackedByEnemy()
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public static bool message2 = false;
        public static bool TuscarRage = false;
        public static bool IsPulled2 = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (HasAggro && Body.TargetObject != null)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (npc.IsAlive && npc.Brain is QueenKulaBrain brain)
                        {
                            if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
                                brain.AddToAggroList(target, 10);
                        }
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }
        #endregion
        #region Think()
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162909);
                Body.Strength = npcTemplate.Strength;
                TuscarRage = false;
                IsPulled2 = false;
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
                message2 = false;
            }
            if (Body.InCombat && HasAggro)
            {
                if (message2 == false)
                {
                    BroadcastMessage(String.Format("King Tuscar raises his weapon and yells, 'Kula wields the finest weapon I have ever made!" +
                        " And the weapon I forged for myself is almost as good in combat! Death comes swiftly with these two weapons!'"));
                    message2 = true;
                }
                if(Body.HealthPercent<=50 && TuscarRage==false)
                {
                    BroadcastMessage(String.Format("King Tuscar rages and gains strength from Odin!"));
                    TuscarRage = true;
                }
                if (Body.TargetObject != null)
                {
                    GameLiving living = Body.TargetObject as GameLiving;
                    if(QueenKula.QueenKulaCount == 1)
                    {
                        Body.Strength = 350;
                    }
                    if (QueenKula.QueenKulaCount == 0 || Body.HealthPercent <= 50)
                    {
                        Body.Strength = 500;
                    }
                }
            }
            base.Think();
        }
        #endregion
    }
}
