/*
AI for Mistress of Runes like NPCs.
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
    public class MistressBrain : StandardMobBrain
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Create a new MistressBrain.
        /// </summary>
        public MistressBrain()
            : base()
        {
            
            AggroLevel = 200;
            AggroRange = 500;
            ThinkInterval = 10000;

            FSM.ClearStates();

            FSM.Add(new StandardMobState_WAKING_UP(FSM, this));
            FSM.Add(new MistressState_RETURN_TO_SPAWN(FSM, this));
            FSM.Add(new MistressState_IDLE(FSM, this));
            FSM.Add(new MistressState_AGGRO(FSM, this));
            FSM.Add(new StandardMobState_DEAD(FSM, this));

            FSM.SetCurrentState(eFSMStateType.WAKING_UP);
        }



        /// <summary>
        /// The brain main loop.
        /// </summary>
        public override void Think()
        {
            FSM.Think();
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
        /// Called whenever Mistress of Runes's body sends something to its brain.
        /// </summary>
        /// <param name="e">The event that occured.</param>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The event details.</param>
        public override void Notify(DOL.Events.DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);
            if (sender == Body)
            {
                GameEpicMistress mistress = sender as GameEpicMistress;
                if (e == GameObjectEvent.TakeDamage)
                {
                    // Someone hit Mistress of Runes. If the attacker is in melee range, there
                    // is a chance Mistress will cast a AoE Spear and if someone isn't Mistress will cast Nearsight
                    // and AoE Spear


                    GameObject source = (args as TakeDamageEventArgs).DamageSource;
                    if (source != null)
                    {
                        if (mistress.IsStunned)
                        {
                            mistress.StopCurrentSpellcast();
                        }
                        else
                        {
                            if (mistress.IsWithinRadius(source, mistress.AttackRange))
                            {
                                if (Body.AttackState)
                                    Body.StopAttack();
                                mistress.CheckAoESpear(source as GamePlayer);
                            }
                            else
                            {
                                if (Body.AttackState)
                                    Body.StopAttack();
                                mistress.CheckNearsight(source as GamePlayer);
                                mistress.CheckAoESpear(source as GamePlayer);
                            }
                        }
                    }

                    else
                    {
                        log.Error("Mistress of Runes takes damage from null source. args = " + (args == null ? "null" : args.ToString()));
                    }
                }
                else if (e == GameLivingEvent.EnemyHealed)
                {
                    // Someone healed an enemy. If the healer is in melee range, there
                    // is a chance Mistress of Runes will cast a Nearsight specific to ranged
                    // classes on him + AoE Spear, if not, there's still Nearsight...

                    GameObject source = (args as EnemyHealedEventArgs).HealSource;

                    if (source != null)
                    {
                        if (mistress.IsWithinRadius(source, mistress.AttackRange))
                        {
                            if (Body.AttackState)
                                Body.StopAttack();
                            mistress.CheckNearsight(source as GamePlayer);
                            mistress.CheckAoESpear(source as GamePlayer);
                        }
                        else
                        {
                            mistress.CheckNearsight(source as GamePlayer);
                        }

                    }
                    else
                    {
                        log.Error("Mistress of Runes heal source null. args = " + (args == null ? "null" : args.ToString()));
                    }
                }

            }
        }

        #region Tether

        /// <summary>
        /// Check whether Mistress of Runes is out of tether range.
        /// </summary>
        /// <returns>True if Mistress has reached the end of its tether.</returns>
        public bool CheckTether()
        {
            GameEpicMistress mistress = Body as GameEpicMistress;
            if (mistress == null) return false;
            return !mistress.IsWithinRadius(mistress.SpawnPoint, mistress.TetherRange);
        }

        #endregion

        #region Nearsight

        /// <summary>
        /// Try to find a potential target for Nearsight.
        /// </summary>
        /// <returns>Whether or not a target was picked.</returns>
        public bool PickNearsightTarget()
        {
            GameEpicMistress mistress = Body as GameEpicMistress;
            if (mistress == null) return false;

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
                        !mistress.IsWithinRadius(living, mistress.AttackRange))
                    {
                        inRangeLiving.Add(living);
                    }
                }
            }

            if (inRangeLiving.Count > 0)
            {
                return mistress.CheckNearsight((GameLiving)(inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
            }

            return false;
        }

        #endregion

        #region AoE Spear

        /// <summary>
        /// Try to find a potential target for AoE Spear.
        /// </summary>
        /// <returns>Whether or not a target was picked.</returns>
        public bool PickAoETarget()
        {
            GameEpicMistress mistress = Body as GameEpicMistress;
            if (mistress == null) return false;

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
                        !mistress.IsWithinRadius(living, mistress.AttackRange))
                    {
                        inRangeLiving.Add(living);
                    }
                }
            }

            if (inRangeLiving.Count > 0)
            {
                return mistress.CheckAoESpear((GameLiving)(inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
            }

            return false;
        }

        #endregion
    }
}
