using System;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.ServerProperties;
using DOL.GS.SpellEffects;
using DOL.GS.Spells;
using DOL.Language;
using ECS.Debug;

namespace DOL.GS
{
    public static class EffectService
    {
        private const string ServiceName = "EffectService";

        static EffectService()
        {
            //This should technically be the world manager
            EntityManager.AddService(typeof(EffectService));
        }


        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);

            foreach (var e in EntityManager.GetAllEffects())
            {
                if (e.CancelEffect)
                {
                    if (tick > e.ExpireTick)
                        HandleCancelEffect(e);
                }
                else if (e.IsDisabled)
                {
                    HandleCancelEffect(e);
                }
                else
                {
                    HandlePropertyModification(e);
                }

                EntityManager.RemoveEffect(e);
            }

            Diagnostics.StopPerfCounter(ServiceName);
        }

        private static void HandlePropertyModification(ECSGameEffect e)
        {

            if (e.Owner == null)
            {
                //Console.WriteLine($"Invalid target for Effect {e}");
                return;
            }

            EffectListComponent effectList = e.Owner.effectListComponent;
            if (effectList == null)
            {
                //Console.WriteLine($"No effect list found for {e.Owner}");
                return;
            }

            if (e.EffectType == eEffect.OffensiveProc || e.EffectType == eEffect.DefensiveProc)
            {
                if (!e.Owner.effectListComponent.Effects.ContainsKey(e.EffectType))
                    effectList.AddEffect(e);

                return;
            }

            // Early out if we're trying to add an effect that is already present.
            if (!effectList.AddEffect(e))
            {
                SendSpellResistAnimation(e);
                return;
            }

            // Update the Concentration List if Conc Buff/Song/Chant.
            if (e.ShouldBeAddedToConcentrationList())
            {
                if (e.SpellHandler.Caster != null && e.SpellHandler.Caster.ConcentrationEffects != null)
                {
                    e.SpellHandler.Caster.ConcentrationEffects.Add(e);
                }
            }

            if (e.EffectType == eEffect.Pulse)
            {
                if (!e.RenewEffect && e.SpellHandler.Spell.IsInstantCast)
                    ((SpellHandler)e.SpellHandler).SendCastAnimation();
            }
            else 
            { 
                if (!(e is ECSImmunityEffect))
                {
                    if (!e.RenewEffect)
                        SendSpellAnimation(e);

                    if ((e.SpellHandler.Spell.IsConcentration && !e.SpellHandler.Spell.IsPulsing) || (!e.IsBuffActive && !e.IsDisabled))
                    {
                        if (e.EffectType == eEffect.Mez || e.EffectType == eEffect.Stun)
                        {
                            if (e.EffectType == eEffect.Mez)
                                e.Owner.IsMezzed = true;
                            else
                                e.Owner.IsStunned = true;

                            e.Owner.attackComponent.LivingStopAttack();
                            e.Owner.StopCurrentSpellcast();
                            e.Owner.DisableTurning(true);

                            ((SpellHandler)e.SpellHandler).MessageToLiving(e.Owner, e.SpellHandler.Spell.Message1, eChatType.CT_Spell);
                            ((SpellHandler)e.SpellHandler).MessageToCaster(Util.MakeSentence(e.SpellHandler.Spell.Message2, e.Owner.GetName(0, true)), eChatType.CT_Spell);
                            Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message2, e.Owner.GetName(0, true)), eChatType.CT_Spell, e.Owner, e.SpellHandler.Caster);

                            GamePlayer gPlayer = e.Owner as GamePlayer;
                            if (gPlayer != null)
                            {
                                gPlayer.Client.Out.SendUpdateMaxSpeed();
                                if (gPlayer.Group != null)
                                    gPlayer.Group.UpdateMember(gPlayer, false, false);
                            }
                            else
                            {
                                e.Owner.attackComponent.LivingStopAttack();
                            }
                        }
                        else if (e.EffectType == eEffect.HealOverTime)
                        {
                            (e.SpellHandler as HoTSpellHandler).SendEffectAnimation(e.Owner, 0, false, 1);
                            //"{0} seems calm and healthy."
                            Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message2, e.Owner.GetName(0, false)), eChatType.CT_Spell, e.Owner);
                        }
                        else if (e.EffectType == eEffect.Confusion)
                        {
                            if (e.Owner is GamePlayer)
                            {
                                /*
                                 *Q: What does the confusion spell do against players?
                                 *A: According to the magic man, “Confusion against a player interrupts their current action, whether it's a bow shot or spellcast.
                                 */
                                if (e.SpellHandler.Spell.Value < 0 || Util.Chance(Convert.ToInt32(Math.Abs(e.SpellHandler.Spell.Value))))
                                {
                                    //Spell value below 0 means it's 100% chance to confuse.
                                    GamePlayer gPlayer = e.Owner as GamePlayer;

                                    gPlayer.StartInterruptTimer(gPlayer.SpellInterruptDuration, AttackData.eAttackType.Spell, e.SpellHandler.Caster);
                                }
                                EffectService.RequestCancelEffect(e);
                            }
                            else if (e.Owner is GameNPC)
                            {
                                //check if we should do anything at all.

                                bool doConfuse = (e.SpellHandler.Spell.Value < 0 || Util.Chance(Convert.ToInt32(e.SpellHandler.Spell.Value)));

                                if (!doConfuse)
                                    return;

                                bool doAttackFriend = e.SpellHandler.Spell.Value < 0 && Util.Chance(Convert.ToInt32(Math.Abs(e.SpellHandler.Spell.Value)));

                                GameNPC npc = e.Owner as GameNPC;

                                npc.IsConfused = true;

                                //if (log.IsDebugEnabled)
                                //    log.Debug("CONFUSION: " + npc.Name + " was confused(true," + doAttackFriend.ToString() + ")");

                                if (npc is GamePet && npc.Brain != null && (npc.Brain as IControlledBrain) != null)
                                {
                                    //it's a pet.
                                    GamePlayer playerowner = (npc.Brain as IControlledBrain).GetPlayerOwner();
                                    if (playerowner != null && playerowner.CharacterClass.ID == (int)eCharacterClass.Theurgist)
                                    {
                                        //Theurgist pets die.
                                        npc.Die(e.SpellHandler.Caster);
                                        EffectService.RequestCancelEffect(e);
                                        return;
                                    }
                                }

                                (e.SpellHandler as ConfusionSpellHandler).targetList.Clear();
                                foreach (GamePlayer target in npc.GetPlayersInRadius(1000))
                                {
                                    if (doAttackFriend)
                                        (e.SpellHandler as ConfusionSpellHandler).targetList.Add(target);
                                    else
                                    {
                                        //this should prevent mobs from attacking friends.
                                        if (GameServer.ServerRules.IsAllowedToAttack(npc, target, true))
                                            (e.SpellHandler as ConfusionSpellHandler).targetList.Add(target);
                                    }
                                }

                                foreach (GameNPC target in npc.GetNPCsInRadius(1000))
                                {
                                    //don't agro yourself.
                                    if (target == npc)
                                        continue;

                                    if (doAttackFriend)
                                        (e.SpellHandler as ConfusionSpellHandler).targetList.Add(target);
                                    else
                                    {
                                        //this should prevent mobs from attacking friends.
                                        if (GameServer.ServerRules.IsAllowedToAttack(npc, target, true) && !GameServer.ServerRules.IsSameRealm(npc, target, true))
                                            (e.SpellHandler as ConfusionSpellHandler).targetList.Add(target);
                                    }
                                }

                                //targetlist should be full, start effect pulse.
                                if ((e.SpellHandler as ConfusionSpellHandler).targetList.Count > 0)
                                {
                                    npc.StopAttack();
                                    npc.StopCurrentSpellcast();

                                    GameLiving target = (e.SpellHandler as ConfusionSpellHandler).targetList[Util.Random((e.SpellHandler as ConfusionSpellHandler).targetList.Count - 1)] as GameLiving;
                                    npc.StartAttack(target);
                                }
                            }
                        }
                        else if (e.EffectType == eEffect.Charm)
                        {
                            GamePlayer gPlayer = e.SpellHandler.Caster as GamePlayer;
                            GameNPC npc = e.Owner as GameNPC;

                            if (gPlayer != null && npc != null)
                            {
                                ((CharmSpellHandler)e.SpellHandler).SendEffectAnimation(npc, 0, false, 1);
                                if (gPlayer.ControlledBrain != null)
                                    gPlayer.CommandNpcRelease();

                                if ((e.SpellHandler as CharmSpellHandler).m_controlledBrain == null)
                                    (e.SpellHandler as CharmSpellHandler).m_controlledBrain = new ControlledNpcBrain(gPlayer);

                                if (!(e.SpellHandler as CharmSpellHandler).m_isBrainSet && 
                                    !(e.SpellHandler as CharmSpellHandler).m_controlledBrain.IsActive)
                                {

                                    npc.AddBrain((e.SpellHandler as CharmSpellHandler).m_controlledBrain);
                                    (e.SpellHandler as CharmSpellHandler).m_isBrainSet = true;

                                    GameEventMgr.AddHandler(npc, GameLivingEvent.PetReleased, new DOLEventHandler((e.SpellHandler as CharmSpellHandler).ReleaseEventHandler));
                                }

                                if (gPlayer.ControlledBrain != (e.SpellHandler as CharmSpellHandler).m_controlledBrain)
                                {

                                    // sorc: "The slough serpent is enthralled!" ct_spell
                                    Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message1, npc.GetName(0, false)), eChatType.CT_Spell);
                                    (e.SpellHandler as CharmSpellHandler).MessageToCaster(npc.GetName(0, true) + " is now under your control.", eChatType.CT_Spell);

                                    gPlayer.SetControlledBrain((e.SpellHandler as CharmSpellHandler).m_controlledBrain);

                                    foreach (GamePlayer ply in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    {
                                        ply.Out.SendNPCCreate(npc);
                                        if (npc.Inventory != null)
                                            ply.Out.SendLivingEquipmentUpdate(npc);

                                        ply.Out.SendObjectGuildID(npc, gPlayer.Guild);
                                    }
                                }                      
                            }
                            //else
                            //{
                            //    // something went wrong.
                            //    if (log.IsWarnEnabled)
                            //        log.Warn(string.Format("charm effect start: Caster={0} effect.Owner={1}",
                            //                               (Caster == null ? "(null)" : Caster.GetType().ToString()),
                            //                               (effect.Owner == null ? "(null)" : effect.Owner.GetType().ToString())));
                            //}
                        }
                        else if (e.EffectType == eEffect.AblativeArmor)
                        {
                            e.Owner.TempProperties.setProperty(AblativeArmorSpellHandler.ABLATIVE_HP, (int)e.SpellHandler.Spell.Value);
                            //GameEventMgr.AddHandler(e.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));

                            eChatType toLiving = (e.SpellHandler.Spell.Pulse == 0) ? eChatType.CT_Spell : eChatType.CT_SpellPulse;
                            eChatType toOther = (e.SpellHandler.Spell.Pulse == 0) ? eChatType.CT_System : eChatType.CT_SpellPulse;
                            (e.SpellHandler as AblativeArmorSpellHandler).MessageToLiving(e.Owner, e.SpellHandler.Spell.Message1, toLiving);
                            Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message2, e.Owner.GetName(0, false)), toOther, e.Owner);
                        }
                        else if (e.EffectType == eEffect.Bladeturn)
                        {                           
                            eChatType toLiving = (e.SpellHandler.Spell.Pulse == 0) ? eChatType.CT_Spell : eChatType.CT_SpellPulse;
                            eChatType toOther = (e.SpellHandler.Spell.Pulse == 0) ? eChatType.CT_System : eChatType.CT_SpellPulse;
                            (e.SpellHandler as BladeturnSpellHandler).MessageToLiving(e.Owner, e.SpellHandler.Spell.Message1, toLiving);
                            Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message2, e.Owner.GetName(0, false)), toOther, e.Owner);
                        }
                        else if (e.EffectType == eEffect.PiercingMagic)
                        {
                            //Console.WriteLine($"Buffing {e.SpellHandler.Spell.Name}");
                            //Console.WriteLine($"Value before: {e.Owner.Effectiveness}");
                            e.Owner.Effectiveness += (e.SpellHandler.Spell.Value / 100);
                            //Console.WriteLine($"Value after: {e.Owner.Effectiveness}");
                        }
                        else if (e.EffectType == eEffect.SavageBuff)
                        {
                            //Console.WriteLine($"Savage Buffing {(e.SpellHandler as AbstractSavageBuff).Property1.ToString()}");
                            ApplyBonus(e.Owner, (e.SpellHandler as AbstractSavageBuff).BonusCategory1, (e.SpellHandler as AbstractSavageBuff).Property1, (int)e.SpellHandler.Spell.Value, false);
                        }
                        else if (e.EffectType == eEffect.ResurrectionIllness)
                        {
                            GamePlayer gPlayer = e.Owner as GamePlayer;
                            if (gPlayer != null)
                            {
                                gPlayer.Effectiveness -= e.SpellHandler.Spell.Value * 0.01;
                                gPlayer.Out.SendUpdateWeaponAndArmorStats();
                                gPlayer.Out.SendStatusUpdate();
                            }
                        }
                        else if (isDebuff(e.EffectType))
                        {
                            if (e.EffectType == eEffect.StrConDebuff || e.EffectType == eEffect.DexQuiDebuff)
                            {
                                foreach (var prop in getPropertyFromEffect(e.EffectType))
                                {
                                    //Console.WriteLine($"Debuffing {prop.ToString()}");
                                    ApplyBonus(e.Owner, eBuffBonusCategory.Debuff, prop, (int)e.SpellHandler.Spell.Value, true);
                                }
                            }
                            else
                            {
                                if (e.EffectType == eEffect.MovementSpeedDebuff)
                                {
                                    //// Cannot apply if the effect owner has a charging effect
                                    //if (effect.Owner.EffectList.GetOfType<ChargeEffect>() != null || effect.Owner.TempProperties.getProperty("Charging", false))
                                    //{
                                    //    MessageToCaster(effect.Owner.Name + " is moving too fast for this spell to have any effect!", eChatType.CT_SpellResisted);
                                    //    return;
                                    //}


                                    //// Cancels mezz on the effect owner, if applied
                                    //e.Owner.effectListComponent.Effects.TryGetValue(eEffect.Mez, out var mezz);
                                    //if (mezz != null)
                                    //{
                                    //    mezz.CancelEffect = true;
                                    //    mezz.ExpireTick = GameLoop.GameLoopTime - 1;
                                    //    EntityManager.AddEffect(mezz);
                                    //}
                                    //Console.WriteLine("Debuffing Speed for " + e.Owner.Name);
                                    e.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, e.SpellHandler.Spell.ID, 1.0 - e.SpellHandler.Spell.Value * 0.01);
                                    UnbreakableSpeedDecreaseSpellHandler.SendUpdates(e.Owner);

                                    (e.SpellHandler as SpellHandler).MessageToLiving(e.Owner, e.SpellHandler.Spell.Message1, eChatType.CT_Spell);
                                    Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message2, e.Owner.GetName(0, true)), eChatType.CT_Spell, e.Owner);
                                }
                                else if (e.EffectType == eEffect.Disease)
                                {
                                    if (e.Owner.Realm == 0 || e.SpellHandler.Caster.Realm == 0)
                                    {
                                        e.Owner.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                                        e.SpellHandler.Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
                                    }
                                    else
                                    {
                                        e.Owner.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
                                        e.SpellHandler.Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
                                    }

                                    e.Owner.effectListComponent.Effects.TryGetValue(eEffect.Mez, out var mezz);
                                    if (mezz != null)
                                    {
                                        EffectService.RequestCancelEffect(mezz.FirstOrDefault());

                                    }
                                    e.Owner.Disease(true);
                                    e.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, e.SpellHandler, 1.0 - 0.15);
                                    e.Owner.BuffBonusMultCategory1.Set((int)eProperty.Strength, e.SpellHandler, 1.0 - 0.075);

                                    (e.SpellHandler as DiseaseSpellHandler).SendUpdates(e);

                                    (e.SpellHandler as DiseaseSpellHandler).MessageToLiving(e.Owner, e.SpellHandler.Spell.Message1, eChatType.CT_Spell);
                                    Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message2, e.Owner.GetName(0, true)), eChatType.CT_System, e.Owner);

                                    e.Owner.StartInterruptTimer(e.Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, e.SpellHandler.Caster);
                                    if (e.Owner is GameNPC)
                                    {
                                        IOldAggressiveBrain aggroBrain = ((GameNPC)e.Owner).Brain as IOldAggressiveBrain;
                                        if (aggroBrain != null)
                                            aggroBrain.AddToAggroList(e.SpellHandler.Caster, 1);
                                    }
                                }
                                else if (e.EffectType == eEffect.Nearsight)
                                {
                                    e.Owner.effectListComponent.Effects.TryGetValue(eEffect.Mez, out var mezz);
                                    if (mezz != null)
                                    {
                                        EffectService.RequestCancelEffect(mezz.FirstOrDefault());
                                    }

                                    // percent category
                                    e.Owner.DebuffCategory[(int)eProperty.ArcheryRange] += (int)e.SpellHandler.Spell.Value;
                                    e.Owner.DebuffCategory[(int)eProperty.SpellRange] += (int)e.SpellHandler.Spell.Value;
                                    (e.SpellHandler as NearsightSpellHandler).SendEffectAnimation(e.Owner, 0, false, 1);
                                    (e.SpellHandler as NearsightSpellHandler).MessageToLiving(e.Owner, e.SpellHandler.Spell.Message1, eChatType.CT_Spell);
                                    Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message2, e.Owner.GetName(0, false)), eChatType.CT_Spell, e.Owner);
                                }
                                else
                                {                                    
                                    foreach (var prop in getPropertyFromEffect(e.EffectType))
                                    {
                                        //Console.WriteLine($"Debuffing {prop.ToString()}");
                                        if (e.EffectType == eEffect.ArmorFactorDebuff)
                                            ApplyBonus(e.Owner, eBuffBonusCategory.Debuff, prop, (int)e.SpellHandler.Spell.Value, false);
                                        else
                                            ApplyBonus(e.Owner, eBuffBonusCategory.Debuff, prop, (int)e.SpellHandler.Spell.Value, true);
                                    }
                                }
                            }

                        }
                        else
                        {
                            if (e.EffectType == eEffect.StrengthConBuff || e.EffectType == eEffect.DexQuickBuff || e.EffectType == eEffect.SpecAFBuff)
                            {
                                foreach (var prop in getPropertyFromEffect(e.EffectType))
                                {
                                    //Console.WriteLine($"Buffing {prop.ToString()}");
                                    ApplyBonus(e.Owner, eBuffBonusCategory.SpecBuff, prop, (int)e.SpellHandler.Spell.Value, false);
                                }
                            } 
                            else
                            {
                                foreach (var prop in getPropertyFromEffect(e.EffectType))
                                {
                                    //Console.WriteLine($"Buffing {prop.ToString()}");

                                    if (e.EffectType == eEffect.MovementSpeedBuff)
                                    {
                                        if (!e.Owner.InCombat && !e.Owner.IsStealthed)
                                        {
                                            //Console.WriteLine($"Value before: {e.Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                                            //e.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, e.SpellHandler, e.SpellHandler.Spell.Value / 100.0);
                                            e.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, e.EffectType, e.SpellHandler.Spell.Value / 100.0);
                                            //Console.WriteLine($"Value after: {e.Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                                            (e.SpellHandler as SpeedEnhancementSpellHandler).SendUpdates(e.Owner);
                                        }
                                        if (e.Owner.IsStealthed)
                                        {
                                            EffectService.RequestDisableEffect(e, true);
                                        }
                                    }
                                    else if (e.EffectType == eEffect.EnduranceRegenBuff)
                                    {
                                        //Console.WriteLine("Applying EnduranceRegenBuff");
                                        var handler = e.SpellHandler as EnduranceRegenSpellHandler;
                                        ApplyBonus(e.Owner, handler.BonusCategory1, handler.Property1, (int)handler.Spell.Value, false);
                                    }
                                    else
                                        ApplyBonus(e.Owner, eBuffBonusCategory.BaseBuff, prop, (int)e.SpellHandler.Spell.Value, false);
                                }
                            }                          
                        }
                        e.IsBuffActive = true;
                    }
                    else if (e.EffectType == eEffect.SavageBuff)
                    {
                        if (e.SpellHandler.Spell.Power != 0)
                        {
                            int cost = 0;
                            if (e.SpellHandler.Spell.Power < 0)
                                cost = (int)(e.SpellHandler.Caster.MaxHealth * Math.Abs(e.SpellHandler.Spell.Power) * 0.01);
                            else
                                cost = e.SpellHandler.Spell.Power;
                            if (e.Owner.Health > cost)
                                e.Owner.ChangeHealth(e.Owner, eHealthChangeType.Spell, -cost);
                        }
                    }                    
                }
                //else
                //{
                //    if (e.Owner is GamePlayer immunePlayer)
                //    {
                //        immunePlayer.Out.SendUpdateIcons(e.Owner.effectListComponent.Effects.Values.Where(ef => ef.Icon != 0).ToList(), ref e.Owner.effectListComponent._lastUpdateEffectsCount);
                //    }
                //}
                
                if (e.Owner is GamePlayer player)
                {
                    List<ECSGameEffect> ecsList = new List<ECSGameEffect>();

                    if (e.PreviousPosition >= 0)
                    {
                        List<ECSGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                        ecsList.AddRange(playerEffects.Skip(e.PreviousPosition));
                    }
                    else
                        ecsList.Add(e);

                    player.Out.SendUpdateIcons(ecsList, ref e.Owner.effectListComponent._lastUpdateEffectsCount);
                    SendPlayerUpdates(player);                   
                }
                else if (e.Owner is GameNPC)
                {
                    IControlledBrain npc = ((GameNPC)e.Owner).Brain as IControlledBrain;
                    if (npc != null)
                        npc.UpdatePetWindow();
                }
            }
        }

        private static void HandleCancelEffect(ECSGameEffect e)
        {
            //Console.WriteLine($"Handling Cancel Effect {e.SpellHandler.ToString()}");

            if (e.EffectType == eEffect.OffensiveProc || e.EffectType == eEffect.DefensiveProc)
            {
                if (e.Owner.effectListComponent.Effects.ContainsKey(e.EffectType))
                    e.Owner.effectListComponent.RemoveEffect(e);
                
                return;
            }

            if (!e.Owner.effectListComponent.RemoveEffect(e))
            {
                //Console.WriteLine("Unable to remove effect!");
                return;
            }
            if (!e.IsBuffActive)
            {
                //Console.WriteLine("Buff not active! {0} on {1}", e.SpellHandler.Spell.Name, e.Owner.Name);
            }
            else if (e.EffectType != eEffect.Pulse)
            {
                if (!(e is ECSImmunityEffect) )
                {
                    if (e.EffectType == eEffect.Mez || e.EffectType == eEffect.Stun)
                    {
                        if (e.EffectType == eEffect.Mez)
                            e.Owner.IsMezzed = false;
                        else
                            e.Owner.IsStunned = false;

                        // Add Immunity------------Hard coded 60 second Immunity and Icon needs work
                        if (e.EffectType == eEffect.Stun && (e.SpellHandler.Caster as GamePlayer) != null)
                        {
                            var immunityEffect = new ECSImmunityEffect(e.Owner, e.SpellHandler, 60000, (int)e.PulseFreq, e.Effectiveness, e.Icon);
                            EntityManager.AddEffect(immunityEffect);
                        }
                        else if (e.EffectType == eEffect.Mez)
                        {
                            var immunityEffect = new ECSImmunityEffect(e.Owner, e.SpellHandler, 60000, (int)e.PulseFreq, e.Effectiveness, e.Icon);
                            EntityManager.AddEffect(immunityEffect);
                        }

                        e.Owner.DisableTurning(false);

                        GamePlayer gPlayer = e.Owner as GamePlayer;

                        if (gPlayer != null)
                        {
                            gPlayer.Client.Out.SendUpdateMaxSpeed();
                            if (gPlayer.Group != null)
                                gPlayer.Group.UpdateMember(gPlayer, false, false);
                        }
                        else
                        {
                            GameNPC npc = e.Owner as GameNPC;
                            if (npc != null)
                            {
                                IOldAggressiveBrain aggroBrain = npc.Brain as IOldAggressiveBrain;
                                if (aggroBrain != null)
                                    aggroBrain.AddToAggroList(e.SpellHandler.Caster, 1);
                                npc.attackComponent.AttackState = true;
                            }
                        }
                    }
                    else if (e.EffectType == eEffect.HealOverTime)
                    {
                        //"Your meditative state fades."
                        (e.SpellHandler as HoTSpellHandler).MessageToLiving(e.Owner, e.SpellHandler.Spell.Message3, eChatType.CT_SpellExpires);
                        //"{0}'s meditative state fades."
                        Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message4, e.Owner.GetName(0, false)), eChatType.CT_SpellExpires, e.Owner);
                    }
                    else if (e.EffectType == eEffect.Confusion)
                    {
                        if (e != null && e.Owner != null && e.Owner is GameNPC)
                        {
                            GameNPC npc = e.Owner as GameNPC;
                            npc.IsConfused = false;
                        }
                    }
                    else if (e.EffectType == eEffect.Charm)
                    {
                        //Console.WriteLine("Canceling Charm effect on " + e.Owner.Name);
                        GamePlayer gPlayer = e.SpellHandler.Caster as GamePlayer;
                        GameNPC npc = e.Owner as GameNPC;

                        if (gPlayer != null && npc != null)
                        {
                            //if (!noMessages) // no overwrite
                            //{

                                GameEventMgr.RemoveHandler(npc, GameLivingEvent.PetReleased, new DOLEventHandler((e.SpellHandler as CharmSpellHandler).ReleaseEventHandler));
                            ControlledNpcBrain oldBrain = (ControlledNpcBrain)gPlayer.ControlledBrain;
                                gPlayer.SetControlledBrain(null);

                                (e.SpellHandler as CharmSpellHandler).MessageToCaster("You lose control of " + npc.GetName(0, false) + "!", eChatType.CT_SpellExpires);

                                lock (npc.BrainSync)
                                {

                                    npc.StopAttack();
                                    if (npc.RemoveBrain(oldBrain/*(e.SpellHandler as CharmSpellHandler).m_controlledBrain*/))
                                        Console.WriteLine("NPC brain removed!");
                                    (e.SpellHandler as CharmSpellHandler).m_isBrainSet = false;


                                    if (npc.Brain != null && npc.Brain is IOldAggressiveBrain)
                                    {

                                        ((IOldAggressiveBrain)npc.Brain).ClearAggroList();

                                        if (e.SpellHandler.Spell.Pulse != 0 && e.SpellHandler.Caster.ObjectState == GameObject.eObjectState.Active && e.SpellHandler.Caster.IsAlive
                                        && !e.SpellHandler.Caster.IsStealthed)
                                        {
                                            ((IOldAggressiveBrain)npc.Brain).AddToAggroList(e.SpellHandler.Caster, e.SpellHandler.Caster.Level * 10);
                                            npc.StartAttack(e.SpellHandler.Caster);
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

                                (e?.SpellHandler as CharmSpellHandler)?.m_controlledBrain?.ClearAggroList();
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

                        //}
                        //else
                        //{
                        //    if (log.IsWarnEnabled)
                        //        log.Warn(string.Format("charm effect expired: Caster={0} effect.Owner={1}",
                        //                               (Caster == null ? "(null)" : Caster.GetType().ToString()),
                        //                               (effect.Owner == null ? "(null)" : effect.Owner.GetType().ToString())));
                        //}
                    }
                    else if (e.EffectType == eEffect.AblativeArmor)
                    {
                        e.Owner.TempProperties.removeProperty(AblativeArmorSpellHandler.ABLATIVE_HP);
                        //if (!noMessages && Spell.Pulse == 0)
                        //{
                        (e.SpellHandler as AblativeArmorSpellHandler).MessageToLiving(e.Owner, e.SpellHandler.Spell.Message3, eChatType.CT_SpellExpires);
                         Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message4, e.Owner.GetName(0, false)), eChatType.CT_SpellExpires, e.Owner);
                        //}
                    }
                    else if (e.EffectType == eEffect.Bladeturn)
                    {
                        (e.SpellHandler as BladeturnSpellHandler).MessageToLiving(e.Owner, e.SpellHandler.Spell.Message3, eChatType.CT_SpellExpires);
                        Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message4, e.Owner.GetName(0, false)), eChatType.CT_SpellExpires, e.Owner);
                    }
                    else if (e.EffectType == eEffect.PiercingMagic)
                    {
                        //Console.WriteLine($"Canceling {e.SpellHandler.Spell.Name}");
                        //Console.WriteLine($"Value before: {e.Owner.Effectiveness}");
                        e.Owner.Effectiveness -= (e.SpellHandler.Spell.Value / 100);
                        //Console.WriteLine($"Value after: {e.Owner.Effectiveness}");                        
                    }
                    else if (e.EffectType == eEffect.SavageBuff)
                    {
                        //Console.WriteLine($"Savage Canceling {(e.SpellHandler as AbstractSavageBuff).Property1.ToString()}");
                        ApplyBonus(e.Owner, (e.SpellHandler as AbstractSavageBuff).BonusCategory1, (e.SpellHandler as AbstractSavageBuff).Property1, (int)e.SpellHandler.Spell.Value, true);

                        if (e.SpellHandler.Spell.Power != 0)
                        {
                            int cost = 0;
                            if (e.SpellHandler.Spell.Power < 0)
                                cost = (int)(e.SpellHandler.Caster.MaxHealth * Math.Abs(e.SpellHandler.Spell.Power) * 0.01);
                            else
                                cost = e.SpellHandler.Spell.Power;
                            if (e.Owner.Health > cost)
                                e.Owner.ChangeHealth(e.Owner, eHealthChangeType.Spell, -cost);
                        }
                    }
                    else if (e.EffectType == eEffect.Pet)
                    {
                        if (e.SpellHandler.Caster.PetCount > 0)
                            e.SpellHandler.Caster.PetCount--;
                        e.Owner.Health = 0; // to send proper remove packet
                        e.Owner.Delete();
                    }
                    else if (e.EffectType == eEffect.ResurrectionIllness)
                    {
                        GamePlayer gPlayer = e.Owner as GamePlayer;
                        if (gPlayer != null)
                        {
                            gPlayer.Effectiveness += e.SpellHandler.Spell.Value * 0.01;
                            gPlayer.Out.SendUpdateWeaponAndArmorStats();
                            gPlayer.Out.SendStatusUpdate();
                        }
                    }
                    else if (isDebuff(e.EffectType))
                    {
                        if (e.EffectType == eEffect.StrConDebuff || e.EffectType == eEffect.DexQuiDebuff)
                        {
                            foreach (var prop in getPropertyFromEffect(e.EffectType))
                            {
                                //Console.WriteLine($"Canceling {prop.ToString()} on {e.Owner}.");
                                ApplyBonus(e.Owner, eBuffBonusCategory.Debuff, prop, (int)e.SpellHandler.Spell.Value, false);
                            }
                        }
                        else
                        {
                            if (e.EffectType == eEffect.MovementSpeedDebuff)
                            {
                                if (e.SpellHandler.Spell.SpellType == (byte)eSpellType.SpeedDecrease)
                                {
                                    ECSImmunityEffect immunityEffect = new ECSImmunityEffect(e.Owner, e.SpellHandler, 60000, (int)e.PulseFreq, e.Effectiveness, e.Icon);
                                    EntityManager.AddEffect(immunityEffect);
                                }

                                e.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, e.SpellHandler.Spell.ID);
                                UnbreakableSpeedDecreaseSpellHandler.SendUpdates(e.Owner);
                            }
                            else if (e.EffectType == eEffect.Disease)
                            {
                                e.Owner.Disease(false);
                                e.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, e.SpellHandler);
                                e.Owner.BuffBonusMultCategory1.Remove((int)eProperty.Strength, e.SpellHandler);

                                //if (!noMessages)
                                //{
                                    ((SpellHandler)e.SpellHandler).MessageToLiving(e.Owner, e.SpellHandler.Spell.Message3, eChatType.CT_SpellExpires);
                                    Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message4, e.Owner.GetName(0, true)), eChatType.CT_SpellExpires, e.Owner);
                                //}

                                (e.SpellHandler as DiseaseSpellHandler).SendUpdates(e);
                            }
                            else if (e.EffectType == eEffect.Nearsight)
                            {
                                // percent category
                                e.Owner.DebuffCategory[(int)eProperty.ArcheryRange] -= (int)e.SpellHandler.Spell.Value;
                                e.Owner.DebuffCategory[(int)eProperty.SpellRange] -= (int)e.SpellHandler.Spell.Value;

                                (e.SpellHandler as NearsightSpellHandler).MessageToLiving(e.Owner, e.SpellHandler.Spell.Message3, eChatType.CT_SpellExpires);
                                Message.SystemToArea(e.Owner, Util.MakeSentence(e.SpellHandler.Spell.Message4, e.Owner.GetName(0, false)), eChatType.CT_SpellExpires, e.Owner);

                                ECSImmunityEffect immunityEffect = new ECSImmunityEffect(e.Owner, e.SpellHandler, 60000, (int)e.PulseFreq, e.Effectiveness, e.Icon);
                                EntityManager.AddEffect(immunityEffect);
                            }
                            else
                            {
                                foreach (var prop in getPropertyFromEffect(e.EffectType))
                                {
                                    //Console.WriteLine($"Canceling {prop.ToString()} on {e.Owner}.");

                                    if (e.EffectType == eEffect.ArmorFactorDebuff)
                                        ApplyBonus(e.Owner, eBuffBonusCategory.Debuff, prop, (int)e.SpellHandler.Spell.Value, true);
                                    else
                                        ApplyBonus(e.Owner, eBuffBonusCategory.Debuff, prop, (int)e.SpellHandler.Spell.Value, false);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (e.EffectType == eEffect.StrengthConBuff || e.EffectType == eEffect.DexQuickBuff || e.EffectType == eEffect.SpecAFBuff)
                        {
                            foreach (var prop in getPropertyFromEffect(e.EffectType))
                            {
                                //Console.WriteLine($"Canceling {prop.ToString()}");
                                ApplyBonus(e.Owner, eBuffBonusCategory.SpecBuff, prop, (int)e.SpellHandler.Spell.Value, true);
                            }
                        }
                        else
                        {                           
                            foreach (var prop in getPropertyFromEffect(e.EffectType))
                            {
                                //Console.WriteLine($"Canceling {prop.ToString()}");


                                if (e.EffectType == eEffect.MovementSpeedBuff)
                                {
                                    if (e.Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed) == e.SpellHandler.Spell.Value / 100 || e.Owner.InCombat)
                                    {
                                        //Console.WriteLine($"Value before: {e.Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                                        //e.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, e.SpellHandler);
                                        e.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, e.EffectType);
                                        //Console.WriteLine($"Value after: {e.Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                                        (e.SpellHandler as SpeedEnhancementSpellHandler).SendUpdates(e.Owner);
                                    }
                                }
                                else if (e.EffectType == eEffect.EnduranceRegenBuff)
                                {
                                    //Console.WriteLine("Removing EnduranceRegenBuff");
                                    var handler = e.SpellHandler as EnduranceRegenSpellHandler;
                                    ApplyBonus(e.Owner, handler.BonusCategory1, handler.Property1, (int)handler.Spell.Value, true);
                                }
                                else
                                    ApplyBonus(e.Owner, eBuffBonusCategory.BaseBuff, prop, (int)e.SpellHandler.Spell.Value, true);
                              
                            }
                        }
                    }
                }
            }

            e.IsBuffActive = false;
            // Update the Concentration List if Conc Buff/Song/Chant.
            if (e.CancelEffect && e.ShouldBeRemovedFromConcentrationList())
            {
                if (e.SpellHandler.Caster != null && e.SpellHandler.Caster.ConcentrationEffects != null)
                {
                    e.SpellHandler.Caster.ConcentrationEffects.Remove(e);
                }
            }

            if (e.Owner is GamePlayer player)
            {
                SendPlayerUpdates(player);
                //Now update EffectList
                List<ECSGameEffect> ecsList = new List<ECSGameEffect>();
                List<ECSGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                ecsList.AddRange(playerEffects.Skip(playerEffects.IndexOf(e)));

                player.Out.SendUpdateIcons(ecsList, ref e.Owner.effectListComponent._lastUpdateEffectsCount);
            }
            else if (e.Owner is GameNPC)
            {
                IControlledBrain npc = ((GameNPC)e.Owner).Brain as IControlledBrain;
                if (npc != null)
                    npc.UpdatePetWindow();
            }
        }

        /// <summary>
        /// Enqueues an ECSGameEffect to be canceled on the next tick.
        /// </summary>
        public static void RequestCancelEffect(ECSGameEffect effect, bool playerCanceled = false)
        {
            if (effect is null)
                return;

            // Player can't remove negative effect or Effect in Immunity State
            if (playerCanceled && ((effect.SpellHandler != null && !effect.SpellHandler.HasPositiveEffect) || effect is ECSImmunityEffect))
            {
                GamePlayer player = effect.Owner as GamePlayer;
                if (player != null)
                    player.Out.SendMessage(LanguageMgr.GetTranslation((effect.Owner as GamePlayer).Client, "Effects.GameSpellEffect.CantRemoveEffect"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                return;
            }

            // playerCanceled param isn't used but it's there in case we eventually want to...
            effect.CancelEffect = true;
            effect.ExpireTick = GameLoop.GameLoopTime - 1;
            EntityManager.AddEffect(effect);
        }

        /// <summary>
        /// Enqueues an ECSGameEffect (as a IConcentrationEffect) to be canceled on the next tick.
        /// </summary>
        public static void RequestCancelConcEffect(IConcentrationEffect concEffect, bool playerCanceled = false)
        {
            ECSGameEffect effect = concEffect as ECSGameEffect;
            if (effect != null)
            {
                RequestCancelEffect(effect, playerCanceled);

                if (effect.SpellHandler.Spell.IsPulsing)
                    effect.Owner.LastPulseCast = null;
            }
        }
        /// <summary>
        /// Enques an ECSGameEffect to be disabled/enabled on next tick
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="disable"></param>
        public static void RequestDisableEffect(ECSGameEffect effect, bool disable)
        {           
            EntityManager.AddEffect(effect);
            effect.IsDisabled = disable;
            effect.RenewEffect = !disable;
        }

        public static void SendSpellAnimation(ECSGameEffect e)
        {
            GameLiving target = e.SpellHandler.GetTarget() != null ? e.SpellHandler.GetTarget() : e.SpellHandler.Caster;

            //foreach (GamePlayer player in e.SpellHandler.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(e.SpellHandler.Caster, e.Owner, e.SpellHandler.Spell.ClientEffect, 0, false, 1);
            }

            if (e.Owner is GamePlayer player1)
            {
                List<ECSGameEffect> ecsList = new List<ECSGameEffect>();
                ecsList.Add(e);

                player1.Out.SendUpdateIcons(ecsList, ref player1.effectListComponent._lastUpdateEffectsCount);
            }
        }

        public static eEffect GetEffectFromSpell(Spell spell)
        {
            //Console.WriteLine("Spell of type: " + (spell.SpellType).ToString());

            switch (spell.SpellType)
            {
                #region Positive Effects
                //positive effects
                case (byte)eSpellType.Bladeturn:
                    return eEffect.Bladeturn;
                case (byte)eSpellType.DamageAdd:
                    return eEffect.DamageAdd;
                //case (byte)eSpellType.DamageReturn:
                //    return eEffect.DamageReturn;
                case (byte)eSpellType.DamageShield: //FocusShield: Could be the wrong SpellType here
                    return eEffect.FocusShield;
                case (byte)eSpellType.AblativeArmor:
                    return eEffect.AblativeArmor;
                case (byte)eSpellType.MeleeDamageBuff:
                    return eEffect.MeleeDamageBuff;
                case (byte)eSpellType.CombatSpeedBuff:
                    return eEffect.MeleeHasteBuff;
                //case (byte)eSpellType.Celerity:  //Possibly the same as CombatSpeedBuff?
                //    return eEffect.Celerity;
                case (byte)eSpellType.SpeedOfTheRealm:
                case (byte)eSpellType.SpeedEnhancement:
                    return eEffect.MovementSpeedBuff;
                case (byte)eSpellType.HealOverTime:
                    return eEffect.HealOverTime;
                case (byte)eSpellType.CombatHeal:
                    return eEffect.CombatHeal;

                //stats
                case (byte)eSpellType.StrengthBuff:
                    return eEffect.StrengthBuff;
                case (byte)eSpellType.DexterityBuff:
                    return eEffect.DexterityBuff;
                case (byte)eSpellType.ConstitutionBuff:
                    return eEffect.ConstitutionBuff;
                case (byte)eSpellType.StrengthConstitutionBuff:
                    return eEffect.StrengthConBuff;
                case (byte)eSpellType.DexterityQuicknessBuff:
                    return eEffect.DexQuickBuff;
                case (byte)eSpellType.AcuityBuff:
                    return eEffect.AcuityBuff;
                case (byte)eSpellType.ArmorAbsorptionBuff:
                    return eEffect.ArmorAbsorptionBuff;
                case (byte)eSpellType.PaladinArmorFactorBuff:
                    return eEffect.PaladinAf;
                case (byte)eSpellType.ArmorFactorBuff:
                    if (spell.IsSpec)
                        return eEffect.SpecAFBuff; 
                    else
                        return eEffect.BaseAFBuff;

                //resists
                case (byte)eSpellType.BodyResistBuff:
                    return eEffect.BodyResistBuff;
                case (byte)eSpellType.SpiritResistBuff:
                    return eEffect.SpiritResistBuff;
                case (byte)eSpellType.EnergyResistBuff:
                    return eEffect.EnergyResistBuff;
                case (byte)eSpellType.HeatResistBuff:
                    return eEffect.HeatResistBuff;
                case (byte)eSpellType.ColdResistBuff:
                    return eEffect.ColdResistBuff;
                case (byte)eSpellType.MatterResistBuff:
                    return eEffect.MatterResistBuff;
                case (byte)eSpellType.BodySpiritEnergyBuff:
                    return eEffect.BodySpiritEnergyBuff;
                case (byte)eSpellType.HeatColdMatterBuff:
                    return eEffect.HeatColdMatterBuff;


                //regen
                case (byte)eSpellType.HealthRegenBuff:
                    return eEffect.HealthRegenBuff;
                case (byte)eSpellType.EnduranceRegenBuff:
                    return eEffect.EnduranceRegenBuff;
                case (byte)eSpellType.PowerRegenBuff:
                    return eEffect.PowerRegenBuff;

                case (byte)eSpellType.OffensiveProc:
                    return eEffect.OffensiveProc;
                case (byte)eSpellType.DefensiveProc:
                    return eEffect.DefensiveProc;
                case (byte)eSpellType.HereticPiercingMagic:
                    return eEffect.HereticPiercingMagic;
                #endregion

                #region Negative Effects

                //persistent negative effects
                case (byte)eSpellType.StyleBleeding:
                    return eEffect.Bleed;
                case (byte)eSpellType.DamageOverTime:
                    return eEffect.DamageOverTime;
                case (byte)eSpellType.Charm:
                    return eEffect.Charm;
                case (byte)eSpellType.DamageSpeedDecrease:
                case (byte)eSpellType.StyleSpeedDecrease:
                case (byte)eSpellType.SpeedDecrease:
                case (byte)eSpellType.UnbreakableSpeedDecrease:
                    return eEffect.MovementSpeedDebuff;
                case (byte)eSpellType.MeleeDamageDebuff:
                    return eEffect.MeleeDamageDebuff;
                case (byte)eSpellType.StyleCombatSpeedDebuff:
                case (byte)eSpellType.CombatSpeedDebuff:
                    return eEffect.MeleeHasteDebuff;
                case (byte)eSpellType.Disease:
                    return eEffect.Disease;
                case (byte)eSpellType.Confusion:
                    return eEffect.Confusion;

                //Crowd Control Effects
                case (byte)eSpellType.StyleStun:
                case (byte)eSpellType.Stun:
                    return eEffect.Stun;
                //case (byte)eSpellType.StunImmunity: // ImmunityEffect
                //return eEffect.StunImmunity;
                case (byte)eSpellType.Mesmerize:
                    return eEffect.Mez;
                case (byte)eSpellType.MesmerizeDurationBuff:
                    return eEffect.MesmerizeDurationBuff;
                //case (byte)eSpellType.MezImmunity: // ImmunityEffect
                //    return eEffect.MezImmunity;
                //case (byte)eSpellType.StyleSpeedDecrease:
                //    return eEffect.MeleeSnare;
                //case (byte)eSpellType.Snare: // May work off of SpeedDecrease
                //    return eEffect.Snare;
                //case (byte)eSpellType.SnareImmunity: // Not implemented
                //    return eEffect.SnareImmunity;
                case (byte)eSpellType.Nearsight:
                    return eEffect.Nearsight;

                //stat debuffs
                case (byte)eSpellType.StrengthDebuff:
                    return eEffect.StrengthDebuff;
                case (byte)eSpellType.DexterityDebuff:
                    return eEffect.DexterityDebuff;
                case (byte)eSpellType.ConstitutionDebuff:
                    return eEffect.ConstitutionDebuff;
                case (byte)eSpellType.StrengthConstitutionDebuff:
                    return eEffect.StrConDebuff;
                case (byte)eSpellType.DexterityQuicknessDebuff:
                    return eEffect.DexQuiDebuff;
                case (byte)eSpellType.WeaponSkillConstitutionDebuff:
                    return eEffect.WsConDebuff;
                //case (byte)eSpellType.AcuityDebuff: //Not sure what this is yet
                //return eEffect.Acuity;
                case (byte)eSpellType.ArmorAbsorptionDebuff:
                    return eEffect.ArmorAbsorptionDebuff;
                case (byte)eSpellType.ArmorFactorDebuff:
                    return eEffect.ArmorFactorDebuff;

                //resist debuffs
                case (byte)eSpellType.BodyResistDebuff:
                    return eEffect.BodyResistDebuff;
                case (byte)eSpellType.SpiritResistDebuff:
                    return eEffect.SpiritResistDebuff;
                case (byte)eSpellType.EnergyResistDebuff:
                    return eEffect.EnergyResistDebuff;
                case (byte)eSpellType.HeatResistDebuff:
                    return eEffect.HeatResistDebuff;
                case (byte)eSpellType.ColdResistDebuff:
                    return eEffect.ColdResistDebuff;
                case (byte)eSpellType.MatterResistDebuff:
                    return eEffect.MatterResistDebuff;

                //misc 
                case (byte)eSpellType.SavageCombatSpeedBuff:
                case (byte)eSpellType.SavageCrushResistanceBuff:
                case (byte)eSpellType.SavageDPSBuff:
                case (byte)eSpellType.SavageEnduranceHeal:
                case (byte)eSpellType.SavageEvadeBuff:
                case (byte)eSpellType.SavageParryBuff:
                case (byte)eSpellType.SavageSlashResistanceBuff:
                case (byte)eSpellType.SavageThrustResistanceBuff:
                    return eEffect.SavageBuff;
                case (byte)eSpellType.DirectDamage:
                    return eEffect.DirectDamage;
                case (byte)eSpellType.FacilitatePainworking:
                    return eEffect.FacilitatePainworking;
                case (byte)eSpellType.FatigueConsumptionBuff:
                    return eEffect.FatigueConsumptionBuff;
                case (byte)eSpellType.DirectDamageWithDebuff:
                    if (spell.DamageType == eDamageType.Body)
                        return eEffect.BodyResistDebuff;
                    else if (spell.DamageType == eDamageType.Cold)
                        return eEffect.ColdResistDebuff;
                    else
                        return eEffect.Unknown;
                case (byte)eSpellType.PiercingMagic:
                    return eEffect.PiercingMagic;
                case (byte)eSpellType.PveResurrectionIllness:
                    return eEffect.ResurrectionIllness;
                case (byte)eSpellType.RvrResurrectionIllness:
                    return eEffect.RvrResurrectionIllness;
                //pets
                case (byte)eSpellType.SummonTheurgistPet:
                case (byte)eSpellType.SummonNoveltyPet:
                case (byte)eSpellType.SummonAnimistPet:
                case (byte)eSpellType.SummonAnimistFnF:
                case (byte)eSpellType.SummonSpiritFighter:
                case (byte)eSpellType.SummonHunterPet:
                case (byte)eSpellType.SummonUnderhill:
                case (byte)eSpellType.SummonDruidPet:
                case (byte)eSpellType.SummonSimulacrum:
                case (byte)eSpellType.SummonNecroPet:
                case (byte)eSpellType.SummonCommander:
                case (byte)eSpellType.SummonMinion:
                    return eEffect.Pet;

                #endregion

                default:
                    //Console.WriteLine($"Unable to map effect for ECSGameEffect! {spell}");
                    return eEffect.Unknown;
            }
        }

        public static void SendSpellResistAnimation(ECSGameEffect e)
        {
            GameLiving target = e.SpellHandler.GetTarget() != null ? e.SpellHandler.GetTarget() : e.SpellHandler.Caster;
            //foreach (GamePlayer player in e.SpellHandler.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(e.SpellHandler.Caster, e.Owner, e.SpellHandler.Spell.ClientEffect, 0, false, 0);
            }
        }

        private static void SendPlayerUpdates(GamePlayer player)
        {
            if (player == null)
                return;

            player.Out.SendCharStatsUpdate();
            player.Out.SendCharResistsUpdate();
            player.Out.SendUpdateWeaponAndArmorStats();
            player.UpdateEncumberance();
            player.UpdatePlayerStatus();
            player.Out.SendUpdatePlayer();
            
            if (player.Group != null)
            {
                player.Group.UpdateMember(player, true, false);
            }
        }

        private static List<eProperty> getPropertyFromEffect(eEffect e)
        {
            List<eProperty> list = new List<eProperty>();
            switch (e)
            {
                //stats
                case eEffect.StrengthBuff:
                case eEffect.StrengthDebuff:
                    list.Add(eProperty.Strength);
                    return list;
                case eEffect.DexterityBuff:
                case eEffect.DexterityDebuff:
                    list.Add(eProperty.Dexterity);
                    return list;
                case eEffect.ConstitutionBuff:
                case eEffect.ConstitutionDebuff:
                    list.Add(eProperty.Constitution);
                    return list;
                case eEffect.AcuityBuff:
                case eEffect.AcuityDebuff:
                    list.Add(eProperty.Acuity);
                    return list;

                case eEffect.StrengthConBuff:
                case eEffect.StrConDebuff:
                    list.Add(eProperty.Strength);
                    list.Add(eProperty.Constitution);
                    return list;
                case eEffect.WsConDebuff:
                    list.Add(eProperty.WeaponSkill);
                    list.Add(eProperty.Constitution);
                    return list;
                case eEffect.DexQuickBuff:
                case eEffect.DexQuiDebuff:
                    list.Add(eProperty.Dexterity);
                    list.Add(eProperty.Quickness);
                    return list;
                case eEffect.BaseAFBuff:
                case eEffect.SpecAFBuff:
                case eEffect.PaladinAf:
                case eEffect.ArmorFactorDebuff:
                    list.Add(eProperty.ArmorFactor);
                    return list;
                case eEffect.ArmorAbsorptionBuff:
                case eEffect.ArmorAbsorptionDebuff:
                    list.Add(eProperty.ArmorAbsorption);
                    return list;
                case eEffect.MeleeDamageBuff:
                case eEffect.MeleeDamageDebuff:
                    list.Add(eProperty.MeleeDamage);
                    return list;
                //resists
                case eEffect.NaturalResistDebuff:
                    list.Add(eProperty.Resist_Natural);
                    return list;
                case eEffect.BodyResistBuff:
                case eEffect.BodyResistDebuff:
                    list.Add(eProperty.Resist_Body);
                    return list;
                case eEffect.SpiritResistBuff:
                case eEffect.SpiritResistDebuff:
                    list.Add(eProperty.Resist_Spirit);
                    return list;
                case eEffect.EnergyResistBuff:
                case eEffect.EnergyResistDebuff:
                    list.Add(eProperty.Resist_Energy);
                    return list;
                case eEffect.HeatResistBuff:
                case eEffect.HeatResistDebuff:
                    list.Add(eProperty.Resist_Heat);
                    return list;
                case eEffect.ColdResistBuff:
                case eEffect.ColdResistDebuff:
                    list.Add(eProperty.Resist_Cold);
                    return list;
                case eEffect.MatterResistBuff:
                case eEffect.MatterResistDebuff:
                    list.Add(eProperty.Resist_Matter);
                    return list;
                case eEffect.HeatColdMatterBuff:
                    list.Add(eProperty.Resist_Heat);
                    list.Add(eProperty.Resist_Cold);
                    list.Add(eProperty.Resist_Matter);
                    return list;
                case eEffect.BodySpiritEnergyBuff:
                    list.Add(eProperty.Resist_Body);
                    list.Add(eProperty.Resist_Spirit);
                    list.Add(eProperty.Resist_Energy);
                    return list;
                case eEffect.AllMagicResistsBuff:
                    list.Add(eProperty.Resist_Body);
                    list.Add(eProperty.Resist_Spirit);
                    list.Add(eProperty.Resist_Energy);
                    list.Add(eProperty.Resist_Heat);
                    list.Add(eProperty.Resist_Cold);
                    list.Add(eProperty.Resist_Matter);
                    return list;

                case eEffect.SlashResistBuff:
                case eEffect.SlashResistDebuff:
                    list.Add(eProperty.Resist_Slash);
                    return list;
                case eEffect.ThrustResistBuff:
                case eEffect.ThrustResistDebuff:
                    list.Add(eProperty.Resist_Thrust);
                    return list;
                case eEffect.CrushResistBuff:
                case eEffect.CrushResistDebuff:
                    list.Add(eProperty.Resist_Crush);
                    return list;
                case eEffect.AllMeleeResistsBuff:
                case eEffect.AllMeleeResistsDebuff:
                    list.Add(eProperty.Resist_Crush);
                    list.Add(eProperty.Resist_Thrust);
                    list.Add(eProperty.Resist_Slash);
                    return list;

                case eEffect.HealthRegenBuff:
                    list.Add(eProperty.HealthRegenerationRate);
                    return list;
                case eEffect.PowerRegenBuff:
                    list.Add(eProperty.PowerRegenerationRate);
                    return list;
                case eEffect.EnduranceRegenBuff:
                    list.Add(eProperty.EnduranceRegenerationRate);
                    return list;
                case eEffect.MeleeHasteBuff:
                case eEffect.MeleeHasteDebuff:
                    list.Add(eProperty.MeleeSpeed);
                    return list;
                case eEffect.MovementSpeedBuff:
                case eEffect.MovementSpeedDebuff:
                    list.Add(eProperty.MaxSpeed);
                    return list;
                case eEffect.MesmerizeDurationBuff:
                    list.Add(eProperty.MesmerizeDurationReduction);
                    return list;
                case eEffect.FatigueConsumptionBuff:
                    list.Add(eProperty.FatigueConsumption);
                    return list;
                default:
                    //Console.WriteLine($"Unable to find property mapping for: {e}");
                    return list;
            }
        }

        private static Boolean isDebuff(eEffect e)
        {
            switch (e)
            {
                case eEffect.Bladeturn:
                case eEffect.DamageAdd:
                case eEffect.DamageReturn:
                case eEffect.FocusShield:
                case eEffect.AblativeArmor:
                case eEffect.MeleeDamageBuff:
                case eEffect.MeleeHasteBuff:
                case eEffect.Celerity:
                case eEffect.MovementSpeedBuff:
                case eEffect.HealOverTime:
                case eEffect.StrengthBuff:
                case eEffect.DexterityBuff:
                case eEffect.ConstitutionBuff:
                case eEffect.AcuityBuff:
                case eEffect.StrengthConBuff:
                case eEffect.DexQuickBuff:
                case eEffect.BaseAFBuff:
                case eEffect.SpecAFBuff:
                case eEffect.PaladinAf:
                case eEffect.ArmorAbsorptionBuff:
                case eEffect.BodyResistBuff:
                case eEffect.SpiritResistBuff:
                case eEffect.EnergyResistBuff:
                case eEffect.HeatResistBuff:
                case eEffect.ColdResistBuff:
                case eEffect.MatterResistBuff:
                case eEffect.HealthRegenBuff:
                case eEffect.EnduranceRegenBuff:
                case eEffect.PowerRegenBuff:
                    return false;

                case eEffect.StrengthDebuff:
                case eEffect.DexterityDebuff:
                case eEffect.ConstitutionDebuff:
                case eEffect.AcuityDebuff:
                case eEffect.StrConDebuff:
                case eEffect.WsConDebuff:
                case eEffect.DexQuiDebuff:
                case eEffect.ArmorFactorDebuff:
                case eEffect.ArmorAbsorptionDebuff:
                case eEffect.BodyResistDebuff:
                case eEffect.SpiritResistDebuff:
                case eEffect.EnergyResistDebuff:
                case eEffect.HeatResistDebuff:
                case eEffect.ColdResistDebuff:
                case eEffect.MatterResistDebuff:
                case eEffect.MeleeHasteDebuff:
                case eEffect.MovementSpeedDebuff:
                case eEffect.Disease:
                case eEffect.Nearsight:
                case eEffect.MeleeDamageDebuff:
                    return true;
                default:
                    //Console.WriteLine($"Unable to detect debuff status for {e}");
                    return false;
            }


        }

        

        /// <summary>
		/// Method used to apply bonuses
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="BonusCat"></param>
		/// <param name="Property"></param>
		/// <param name="Value"></param>
		/// <param name="IsSubstracted"></param>
		private static void ApplyBonus(GameLiving owner, eBuffBonusCategory BonusCat, eProperty Property, int Value, bool IsSubstracted)
        {
            IPropertyIndexer tblBonusCat;
            if (Property != eProperty.Undefined)
            {
                tblBonusCat = GetBonusCategory(owner, BonusCat);
                //Console.WriteLine($"Value before: {tblBonusCat[(int)Property]}");
                if (IsSubstracted)
                    tblBonusCat[(int)Property] -= Value;
                else
                    tblBonusCat[(int)Property] += Value;
                //Console.WriteLine($"Value after: {tblBonusCat[(int)Property]}");
            }
        } 

        private static IPropertyIndexer GetBonusCategory(GameLiving target, eBuffBonusCategory categoryid)
        {
            IPropertyIndexer bonuscat = null;
            switch (categoryid)
            {
                case eBuffBonusCategory.BaseBuff:
                    bonuscat = target.BaseBuffBonusCategory;
                    break;
                case eBuffBonusCategory.SpecBuff:
                    bonuscat = target.SpecBuffBonusCategory;
                    break;
                case eBuffBonusCategory.Debuff:
                    bonuscat = target.DebuffCategory;
                    break;
                case eBuffBonusCategory.Other:
                    bonuscat = target.BuffBonusCategory4;
                    break;
                case eBuffBonusCategory.SpecDebuff:
                    bonuscat = target.SpecDebuffCategory;
                    break;
                case eBuffBonusCategory.AbilityBuff:
                    bonuscat = target.AbilityBonus;
                    break;
                default:
                    //if (log.IsErrorEnabled)
                    //Console.WriteLine("BonusCategory not found " + categoryid + "!");
                    break;
            }
            return bonuscat;
        }
        #region DoT/Bleed

        // For DoT/Bleed functionality. Can be moved.
        public static void OnEffectPulse(ECSGameEffect effect)
        {

            if (effect.Owner.IsAlive == false)
            {
                EffectService.RequestCancelEffect(effect);
            }

            if (effect.Owner.IsAlive)
            {
                if (effect.SpellHandler is DoTSpellHandler handler)
                {
                    // An acidic cloud surrounds you!
                    handler.MessageToLiving(effect.Owner, effect.SpellHandler.Spell.Message1, eChatType.CT_Spell);
                    // {0} is surrounded by an acidic cloud!
                    Message.SystemToArea(effect.Owner, Util.MakeSentence(effect.SpellHandler.Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_YouHit, effect.Owner);
                    if (effect.LastTick == 0/*StartTick + effect.TickInterval > GameLoop.GameLoopTime*/)
                        handler.OnDirectEffect(effect.Owner, effect.Effectiveness, true);
                    else
                        handler.OnDirectEffect(effect.Owner, effect.Effectiveness, false);
                }
                else if (effect.SpellHandler is StyleBleeding bleedHandler)
                {
                    if (effect.StartTick + effect.PulseFreq > GameLoop.GameLoopTime && effect.Owner.TempProperties.getProperty<int>(StyleBleeding.BLEED_VALUE_PROPERTY) == 0)
                    {
                        effect.Owner.TempProperties.setProperty(StyleBleeding.BLEED_VALUE_PROPERTY, (int)bleedHandler.Spell.Damage + (int)bleedHandler.Spell.Damage * Util.Random(25) / 100);  // + random max 25%

                    }
                    bleedHandler.MessageToLiving(effect.Owner, bleedHandler.Spell.Message1, eChatType.CT_YouWereHit);
                    Message.SystemToArea(effect.Owner, Util.MakeSentence(bleedHandler.Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_YouHit, effect.Owner);

                    int bleedValue = effect.Owner.TempProperties.getProperty<int>(StyleBleeding.BLEED_VALUE_PROPERTY);

                    AttackData ad = bleedHandler.CalculateDamageToTarget(effect.Owner, 1.0);

                    bleedHandler.SendDamageMessages(ad);

                    // attacker must be null, attack result is 0x0A
                    foreach (GamePlayer player in ad.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        player.Out.SendCombatAnimation(null, ad.Target, 0, 0, 0, 0, 0x0A, ad.Target.HealthPercent);
                    }
                    // send animation before dealing damage else dead livings show no animation
                    ad.Target.OnAttackedByEnemy(ad);
                    ad.Attacker.DealDamage(ad);

                    if (--bleedValue <= 0 || !effect.Owner.IsAlive)
                    {
                        effect.ExpireTick = GameLoop.GameLoopTime - 1;
                    }
                    else effect.Owner.TempProperties.setProperty(StyleBleeding.BLEED_VALUE_PROPERTY, bleedValue);
                }
            }
        }

        #endregion
    }
}