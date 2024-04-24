using System.Linq;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.GS
{
    public class CharmECSGameEffect : ECSGameSpellEffect
    {
        public CharmECSGameEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStartEffect()
        {
            if (SpellHandler.Caster is not GamePlayer casterPlayer || Owner is not GameNPC charmMob)
                return;

            CharmSpellHandler charmSpellHandler = SpellHandler as CharmSpellHandler;

            if (charmSpellHandler.m_controlledBrain == null && charmMob.Brain is not ControlledMobBrain)
                charmSpellHandler.m_controlledBrain = new ControlledMobBrain(casterPlayer);
            else
            {
                charmSpellHandler.m_controlledBrain = charmMob.Brain as ControlledMobBrain;
                charmSpellHandler.m_isBrainSet = true;
            }

            if (!charmSpellHandler.m_isBrainSet && !charmSpellHandler.m_controlledBrain.IsActive)
            {
                charmMob.AddBrain(charmSpellHandler.m_controlledBrain);
                charmMob.TargetObject = null;
                charmSpellHandler.m_isBrainSet = true;
                GameEventMgr.AddHandler(charmMob, GameLivingEvent.PetReleased, charmSpellHandler.ReleaseEventHandler);
            }

            if (casterPlayer.ControlledBrain != charmSpellHandler.m_controlledBrain)
            {
                // Message: "{0}The slough serpent} is now enthralled!"
                if (!string.IsNullOrEmpty(SpellHandler.Spell.Message1))
                    Message.SystemToArea(charmMob, Util.MakeSentence(SpellHandler.Spell.Message1, charmMob.GetName(0, true)), eChatType.CT_System, charmMob, casterPlayer);

                // Message: {0} is now under your control.
                if (!string.IsNullOrEmpty(SpellHandler.Spell.Message2))
                    charmSpellHandler.MessageToCaster(Util.MakeSentence(SpellHandler.Spell.Message2, charmMob.GetName(0, true)), eChatType.CT_Spell);
                else
                    charmSpellHandler.MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "GamePlayer.GamePet.StartSpell.UnderControl", charmMob.GetName(0, true)), eChatType.CT_Spell);

                casterPlayer.SetControlledBrain(charmSpellHandler.m_controlledBrain);

                foreach (GamePlayer playerInRadius in charmMob.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    playerInRadius.Out.SendNPCCreate(charmMob);

                    if (charmMob.Inventory != null)
                        playerInRadius.Out.SendLivingEquipmentUpdate(charmMob);
                }
            }

            charmSpellHandler.SendEffectAnimation(charmMob, 0, false, 1);
        }

        public override void OnStopEffect()
        {
            GamePlayer casterPlayer = SpellHandler.Caster as GamePlayer;
            GameNPC charmMob = Owner as GameNPC;
            CharmSpellHandler charmSpellHandler = SpellHandler as CharmSpellHandler;
            bool keepSongAlive = false;

            if (casterPlayer != null && charmMob != null)
            {
                GameEventMgr.RemoveHandler(charmMob, GameLivingEvent.PetReleased, charmSpellHandler.ReleaseEventHandler);
                ControlledMobBrain oldBrain = casterPlayer.ControlledBrain as ControlledMobBrain;
                casterPlayer.SetControlledBrain(null);

                var immunityEffects = charmMob.effectListComponent.GetSpellEffects().Where(e => e.TriggersImmunity).ToArray();

                for (int i = 0; i < immunityEffects.Length; i++)
                    EffectService.RequestImmediateCancelEffect(immunityEffects[i]);

                charmMob.StopAttack();
                charmMob.StopCurrentSpellcast();
                charmMob.RemoveBrain(oldBrain);
                StandardMobBrain newBrain = new();
                charmMob.AddBrain(newBrain);
                charmSpellHandler.m_isBrainSet = false;

                if (newBrain is IOldAggressiveBrain)
                {
                    newBrain.ClearAggroList();

                    if (SpellHandler.Spell.Pulse != 0 &&
                        SpellHandler.Caster.ObjectState == GameObject.eObjectState.Active &&
                        SpellHandler.Caster.IsAlive &&
                        !SpellHandler.Caster.IsStealthed)
                    {
                        newBrain.FSM.SetCurrentState(eFSMStateType.AGGRO);
                        newBrain.AddToAggroList(SpellHandler.Caster, SpellHandler.Caster.Level * 10);
                        charmMob.StartAttack(SpellHandler.Caster);
                        charmMob.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                    }
                }

                // remove NPC with new brain from all attackers aggro list
                foreach (GameLiving attacker in charmMob.attackComponent.Attackers.Keys)
                {
                    if (attacker is GameNPC npcAttacker && npcAttacker.Brain is IOldAggressiveBrain aggressiveBrain)
                    {
                        aggressiveBrain.RemoveFromAggroList(charmMob);
                        aggressiveBrain.AddToAggroList(casterPlayer, casterPlayer.Level * 10);
                        npcAttacker.StartAttack(casterPlayer);
                        npcAttacker.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                    }
                }

                charmSpellHandler.m_controlledBrain?.ClearAggroList();
                charmMob.StopFollowing();
                charmMob.TempProperties.SetProperty(GameNPC.CHARMED_TICK_PROP, charmMob.CurrentRegion.Time);

                foreach (GamePlayer playerInRadius in charmMob.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (charmMob.IsAlive)
                    {
                        playerInRadius.Out.SendNPCCreate(charmMob);

                        if (charmMob.Inventory != null)
                            playerInRadius.Out.SendLivingEquipmentUpdate(charmMob);

                        playerInRadius.Out.SendObjectGuildID(charmMob, null);
                    }
                }

                keepSongAlive = charmMob.IsAlive && charmMob.IsWithinRadius(casterPlayer, ControlledMobBrain.MAX_OWNER_FOLLOW_DIST);
            }

            if (!keepSongAlive)
            {
                ECSPulseEffect song = EffectListService.GetPulseEffectOnTarget(casterPlayer, SpellHandler.Spell);

                if (song != null)
                    EffectService.RequestImmediateCancelConcEffect(song);
            }

            charmSpellHandler.m_controlledBrain = null;
        }
    }
}
