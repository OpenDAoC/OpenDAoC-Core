using System;
using DOL.GS;

public class MorphECSEffect : ECSGameSpellEffect
{
    public override void OnStartEffect()
    {
        if(Owner is GamePlayer p)
        {
            p.Model = (ushort)SpellHandler.Spell.LifeDrainReturn;     
            p.Out.SendUpdatePlayer();  
            p.Out.SendCharStatsUpdate();
            p.UpdateEncumberance();
            p.UpdatePlayerStatus();
            p.Out.SendUpdatePlayer(); 
        }       	
    }

    public override void OnStopEffect()
    {
        if(Owner is GamePlayer p)
        {
            GameClient client = p.Client;
            p.Model = (ushort)client.Account.Characters[client.ActiveCharIndex].CreationModel;            	
            p.Out.SendUpdatePlayer(); 
            p.Out.SendCharStatsUpdate();
            p.UpdateEncumberance();
            p.UpdatePlayerStatus();
            p.Out.SendUpdatePlayer(); 
        }                       
    }

    public MorphECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
    }
}