using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.ECS;

public class BerserkEcsAbilityEffect : EcsGameAbilityEffect
{
    public BerserkEcsAbilityEffect(EcsGameEffectInitParams initParams)
        : base(initParams)
    {
        EffectType = EEffect.Berserk;
        EffectService.RequestStartEffect(this);
    }

    protected ushort m_startModel = 0;

    public override ushort Icon { get { return 479; } }
    public override string Name { get { return LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.BerserkEffect.Name"); } }
    public override bool HasPositiveEffect { get { return true; } }

    public override void OnStartEffect()
    {
        m_startModel = Owner.Model;

        if (OwnerPlayer != null)
        {
            // "You go into a berserker frenzy!"
            OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.BerserkEffect.StartFrenzy"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            // "{0} goes into a berserker frenzy!"
            MessageUtil.SystemToArea(OwnerPlayer, LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.BerserkEffect.AreaStartFrenzy",OwnerPlayer.GetName(0, true)), EChatType.CT_System, OwnerPlayer);

            OwnerPlayer.Emote(EEmote.MidgardFrenzy);
            //TODO differentiate model between Dwarves and other races
            if (OwnerPlayer.Race == (int)ERace.Dwarf)
            {
                OwnerPlayer.Model = 12;
            }
            else
            {
                OwnerPlayer.Model = 3;
            }
        }
    }
    
    public override void OnStopEffect()
    {
        Owner.Model = m_startModel;

        // there is no animation on end of the effect
        if (OwnerPlayer != null)
        {
            // "Your berserker frenzy ends."
            OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.BerserkEffect.EndFrenzy"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            // "{0}'s berserker frenzy ends."
            MessageUtil.SystemToArea(OwnerPlayer, LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.BerserkEffect.AreaEndFrenzy", OwnerPlayer.GetName(0, true)), EChatType.CT_System, OwnerPlayer);
        }

    }
}