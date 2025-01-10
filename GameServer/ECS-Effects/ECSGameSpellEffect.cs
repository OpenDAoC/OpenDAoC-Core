using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.Spells;

namespace DOL.GS
{
    /// <summary>
    /// Spell-Based Effect
    /// </summary>
    public class ECSGameSpellEffect : ECSGameEffect, IConcentrationEffect
    {
        public new ISpellHandler SpellHandler;
        string IConcentrationEffect.Name => Name;
        ushort IConcentrationEffect.Icon => Icon;
        byte IConcentrationEffect.Concentration => SpellHandler.Spell.Concentration;
        public override ushort Icon => SpellHandler.Spell.Icon;
        public override string Name => SpellHandler.Spell.Name;
        public override bool HasPositiveEffect => SpellHandler != null && SpellHandler.HasPositiveEffect;
        public bool IsAllowedToPulse => NextTick > 0 && PulseFreq > 0;

        public ECSGameSpellEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            SpellHandler = initParams.SpellHandler;
            Spell spell = SpellHandler.Spell;
            EffectType = EffectService.GetEffectFromSpell(SpellHandler.Spell);
            PulseFreq = spell.Frequency;

            if (spell.SpellType is eSpellType.SpeedDecrease or eSpellType.UnbreakableSpeedDecrease)
            {
                PulseFreq = 250;
                NextTick = 1 + Duration / 2 + StartTick + PulseFreq;

                if (!spell.Name.Equals("Prevent Flight", StringComparison.OrdinalIgnoreCase) && !spell.IsFocus)
                    TriggersImmunity = true;
            }
            else if (spell.IsConcentration)
            {
                PulseFreq = 2500;
                NextTick = StartTick + PulseFreq;
            }

            // These classes start their effects themselves.
            if (this is not ECSImmunityEffect and not ECSPulseEffect and not BleedECSEffect)
                EffectService.RequestStartEffect(this);
        }

        public override bool IsConcentrationEffect()
        {
            return SpellHandler.Spell.IsConcentration;
        }

        public override bool ShouldBeAddedToConcentrationList()
        {
            return SpellHandler.Spell.IsConcentration || EffectType == eEffect.Pulse;
        }

        public override bool ShouldBeRemovedFromConcentrationList()
        {
            return SpellHandler.Spell.IsConcentration || EffectType == eEffect.Pulse;
        }

        public override void TryApplyImmunity()
        {
            if (TriggersImmunity)
            {
                if (OwnerPlayer != null)
                {
                    if ((EffectType == eEffect.Stun && SpellHandler.Caster is GameSummonedPet) || SpellHandler is UnresistableStunSpellHandler)
                        return;

                    new ECSImmunityEffect(Owner, SpellHandler, ImmunityDuration, (int)PulseFreq, Effectiveness, Icon);
                }
                else if (Owner is GameNPC)
                {
                    if (EffectType == eEffect.Stun)
                    {
                        if (EffectListService.GetEffectOnTarget(Owner, eEffect.NPCStunImmunity) is not NPCECSStunImmunityEffect)
                            new NPCECSStunImmunityEffect(new ECSGameEffectInitParams(Owner, ImmunityDuration, Effectiveness, SpellHandler));
                    }
                    else if (EffectType == eEffect.Mez)
                    {
                        if (EffectListService.GetEffectOnTarget(Owner, eEffect.NPCMezImmunity) is not NPCECSMezImmunityEffect)
                            new NPCECSMezImmunityEffect(new ECSGameEffectInitParams(Owner, ImmunityDuration, Effectiveness, SpellHandler));
                    }
                }
            }
        }

        public virtual bool IsBetterThan(ECSGameSpellEffect effect)
        {
            return SpellHandler.Spell.Value > effect.SpellHandler.Spell.Value || SpellHandler.Spell.Damage > effect.SpellHandler.Spell.Damage;
        }

        /// <summary>
        /// Sends Spell messages to all nearby/associated players when an ability/spell/style effect becomes active on a target.
        /// </summary>
        /// <param name="target">The owner of the effect.</param>
        /// <param name="msgTarget">If 'true', the system sends a first-person spell message to the target/owner of the effect.</param>
        /// <param name="msgSelf">If 'true', the system sends a third-person spell message to the caster triggering the effect, regardless of their proximity to the target.</param>
        /// <param name="msgArea">If 'true', the system sends a third-person message to all players within range of the target.</param>
        /// <returns>'Message1' and 'Message2' values from the 'spell' table.</returns>
        public void OnEffectStartsMsg(GameLiving target, bool msgTarget, bool msgSelf, bool msgArea)
        {
            SendMessages(target, msgTarget, msgSelf, msgArea, SpellHandler.Spell.Message1, SpellHandler.Spell.Message2);
        }

