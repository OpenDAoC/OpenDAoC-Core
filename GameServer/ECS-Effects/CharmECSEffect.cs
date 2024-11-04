using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
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
            if (Owner is not GameNPC charmNpc)
                return;

            CharmSpellHandler charmSpellHandler = SpellHandler as CharmSpellHandler;
            ControlledMobBrain newBrain = new(SpellHandler.Caster);
            charmNpc.AddBrain(newBrain);
            charmNpc.TargetObject = null;

            if (SpellHandler.Caster is GamePlayer playerCaster)
            {
                // Message: "{0}The slough serpent} is now enthralled!"
                if (!string.IsNullOrEmpty(SpellHandler.Spell.Message1))
                    Message.SystemToArea(charmNpc, Util.MakeSentence(SpellHandler.Spell.Message1, charmNpc.GetName(0, true)), eChatType.CT_System, charmNpc, playerCaster);

                // Message: {0} is now under your control.
                if (!string.IsNullOrEmpty(SpellHandler.Spell.Message2))
                    charmSpellHandler.MessageToCaster(Util.MakeSentence(SpellHandler.Spell.Message2, charmNpc.GetName(0, true)), eChatType.CT_Spell);
                else
                    charmSpellHandler.MessageToCaster(LanguageMgr.GetTranslation(playerCaster.Client, "GamePlayer.GamePet.StartSpell.UnderControl", charmNpc.GetName(0, true)), eChatType.CT_Spell);

                playerCaster.AddControlledBrain(newBrain);
            }

            ClientService.CreateNpcForPlayers(charmNpc);
            charmSpellHandler.SendEffectAnimation(charmNpc, 0, false, 1);
        }

        public override void OnStopEffect()
        {
            if (Owner is not GameNPC charmNpc)
                return;

            ECSGameSpellEffect[] immunityEffects = charmNpc.effectListComponent.GetSpellEffects().Where(e => e.TriggersImmunity).ToArray();

            for (int i = 0; i < immunityEffects.Length; i++)
                EffectService.RequestImmediateCancelEffect(immunityEffects[i]);

            ControlledMobBrain oldBrain = SpellHandler.Caster.ControlledBrain as ControlledMobBrain;
            SpellHandler.Caster.RemoveControlledBrain(oldBrain);
            bool keepSongAlive = false;

            if (oldBrain != null)
            {
                oldBrain.ClearAggroList();
                charmNpc.StopAttack();
                charmNpc.StopCurrentSpellcast();
                charmNpc.RemoveBrain(oldBrain);

                if (charmNpc.Brain == null)
                    charmNpc.AddBrain(new StandardMobBrain());

                if (charmNpc.Brain is IOldAggressiveBrain aggroBrain)
                {
                    aggroBrain.ClearAggroList();

                    if (SpellHandler.Spell.Pulse != 0 &&
                        SpellHandler.Caster.ObjectState is GameObject.eObjectState.Active &&
                        SpellHandler.Caster.IsAlive &&
                        !SpellHandler.Caster.IsStealthed)
                    {
                        aggroBrain.AddToAggroList(SpellHandler.Caster, SpellHandler.Caster.Level * 10);
                        charmNpc.StartAttack(SpellHandler.Caster);
                        charmNpc.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                    }
                }

                // Remove NPC with new brain from all attackers aggro list.
                foreach (GameLiving attacker in charmNpc.attackComponent.Attackers.Keys)
                {
                    if (attacker is GameNPC npcAttacker && npcAttacker.Brain is IOldAggressiveBrain attackerAggroBrain)
                    {
                        attackerAggroBrain.RemoveFromAggroList(charmNpc);
                        attackerAggroBrain.AddToAggroList(SpellHandler.Caster, SpellHandler.Caster.Level * 10);
                        npcAttacker.StartAttack(SpellHandler.Caster);
                        npcAttacker.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                    }
                }

                charmNpc.TempProperties.SetProperty(GameNPC.CHARMED_TICK_PROP, charmNpc.CurrentRegion.Time);

                foreach (GamePlayer playerInRadius in charmNpc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (charmNpc.IsAlive)
                    {
                        playerInRadius.Out.SendNPCCreate(charmNpc);

                        if (charmNpc.Inventory != null)
                            playerInRadius.Out.SendLivingEquipmentUpdate(charmNpc);

                        playerInRadius.Out.SendObjectGuildID(charmNpc, null);
                    }
                }

                keepSongAlive = charmNpc.IsAlive && charmNpc.IsWithinRadius(SpellHandler.Caster, ControlledMobBrain.MAX_OWNER_FOLLOW_DIST);
            }

            if (!keepSongAlive)
            {
                ECSPulseEffect song = EffectListService.GetPulseEffectOnTarget(SpellHandler.Caster, SpellHandler.Spell);

                if (song != null)
                    EffectService.RequestImmediateCancelConcEffect(song);
            }
        }

        public static void FindAndCancelEffectOnTarget(GameNPC target)
        {
            if (target.Brain is not IControlledBrain)
                return;

            if (target.effectListComponent.Effects.TryGetValue(eEffect.Charm, out List<ECSGameEffect> charms))
                EffectService.RequestImmediateCancelEffect(charms.FirstOrDefault());
        }
    }
}
