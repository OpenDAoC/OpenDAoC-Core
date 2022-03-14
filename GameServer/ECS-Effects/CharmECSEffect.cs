using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class CharmECSGameEffect : ECSGameSpellEffect
    {
        public CharmECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            GamePlayer gPlayer = SpellHandler.Caster as GamePlayer;
            GameNPC npc = Owner as GameNPC;

            if (gPlayer != null && npc != null)
            {
                if ((SpellHandler as CharmSpellHandler).m_controlledBrain == null && !(npc.Brain is ControlledNpcBrain))
                {
                    (SpellHandler as CharmSpellHandler).m_controlledBrain = new ControlledNpcBrain(gPlayer);
                }
                else
                {
                    (SpellHandler as CharmSpellHandler).m_controlledBrain = npc.Brain as ControlledNpcBrain;
                    (SpellHandler as CharmSpellHandler).m_isBrainSet = true;
                }

                if (!(SpellHandler as CharmSpellHandler).m_isBrainSet &&
                    !(SpellHandler as CharmSpellHandler).m_controlledBrain.IsActive)
                {

                    npc.AddBrain((SpellHandler as CharmSpellHandler).m_controlledBrain);
                    (SpellHandler as CharmSpellHandler).m_isBrainSet = true;

                    GameEventMgr.AddHandler(npc, GameLivingEvent.PetReleased, new DOLEventHandler((SpellHandler as CharmSpellHandler).ReleaseEventHandler));
                }

                if (gPlayer.ControlledBrain != (SpellHandler as CharmSpellHandler).m_controlledBrain)
                {

                    // sorc: "The slough serpent is enthralled!" ct_spell
                    Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message1, npc.GetName(0, false)), eChatType.CT_Spell);
                    (SpellHandler as CharmSpellHandler).MessageToCaster(npc.GetName(0, true) + " is now under your control.", eChatType.CT_Spell);

                    gPlayer.SetControlledBrain((SpellHandler as CharmSpellHandler).m_controlledBrain);

                    foreach (GamePlayer ply in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        ply.Out.SendNPCCreate(npc);
                        if (npc.Inventory != null)
                            ply.Out.SendLivingEquipmentUpdate(npc);

                        ply.Out.SendObjectGuildID(npc, gPlayer.Guild);
                    }
                }
                ((CharmSpellHandler)SpellHandler).SendEffectAnimation(npc, 0, false, 1);
            }
        }

        public override void OnStopEffect()
        {
            GamePlayer gPlayer = SpellHandler.Caster as GamePlayer;
            GameNPC npc = Owner as GameNPC;

            if (gPlayer != null && npc != null)
            {
                GameEventMgr.RemoveHandler(npc, GameLivingEvent.PetReleased, new DOLEventHandler((SpellHandler as CharmSpellHandler).ReleaseEventHandler));
                ControlledNpcBrain oldBrain = (ControlledNpcBrain)gPlayer.ControlledBrain;
                gPlayer.SetControlledBrain(null);

                (SpellHandler as CharmSpellHandler).MessageToCaster("You lose control of " + npc.GetName(0, false) + "!", eChatType.CT_SpellExpires);

                lock (npc.BrainSync)
                {
                    var immunityEffects = npc.effectListComponent.GetSpellEffects().Where(e => e.TriggersImmunity).ToArray();
                    for (int i = 0; i < immunityEffects.Length; i++)
                    {
                        EffectService.RequestImmediateCancelEffect(immunityEffects[i]);
                    }

                    npc.StopAttack();
                    npc.RemoveBrain(oldBrain);

                    npc.AddBrain(new StandardMobBrain());
                    (SpellHandler as CharmSpellHandler).m_isBrainSet = false;

                    if (npc.Brain != null && npc.Brain is IOldAggressiveBrain)
                    {

                        ((IOldAggressiveBrain)npc.Brain).ClearAggroList();

                        if (SpellHandler.Spell.Pulse != 0 && SpellHandler.Caster.ObjectState == GameObject.eObjectState.Active && SpellHandler.Caster.IsAlive
                        && !SpellHandler.Caster.IsStealthed)
                        {
                            ((IOldAggressiveBrain)npc.Brain).AddToAggroList(SpellHandler.Caster, SpellHandler.Caster.Level * 10);
                            npc.StartAttack(SpellHandler.Caster);
                            npc.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                        }
                        else
                        {
                            npc.WalkToSpawn();
                        }
                    }
                }

                // remove NPC with new brain from all attackers aggro list
                lock (npc.attackComponent.Attackers)
                    foreach (GameObject obj in npc.attackComponent.Attackers)
                    {

                        if (obj == null || !(obj is GameNPC))
                            continue;

                        if (((GameNPC)obj).Brain != null && ((GameNPC)obj).Brain is IOldAggressiveBrain)
                            ((IOldAggressiveBrain)((GameNPC)obj).Brain).RemoveFromAggroList(npc);
                    }

                    (SpellHandler as CharmSpellHandler)?.m_controlledBrain?.ClearAggroList();
                npc.StopFollowing();

                npc.TempProperties.setProperty(GameNPC.CHARMED_TICK_PROP, npc.CurrentRegion.Time);


                foreach (GamePlayer ply in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (npc.IsAlive)
                    {

                        ply.Out.SendNPCCreate(npc);

                        if (npc.Inventory != null)
                            ply.Out.SendLivingEquipmentUpdate(npc);

                        ply.Out.SendObjectGuildID(npc, null);

                    }
                }
            }
            (SpellHandler as CharmSpellHandler).m_controlledBrain = null;
        }
    }
}
