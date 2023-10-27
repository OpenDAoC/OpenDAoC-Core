using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.RealmAbilities;

public class OfRaConcentrationAbility : NfRaConcentrationAbility
{
	public OfRaConcentrationAbility(DbAbility dba, int level) : base(dba, level) { }

    public override int MaxLevel { get { return 1; } }
    public override int GetReUseDelay(int level) { return 900; } // 15 min
    public override int CostForUpgrade(int level) { return 10; }
    public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugAcuityLevel(player) >= 3; }

    public override void Execute(GameLiving living)
    {
        if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

        GamePlayer player = living as GamePlayer;
        if (player != null)
        {
            if(player.PlayerClass.ID == (int)EPlayerClass.Necromancer)
            {
                
                Skill FacilitatePainworking = null;
                ICollection<Skill> disabledSkills = player.GetAllDisabledSkills();
                foreach(Skill skill in disabledSkills)
                {
                    if(skill.Name == "Facilitate Painworking")
                    {
                        FacilitatePainworking = skill;
                        break;
                    }   
                }

                if (FacilitatePainworking == null)
                    return;

                
                // Is FacilitatePainWorking cooldown actually active?
                if (player.GetSkillDisabledDuration(FacilitatePainworking) > 0)
                {
                    player.RemoveDisabledSkill(FacilitatePainworking);
                    DisableSkill(living);

                    // Force the icon in the client to re-enable by updating its disabled time to 0
                    var disables = new List<Tuple<Skill, int>>();
                    disables.Add(new Tuple<Skill, int>(FacilitatePainworking, 0));
                    player.Out.SendDisableSkill(disables);

                    SendCasterSpellEffectAndCastMessage(living, 7006, true);
                }
                else
                {
                    player.DisableSkill(this, 1000);
                    SendCasterSpellEffectAndCastMessage(living, 7006, false);
                }
            }
            else
            {
                Ability QuickcastAbility = player.GetAbility(AbilityConstants.Quickcast);

                if (QuickcastAbility == null)
                    return;

                // Is Quickcast's cooldown actually active?
                if (player.GetSkillDisabledDuration(player.GetAbility(AbilityConstants.Quickcast)) > 0)
                {
                    player.TempProperties.SetProperty(GamePlayer.QUICK_CAST_CHANGE_TICK, 0);
                    player.RemoveDisabledSkill(SkillBase.GetAbility(AbilityConstants.Quickcast));
                    DisableSkill(living);

                    // Force the icon in the client to re-enable by updating its disabled time to 1ms
                    var disables = new List<Tuple<Skill, int>>();
                    disables.Add(new Tuple<Skill, int>(player.GetAbility(AbilityConstants.Quickcast), 1));
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