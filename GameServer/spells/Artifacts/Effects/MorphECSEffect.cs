using System;
using System.Linq;
using DOL.GS;
using DOL.GS.Spells;

public class MorphECSEffect : StatBuffECSEffect
{
    public override void OnStartEffect()
    {
        TryCastProc();
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
        //cancel any related subspells
        var effect = Owner.effectListComponent.GetAllEffects().FirstOrDefault(effect => effect.SpellHandler.Spell.ID == SpellHandler.Spell.SubSpellID);
        if (effect != null) EffectService.RequestCancelEffect(effect);
        
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

    public void TryCastProc()
    {
        var subspell = SkillBase.GetSpellByID((int) SpellHandler.Spell.SubSpellID);
        var subspellHandler = ScriptMgr.CreateSpellHandler(Owner, subspell, SkillBase.GetSpellLine(subspell.SpellType.ToString())) as SpellHandler;
        
        if (subspellHandler != null)
        {
            subspellHandler.Spell.Level = this.SpellHandler.Spell.Level;
            subspellHandler.StartSpell(Owner);
        }
    }
}