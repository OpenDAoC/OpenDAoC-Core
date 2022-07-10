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
    public class MoranTheMighty : GameEpicBoss
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MoranTheMighty()
            : base()
        {
        }
        
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40; // dmg reduction for melee dmg
                case eDamageType.Crush: return 40; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 70; // dmg reduction for rest resists
            }
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
            get { return 40000; }
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
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(770024);
            LoadTemplate(npcTemplate);
            
            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;

            // giant
            BodyType = 5;
            Race = 2004;
            Size = 230;

            MaxDistance = 3000;
            TetherRange = 4500;
            
            Faction = FactionMgr.GetFactionByID(31);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(31));
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

            MoranBrain sBrain = new MoranBrain();
            MoranBrain._aggroStart = true;
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class MoranBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool _aggroStart = true;
        
        public MoranBrain()
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

        public override void Think()
        {
            if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
            {
                Body.Health = Body.MaxHealth;
                Body.WalkToSpawn();
            }

            if (HasAggro && Body.InCombat)
            {
                if (Body.TargetObject != null)
                {
                    if (_aggroStart)
                    {
                        foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
                        {
                            foreach (GamePlayer player in Body.GetPlayersInRadius(2250))
                            {
                                npc.StartAttack(player);
                            }
                        }

                        _aggroStart = false;
                    }
                    else
                    {
                        // chance to teleport a random player to another mob camp 
                        if (Util.Chance(5) && Body.HealthPercent <= 50)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(TeleportPlayerAway), 5000);
                        }
                    }
                }
            }
            else
            {
                _aggroStart = true;
            }

            base.Think();
        }

        public int TeleportPlayerAway(ECSGameTimer timer)
        {
            string gender;
            foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
            {
                if (player == null)
                    return 0;

                List<GamePlayer> portPlayer = new List<GamePlayer>();
                portPlayer.Add(player);
                int ranPlayer = Util.Random(0, portPlayer.Count - 1);
                
                if (player.IsAlive && ranPlayer >= 0)
                {
                    switch(portPlayer[ranPlayer].Gender)
                    {
                        case eGender.Female:
                            gender = "her";
                            break;
                        case eGender.Male:
                            gender = "him";
                            break;
                        case eGender.Neutral:
                            gender = "it";
                            break;
                        default:
                            gender = "it";
                            break;
                    }
                    
                    switch (Util.Random(1, 3))
                    {
                        case 1:
                            BroadcastMessage(String.Format("{0} picked up {1} on gust of winds and tossed {2} away! ", Body.Name, portPlayer[ranPlayer].Name, gender));
                            portPlayer[ranPlayer].Out.SendSpellEffectAnimation(portPlayer[ranPlayer], portPlayer[ranPlayer], 1735, 0, false, 1);
                            portPlayer[ranPlayer].MoveTo(1, 401943, 753091, 222, 3499);
                            foreach (GameNPC npc in portPlayer[ranPlayer].GetNPCsInRadius(2000))
                            {
                                npc.StartAttack(portPlayer[ranPlayer]);
                            }
                            portPlayer.Clear();
                            break;
                        case 2:
                            BroadcastMessage(String.Format("{0} picked up {1} on gust of winds and tossed {2} away! ", Body.Name, portPlayer[ranPlayer].Name, gender));
                            portPlayer[ranPlayer].Out.SendSpellEffectAnimation(portPlayer[ranPlayer], portPlayer[ranPlayer], 1735, 0, false, 1);
                            portPlayer[ranPlayer].MoveTo(1, 406787, 749150, 213, 3926);
                            foreach (GameNPC npc in portPlayer[ranPlayer].GetNPCsInRadius(2000))
                            {
                                npc.StartAttack(portPlayer[ranPlayer]);
                            }
                            portPlayer.Clear();
                            break;
                        case 3:
                            BroadcastMessage(String.Format("{0} picked up {1} on gust of winds and tossed {2} away! ", Body.Name, portPlayer[ranPlayer].Name, gender));
                            portPlayer[ranPlayer].Out.SendSpellEffectAnimation(portPlayer[ranPlayer], portPlayer[ranPlayer], 1735, 0, false, 1);
                            portPlayer[ranPlayer].MoveTo(1, 401061, 755882, 469, 3050);
                            foreach (GameNPC npc in portPlayer[ranPlayer].GetNPCsInRadius(2000))
                            {
                                npc.StartAttack(portPlayer[ranPlayer]);
                            }
                            portPlayer.Clear();
                            break;
                        default:
                            break;
                    }
                }
                portPlayer.Clear();
            }
            return 0;
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);
        }
    }
}