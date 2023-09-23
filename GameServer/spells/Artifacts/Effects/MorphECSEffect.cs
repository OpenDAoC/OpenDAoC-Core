using System;
using System.Linq;
using DOL.GS;
using DOL.GS.Spells;
using Microsoft.Win32.SafeHandles;

public class MorphECSEffect : StatBuffECSEffect
{
    private ushort originalModel;
    public override void OnStartEffect()
    {
        TryCastProc();
        originalModel = Owner.Model;
        Owner.Model = (ushort)SpellHandler.Spell.LifeDrainReturn;    
        if(Owner is GamePlayer p)
        {
            p.Out.SendUpdatePlayer();  
            p.Out.SendCharStatsUpdate();
            p.UpdateEncumberance();
            p.UpdatePlayerStatus();
            p.Out.SendUpdatePlayer(); 
        }
        else
        {
            Owner.BroadcastLivingEquipmentUpdate();
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
        else
        {
            Owner.Model = originalModel;
            Owner.BroadcastLivingEquipmentUpdate();
        }
    }

    public MorphECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
    }

    public void TryCastProc()
    {
        var subspell = SkillBase.GetSpellByID((int) SpellHandler.Spell.SubSpellID);
        if (subspell == null) return;
        var subspellHandler = ScriptMgr.CreateSpellHandler(Owner, subspell, SkillBase.GetSpellLine(subspell.SpellType.ToString())) as SpellHandler;
        
        if (subspellHandler != null)
        {
            subspellHandler.Spell.Level = this.SpellHandler.Spell.Level;
            subspellHandler.StartSpell(Owner);
        }
    }
}