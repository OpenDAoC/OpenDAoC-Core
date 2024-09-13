using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.SummonCommander)]
    public class SummonCommanderPet : SummonSpellHandler
    {
        public SummonCommanderPet(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Caster is GamePlayer playerCaster && playerCaster.ControlledBrain != null)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.CheckBeginCast.Text"), eChatType.CT_SpellResisted);
                return false;
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override void OnPetReleased(GameSummonedPet pet)
        {
            CommanderPet commanderPet = pet as CommanderPet;

            if (commanderPet == null)
                return;

            if (commanderPet.ControlledNpcList != null)
            {
                foreach (BdPetBrain bdPetBrain in commanderPet.ControlledNpcList)
                    bdPetBrain?.OnRelease();
            }

            base.OnPetReleased(commanderPet);
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            return new CommanderBrain(owner);
        }

        protected override GameSummonedPet GetGamePet(INpcTemplate template)
        {
            return new CommanderPet(template);
        }

        /// <summary>
        /// Delve info string.
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var delve = new List<string>();
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.DelveInfo.Text1"));
                delve.Add("");
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.DelveInfo.Text2"));
                delve.Add("");
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.DelveInfo.Text3", Spell.Target));
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.DelveInfo.Text4", Math.Abs(Spell.Power)));
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.DelveInfo.Text5", (Spell.CastTime / 1000).ToString("0.0## " + (LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "Effects.DelveInfo.Seconds")))));
                return delve;
            }
        }
    }
}
