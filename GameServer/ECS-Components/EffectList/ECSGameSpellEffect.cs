using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
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

        public ECSGameSpellEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            SpellHandler = initParams.SpellHandler;
            Spell spell = SpellHandler.Spell;
            EffectType = EffectService.GetEffectFromSpell(SpellHandler.Spell, SpellHandler.SpellLine.IsBaseLine);
            PulseFreq = spell.Frequency;
            Caster = SpellHandler.Caster;

            if (spell.SpellType is eSpellType.SpeedDecrease or eSpellType.UnbreakableSpeedDecrease)
            {
                TickInterval = 650;
                NextTick = 1 + (Duration >> 1) + (int)StartTick;
                if (!spell.Name.Equals("Prevent Flight") && !spell.IsFocus)
                    TriggersImmunity = true;
            }
            else if (spell.IsConcentration)
            {
                NextTick = StartTick;
                // 60 seconds taken from PropertyChangingSpell
                // Not sure if this is correct
                PulseFreq = 650;
            }

            // These classes start their effects themselves.
            if (this is not ECSImmunityEffect and not ECSPulseEffect)
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

        /// <summary>
        /// Used for 'OnEffectStartMsg' and 'OnEffectExpiresMsg'. Identifies the entity triggering the effect (sometimes the caster and effect owner are the same entity).
        /// </summary>
        public GameLiving Caster { get; }

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
            if (msgSelf && Caster != target)
            {
                ((SpellHandler) SpellHandler).MessageToCaster(Util.MakeSentence(thirdPersonMessage, target.GetName(0, true)), eChatType.CT_Spell);

                if (Caster is GamePlayer)
                    toExclude = Caster;
                else if (Caster is GameNPC pet && pet.Brain is ControlledNpcBrain petBrain)
                    toExclude = petBrain.GetPlayerOwner();
            }

            // Sends a third-person message to all players surrounding the target.
            if (msgArea)
            {
                if (Caster == target && Caster is GamePlayer)
                    toExclude = Caster;

                // "{0} looks more agile!"
                Message.SystemToArea(target, Util.MakeSentence(thirdPersonMessage, target.GetName(0, thirdPersonMessage.StartsWith("{0}"))), eChatType.CT_Spell, target, toExclude);
            }
        }

        public override DbPlayerXEffect getSavedEffect()
        {
            if (SpellHandler == null || SpellHandler.Spell == null) return null;

            DbPlayerXEffect eff = new DbPlayerXEffect();
            eff.Var1 = SpellHandler.Spell.ID;
            eff.Var2 = Effectiveness;
            eff.Var3 = (int)SpellHandler.Spell.Value;

            if (Duration > 0)
                eff.Duration = (int)(ExpireTick - GameLoop.GameLoopTime);
            else
                eff.Duration = 30 * 60 * 1000;

            eff.IsHandler = true;
            eff.SpellLine = SpellHandler.SpellLine.KeyName;
            return eff;
        }
    }
}
