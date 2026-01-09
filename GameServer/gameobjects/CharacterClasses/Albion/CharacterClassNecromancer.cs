using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerClass;

namespace DOL.GS
{
    public class CharacterClassNecromancer : ClassDisciple
    {
        private int _petHealthPercentAfterBrainSet;

        public override void Init(GamePlayer player)
        {
            base.Init(player);

            if (Player.HasShadeModel)
                player.Shade(false);
        }

        public override void SetControlledBrain(IControlledBrain controlledNpcBrain)
        {
            if (controlledNpcBrain == null)
                OnPetReleased();

            base.SetControlledBrain(controlledNpcBrain);
        }

        public override void CommandNpcRelease()
        {
            base.CommandNpcRelease();
            OnPetReleased();
        }

        public override void OnPetReleased()
        {
            IControlledBrain controlledBrain = Player.ControlledBrain;

            if (controlledBrain == null)
                return;

            _petHealthPercentAfterBrainSet = controlledBrain.Body.HealthPercent;

            if (Player.HasShadeModel)
                Player.Shade(false);
        }

        public override bool StartAttack(GameObject attackTarget)
        {
            if (!Player.HasShadeModel)
                return true;
            else
            {
                Player.Out.SendMessage("You cannot enter combat while in shade form!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }
        }

        public override byte HealthPercentGroupWindow
        {
            get
            {
                // Snapshot to prevent null reference if the pet is released mid-call.
                // Seems to happen during EffectListService tick, via EffectListComponent.SendPlayerUpdates > PacketHandler.PacketLib1125.WriteGroupMemberUpdate.
                GameNPC pet = Player.ControlledBrain?.Body;
                return pet == null ? Player.HealthPercent : pet.HealthPercent;
            }
        }

        public override bool CreateShadeEffect(out ECSGameAbilityEffect effect)
        {
            effect = EffectListService.GetAbilityEffectOnTarget(Player, eEffect.Shade);

            if (effect != null)
                return false;

            effect = ECSGameEffectFactory.Create(new(Player, 0, 1), static (in i) => new NecromancerShadeECSGameEffect(i));
            return effect.IsActive;
        }

        public override bool Shade(bool makeShade, out ECSGameAbilityEffect effect)
        {
            if (!base.Shade(makeShade, out effect))
                return false;

            if (effect is not NecromancerShadeECSGameEffect)
                return true;

            if (makeShade)
            {
                GameNPC pet = Player.ControlledBrain.Body;

                if (pet == null)
                    return true;

                // Necromancer has become a shade. Have any previous NPC attacker aggro the pet now, as they can't attack the necromancer any longer.
                foreach (GameObject attacker in Player.attackComponent.AttackerTracker.Attackers)
                {
                    if (attacker is not GameNPC npcAttacker || !npcAttacker.attackComponent.AttackState || npcAttacker.Brain is not IOldAggressiveBrain npcAttackerBrain)
                        continue;

                    npcAttacker.StopAttack();
                    npcAttackerBrain.AddToAggroList(pet, npcAttackerBrain.GetBaseAggroAmount(Player));
                }
            }
            else
            {
                // The necromancer has lost his shade form. Release the pet if it isn't dead already and update the necromancer's current health.
                if (Player.ControlledBrain is ControlledMobBrain controlledMobBrain)
                    controlledMobBrain.Stop();

                Player.Health = (int) Math.Ceiling(Math.Min(Player.Health, Player.MaxHealth * Math.Max(10, _petHealthPercentAfterBrainSet) * 0.01));
            }

            return true;
        }

        public override bool RemoveFromWorld()
        {
            if (Player.HasShadeModel)
                Player.Shade(false);

            return base.RemoveFromWorld();
        }

        public override void Die(GameObject killer)
        {
            if (Player.HasShadeModel)
                Player.Shade(false);

            base.Die(killer);
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            if (Player.ControlledBrain != null)
            {
                GameNPC pet = Player.ControlledBrain.Body;

                if (pet != null && sender == pet && e == GameLivingEvent.CastStarting && args is CastingEventArgs)
                    return;
            }

            base.Notify(e, sender, args);
        }
    }
}