        /// <summary>
        /// Sends Spell messages to all nearby/associated players when an ability/spell/style effect ends on a target.
        /// </summary>
        /// <param name="target">The owner of the effect.</param>
        /// <param name="msgTarget">If 'true', the system sends a first-person spell message to the target/owner of the effect.</param>
        /// <param name="msgSelf">If 'true', the system sends a third-person spell message to the caster triggering the effect, regardless of their proximity to the target.</param>
        /// <param name="msgArea">If 'true', the system sends a third-person message to all players within range of the target.</param>
        /// <returns>'Message3' and 'Message4' values from the 'spell' table.</returns>
        public void OnEffectExpiresMsg(GameLiving target, bool msgTarget, bool msgSelf, bool msgArea)
        {
            SendMessages(target, msgTarget, msgSelf, msgArea, SpellHandler.Spell.Message3, SpellHandler.Spell.Message4);
        }

        private void SendMessages(GameLiving target, bool msgTarget, bool msgSelf, bool msgArea, string firstPersonMessage, string thirdPersonMessage)
        {
            // Sends a first-person message directly to the caster's target, if they are a player.
            if (msgTarget && target is GamePlayer playerTarget)
                // "You feel more dexterous!"
                ((SpellHandler) SpellHandler).MessageToLiving(playerTarget, firstPersonMessage, eChatType.CT_Spell);

            GameLiving toExclude = null; // Either the caster or the owner if it's a pet.

            // Sends a third-person message directly to the caster to indicate the spell had landed, regardless of range.
            if (msgSelf && SpellHandler.Caster != target)
            {
                ((SpellHandler) SpellHandler).MessageToCaster(Util.MakeSentence(thirdPersonMessage, target.GetName(0, true)), eChatType.CT_Spell);

                if (SpellHandler.Caster is GamePlayer)
                    toExclude = SpellHandler.Caster;
                else if (SpellHandler.Caster is GameNPC pet && pet.Brain is ControlledMobBrain petBrain)
                {
                    GamePlayer playerOwner = petBrain.GetPlayerOwner();

                    if (playerOwner != null)
                        toExclude = playerOwner;
                }
            }

            // Sends a third-person message to all players surrounding the target.
            if (msgArea)
            {
                if (SpellHandler.Caster == target && SpellHandler.Caster is GamePlayer)
                    toExclude = SpellHandler.Caster;

                // "{0} looks more agile!"
                Message.SystemToArea(target, Util.MakeSentence(thirdPersonMessage, target.GetName(0, thirdPersonMessage.StartsWith("{0}"))), eChatType.CT_Spell, target, toExclude);
            }
        }

        public override DbPlayerXEffect getSavedEffect()
        {
            if (SpellHandler?.Spell == null)
                return null;

            DbPlayerXEffect eff = new()
            {
                Var1 = SpellHandler.Spell.ID,
                Var2 = Effectiveness,
                Var3 = (int) SpellHandler.Spell.Value,
                IsHandler = true,
                SpellLine = SpellHandler.SpellLine.KeyName
            };

            if (Duration > 0)
                eff.Duration = (int) (ExpireTick - GameLoop.GameLoopTime);

            return eff;
        }

        protected static IPropertyIndexer GetPropertyIndexer(GameLiving target, eBuffBonusCategory buffBonusCategory)
        {
            return buffBonusCategory switch
            {
                eBuffBonusCategory.BaseBuff => target.BaseBuffBonusCategory,
                eBuffBonusCategory.SpecBuff => target.SpecBuffBonusCategory,
                eBuffBonusCategory.AbilityBuff => target.AbilityBonus,
                eBuffBonusCategory.OtherBuff => target.OtherBonus,
                eBuffBonusCategory.Debuff => target.DebuffCategory,
                eBuffBonusCategory.SpecDebuff => target.SpecDebuffCategory,
                _ => null,
            };
        }

        protected static void ApplyBonus(GameLiving owner, eBuffBonusCategory bonusCategory, eProperty property, double value, double effectiveness, bool isSubtracted)
        {
            if (property is eProperty.Undefined)
                return;

            if (isSubtracted)
                value = -value;

            int effectiveValue = (int) (value * effectiveness);

            if (owner is GamePlayer player && player.UseDetailedCombatLog)
                player.Out.SendMessage($"BonusCategory: {bonusCategory} | Property: {property}\nValue: {value} | Effectiveness: {effectiveness} | EffectiveValue: {effectiveValue}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

            GetPropertyIndexer(owner, bonusCategory)[(int) property] += effectiveValue;
        }
    }
}
