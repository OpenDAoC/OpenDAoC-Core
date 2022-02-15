/*
AI for Aros The Spiritmaster like NPCs.
<author>Kelt</author>
 */
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.Scripts;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DOL.AI.Brain
{
    public class ArosBrain : StandardMobBrain
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Create a new ArosBrain.
        /// </summary>
        public ArosBrain()
            : base()
        {
            AggroLevel = 200;
            AggroRange = 500;
            ThinkInterval = 1500;

            FSM.ClearStates();

            FSM.Add(new StandardMobState_WAKING_UP(FSM, this));
            FSM.Add(new ArosState_RETURN_TO_SPAWN(FSM, this));
            FSM.Add(new ArosState_IDLE(FSM, this));
            FSM.Add(new ArosState_AGGRO(FSM, this));
            FSM.Add(new StandardMobState_DEAD(FSM, this));

            FSM.SetCurrentState(eFSMStateType.WAKING_UP);
        }

        /// <summary>
        /// The brain main loop. Do necessary health checks first and take
        /// any actions, if necessary. If everything's fine either pick a
        /// player to Glare at or to throw around.
        /// </summary>
        public override void Think()
        {
            Resists();
            ResistsTwo();
            FSM.Think();
        }

        public void Resists()
        {
            int m_value = 100;
            int min_value = 35;
            ushort minradius = 300;

            GameNPC nearbyGuardian = null;

            foreach (GameNPC npc in this.Body.GetNPCsInRadius(minradius))
            {
                if (npc.Name.Equals("Summoned Guardian"))
                {
                    nearbyGuardian = npc;
                    break;
                }
            }
            if (nearbyGuardian != null)
            {
                Body.AbilityBonus[(int)eProperty.Resist_Body] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Heat] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Cold] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Matter] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Energy] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Spirit] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Slash] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Crush] = m_value;
                Body.AbilityBonus[(int)eProperty.Resist_Thrust] = m_value;
            }
            else
            {
                Body.AbilityBonus[(int)eProperty.Resist_Body] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Heat] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Cold] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Matter] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Energy] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Spirit] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Slash] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Crush] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Thrust] = min_value;
            }
        }

        public void ResistsTwo()
        {
            int summonedValue = 1000;
            int min_value = 30;
            ushort radius = 3000;

            GameNPC summonedGuardian = null;
            foreach (GameNPC summonNpc in this.Body.GetNPCsInRadius(radius))
            {
                if (summonNpc.Name.Equals("Guardian of Aros"))
                {
                    summonedGuardian = summonNpc;
                    break;
                }

            }
            if (summonedGuardian != null)
            {
                Body.AbilityBonus[(int)eProperty.Resist_Body] = summonedValue;
                Body.AbilityBonus[(int)eProperty.Resist_Heat] = summonedValue;
                Body.AbilityBonus[(int)eProperty.Resist_Cold] = summonedValue;
                Body.AbilityBonus[(int)eProperty.Resist_Matter] = summonedValue;
                Body.AbilityBonus[(int)eProperty.Resist_Energy] = summonedValue;
                Body.AbilityBonus[(int)eProperty.Resist_Spirit] = summonedValue;
                Body.AbilityBonus[(int)eProperty.Resist_Slash] = summonedValue;
                Body.AbilityBonus[(int)eProperty.Resist_Crush] = summonedValue;
                Body.AbilityBonus[(int)eProperty.Resist_Thrust] = summonedValue;
                Body.AbilityBonus[(int)eProperty.MagicAbsorption] = summonedValue + 100;
                Body.AbilityBonus[(int)eProperty.ArmorAbsorption] = summonedValue + 100;
                Body.AbilityBonus[(int)eProperty.StyleAbsorb] = summonedValue + 100;
            }
            else
            {
                Body.AbilityBonus[(int)eProperty.Resist_Body] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Heat] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Cold] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Matter] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Energy] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Spirit] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Slash] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Crush] = min_value;
                Body.AbilityBonus[(int)eProperty.Resist_Thrust] = min_value;
                Body.AbilityBonus[(int)eProperty.MagicAbsorption] = 10;
                Body.AbilityBonus[(int)eProperty.ArmorAbsorption] = 10;
                Body.AbilityBonus[(int)eProperty.StyleAbsorb] = 10;
            }
        }
        public override void CheckNPCAggro()
        {
            if (Body.attackComponent.AttackState)
                return;

            foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange))
            {
                if (!npc.IsAlive || npc.ObjectState != GameObject.eObjectState.Active)
                    continue;

                if (!GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
                    continue;

                if (m_aggroTable.ContainsKey(npc))
                    continue; // add only new NPCs

                if (npc.Brain != null && npc.Brain is IControlledBrain)
                {
                    if (CalculateAggroLevelToTarget(npc) > 0)
                    {
                        AddToAggroList(npc, (npc.Level + 1) << 1);
                    }
                }
            }
        }
        
        /// <summary>
        /// Called whenever Aros the Spiritmaster's body sends something to its brain.
        /// </summary>
        /// <param name="e">The event that occured.</param>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The event details.</param>
        public override void Notify(DOL.Events.DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);
            if (sender == Body)
            {
                GameEpicAros aros = sender as GameEpicAros;
                if (e == GameObjectEvent.TakeDamage)
                {
                    if (CheckHealth()) return;
                    // Someone hit Aros the Spiritmaster. If the attacker is in melee range, there
                    // is a chance Aros will cast a Bomb + Debuff
                }
                else if (e == GameLivingEvent.EnemyHealed)
                {
                }

            }
        }

        #region Tether

        /// <summary>
        /// Check whether Aros the Spiritmaster is out of tether range.
        /// </summary>
        /// <returns>True if Aros has reached the end of its tether.</returns>
        public bool CheckTether()
        {
            GameEpicAros aros = Body as GameEpicAros;
            if (aros == null) return false;
            return !aros.IsWithinRadius(aros.SpawnPoint, aros.TetherRange);
        }

        #endregion

        #region Debuff

        /// <summary>
        /// Try to find a potential target for Debuff.
        /// </summary>
        /// <returns>Whether or not a target was picked.</returns>
        public bool PickDebuffTarget()
        {
            GameEpicAros aros = Body as GameEpicAros;
            if (aros == null) return false;

            ArrayList inRangeLiving = new ArrayList();

            lock ((m_aggroTable as ICollection).SyncRoot)
            {
                Dictionary<GameLiving, long>.Enumerator enumerator = m_aggroTable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    GameLiving living = enumerator.Current.Key;
                    if (living != null &&
                        living.IsAlive &&
                        living.EffectList.GetOfType<NecromancerShadeEffect>() == null &&
                        !aros.IsWithinRadius(living, aros.AttackRange))
                    {
                        inRangeLiving.Add(living);
                    }
                }
            }

            if (inRangeLiving.Count > 0)
            {
                return aros.CheckDebuff((GameLiving)(inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
            }

            return false;
        }

        #endregion


        #region Health Check

        private int m_stage = 10;
        private int m_stageTwo = 100;

        /// <summary>
        /// This keeps track of the stage the encounter is in, so players
        /// don't have to go through all the PBAoE etc. again, just because
        /// Aros the Spiritmaster regains a small amount of health. Starts at 10 (full
        /// health) and drops to 0.
        /// </summary>
        public int Stage
        {
            get { return m_stage; }
            set { if (value >= 0 && value <= 10) m_stage = value; }
        }

        /// <summary>
        /// This keeps track of the stage the encounter is in, so players
        /// don't have to go through all the PBAoE etc. again, just because
        /// Aros the Spiritmaster regains a small amount of health. Starts at 100 (full
        /// health) and drops to 0.
        /// </summary>
        public int StageTwo
        {
            get { return m_stageTwo; }
            set { if (value >= 0 && value <= 10) m_stageTwo = value; }
        }

        /// <summary>
        /// Actions to be taken into consideration when health drops.
        /// </summary>
        /// <returns>Whether any action was taken.</returns>
        public bool CheckHealth()
        {
            GameEpicAros aros = Body as GameEpicAros;
            if (aros == null) return false;

            int healthOld = aros.HealthPercentOld / 10;
            int healthNow = aros.HealthPercent / 10;
            //int healthOldTwo = aros.HealthPercentOld;
            //int healthNowTwo = aros.HealthPercent;

            if (healthNow < healthOld && Stage > healthNow)
            {
                Stage = healthNow;

                // Bomb at 89%/79%/69%/59%/39%/29% and 9%.

                switch (healthNow)
                {
                    case 9:
                        if (aros.CheckSummon())
                            return true;                      
                        break;
                    case 8:
                        if (aros.CheckBomb())
                            return true;
                        break;
                    case 7:
                        if (aros.CheckBomb())
                            return true;                  
                        break;
                    case 6:
                        if (aros.CheckBomb())
                            return true;                      
                        break;
                    case 5:
                        if (aros.CheckBomb())
                            return true;
                        break;
                    case 4:
                        // Big Bomb when health drops below 50%.
                        if (aros.CheckBigBomb())
                            return true;                        
                        break;
                    case 3:
                        if (aros.CheckBomb())
                            return true;
                        break;
                    case 2:
                        if (aros.CheckBomb())
                            return true;
                        break;
                    case 1:
                        // Big Bomb when health drops below 20%.
                        if (aros.CheckBigBomb())
                            return true;
                        break;
                    case 0:
                        if (aros.CheckBomb())
                            return true;
                        break;
                    default:
                        break;
                }
            }
            return false;
        }
        #endregion
    }
}
