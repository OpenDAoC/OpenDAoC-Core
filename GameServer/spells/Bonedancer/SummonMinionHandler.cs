using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Pet summon spell handler
    ///
    /// Spell.LifeDrainReturn is used for pet ID.
    ///
    /// Spell.Value is used for hard pet level cap
    /// Spell.Damage is used to set pet level:
    /// less than zero is considered as a percent (0 .. 100+) of target level;
    /// higher than zero is considered as level value.
    /// Resulting value is limited by the Byte field type.
    /// Spell.DamageType is used to determine which type of pet is being cast:
    /// 0 = melee
    /// 1 = healer
    /// 2 = mage
    /// 3 = debuffer
    /// 4 = Buffer
    /// 5 = Range
    /// </summary>
    [SpellHandler(eSpellType.SummonMinion)]
    public class SummonMinionHandler : SummonSpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SummonMinionHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Caster is GamePlayer playerCaster)
            {
                IControlledBrain controlledBrain = playerCaster.ControlledBrain;

                if (controlledBrain == null)
                {
                    MessageToCaster(LanguageMgr.GetTranslation(playerCaster.Client, "SummonMinionHandler.CheckBeginCast.Text1"), eChatType.CT_SpellResisted);
                    return false;
                }

                GameNPC controlledBody = controlledBrain.Body;
                IControlledBrain[] controlledBodyControlledBrainList = controlledBody.ControlledNpcList;

                if (controlledBodyControlledBrainList == null || controlledBody.PetCount >= controlledBodyControlledBrainList.Length)
                {
                    MessageToCaster(LanguageMgr.GetTranslation(playerCaster.Client, "SummonMinionHandler.CheckBeginCast.Text2"), eChatType.CT_SpellResisted);
                    return false;
                }

                int cumulativeLevel = 0;

                foreach (IControlledBrain controlledBodyControlledBrain in controlledBodyControlledBrainList)
                    cumulativeLevel += controlledBodyControlledBrain?.Body != null ? controlledBodyControlledBrain.Body.Level : 0;

                byte newPetLevel = (byte) (Caster.Level * m_spell.Damage * -0.01);

                if (newPetLevel > m_spell.Value)
                    newPetLevel = (byte) m_spell.Value;

                if (cumulativeLevel + newPetLevel > 75)
                {
                    MessageToCaster("Your commander is not powerful enough to control a minion of this level.", eChatType.CT_SpellResisted);
                    return false;
                }
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (Caster?.ControlledBrain == null)
                return;

            GameNPC pet = Caster.ControlledBrain.Body;

            // Lets let NPC's able to cast minions. Here we make sure that the Caster is a GameNPC and that m_controlledNpc is initialized (since we aren't thread safe).
            if (pet == null)
            {
                if (Caster is not GameNPC caterNpc)
                    return;

                pet = caterNpc;

                // We'll give default NPCs 2 minions!
                if (pet.ControlledNpcList == null)
                    pet.InitControlledBrainArray(2);
            }

            base.ApplyEffectOnTarget(target);

            if (m_pet.Brain is BdPetBrain brain && !brain.MinionsAssisting)
                brain.SetAggressionState(eAggressionState.Passive);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect.Owner is BdPet bdPetOwner && bdPetOwner.Brain is IControlledBrain brain && brain.Owner is CommanderPet commander)
                commander.RemoveControlledBrain(brain);

            return base.OnEffectExpires(effect, noMessages);
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            BdSubPet.SubPetType type = (BdSubPet.SubPetType) Spell.DamageType;
            owner = owner.ControlledBrain.Body;

            IControlledBrain controlledBrain = type switch
            {
                BdSubPet.SubPetType.Melee => new BdMeleeBrain(owner),
                BdSubPet.SubPetType.Healer => new BdHealerBrain(owner),
                BdSubPet.SubPetType.Caster => new BdCasterBrain(owner),
                BdSubPet.SubPetType.Debuffer => new BdDebufferBrain(owner),
                BdSubPet.SubPetType.Buffer => new BdBufferBrain(owner),
                BdSubPet.SubPetType.Archer => new BdArcherBrain(owner),
                _ => new ControlledMobBrain(owner)
            };

            return controlledBrain;
        }

        protected override GameSummonedPet GetGamePet(INpcTemplate template)
        {
            BdSubPet.SubPetType type = (BdSubPet.SubPetType) Spell.DamageType;

            GameSummonedPet pet = type switch
            {
                BdSubPet.SubPetType.Melee => new BdMeleeSubPet(template),
                BdSubPet.SubPetType.Healer => new BdHealerSubPet(template),
                BdSubPet.SubPetType.Caster => new BdCasterSubPet(template),
                BdSubPet.SubPetType.Debuffer => new BdDebufferSubPet(template),
                BdSubPet.SubPetType.Buffer => new BdBufferSubPet(template),
                BdSubPet.SubPetType.Archer => new BdArcherSubPet(template),
                _ => new GameSummonedPet(template)
            };

            return pet;
        }

        protected override void SetBrainToOwner(IControlledBrain brain)
        {
            Caster.ControlledBrain.Body.AddControlledBrain(brain);
        }

        public override IList<string> DelveInfo
        {
            get
            {
                GameClient client = (Caster as GamePlayer).Client;

                return
                [
                    string.Format(LanguageMgr.GetTranslation(client, "SummonMinionHandler.DelveInfo.Text1", Spell.Target)),
                    string.Format(LanguageMgr.GetTranslation(client, "SummonMinionHandler.DelveInfo.Text2", Math.Abs(Spell.Power))),
                    LanguageMgr.GetTranslation(client, "SummonMinionHandler.DelveInfo.Text3", (Spell.CastTime / 1000).ToString("0.0## " + LanguageMgr.GetTranslation(client, "Effects.DelveInfo.Seconds")))
                ];
            }
        }
    }
}
