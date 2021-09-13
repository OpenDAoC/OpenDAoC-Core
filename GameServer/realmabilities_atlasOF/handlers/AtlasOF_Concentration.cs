using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_Concentration : ConcentrationAbility
	{
		public AtlasOF_Concentration(DBAbility dba, int level) : base(dba, level) { }

        public override int MaxLevel { get { return 1; } }
        public override int GetReUseDelay(int level) { return 900; } // 15 min
        public override int CostForUpgrade(int level) { return 10; }
        public override bool CheckRequirement(GamePlayer player) { return AtlasRAHelpers.HasAugAcuityLevel(player, 3); }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

            GamePlayer player = living as GamePlayer;
            if (player != null)
            {
                Ability QuickcastAbility = player.GetAbility(Abilities.Quickcast);

                if (QuickcastAbility == null)
                    return;

                // Is Quickcast's cooldown actually active?
                if (player.GetSkillDisabledDuration(player.GetAbility(Abilities.Quickcast)) > 0)
                {
                    player.TempProperties.setProperty(GamePlayer.QUICK_CAST_CHANGE_TICK, 0);
                    player.RemoveDisabledSkill(SkillBase.GetAbility(Abilities.Quickcast));
                    DisableSkill(living);

                    // Force the icon in the client to re-enable by updating its disabled time to 1ms
                    var disables = new List<Tuple<Skill, int>>();
                    disables.Add(new Tuple<Skill, int>(player.GetAbility(Abilities.Quickcast), 1));
                    player.Out.SendDisableSkill(disables);

                    SendCasterSpellEffectAndCastMessage(living, 7006, true);
                }
                else
                {
                    player.DisableSkill(this, 1000);
                    SendCasterSpellEffectAndCastMessage(living, 7006, false);
                }
            }
        }
    }
}