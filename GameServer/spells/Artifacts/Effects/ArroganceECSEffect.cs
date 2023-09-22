using DOL.GS;

public class ArroganceECSEffect : StatBuffECSEffect
{
    public ArroganceECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
    }

    public override void OnStartEffect()
    {
        ArroganceHelper.UpdateAllStats(Owner, (int)SpellHandler.Spell.Value, true);                      
            
        if (Owner is GamePlayer player)
        {
            ArroganceHelper.UpdatePlayer(player);
        }
    }

    public override void OnStopEffect()
    {
        ArroganceHelper.UpdateAllStats(Owner, (int)SpellHandler.Spell.Value, false);
        new ArrogancePenaltyECSEffect(new ECSGameEffectInitParams(Owner, SpellHandler.Spell.Duration, Effectiveness, SpellHandler));
        
        if (Owner is GamePlayer player)
        {
            ArroganceHelper.UpdatePlayer(player);
        }
    }
}

public class ArrogancePenaltyECSEffect : ECSGameSpellEffect
{
    public override void OnStartEffect()
    {
        ArroganceHelper.UpdateAllStats(Owner,(int)SpellHandler.Spell.Value, false);
             
        if (Owner is GamePlayer player)
        {
            ArroganceHelper.UpdatePlayer(player);
        }
    }

    public override void OnStopEffect()
    {
        ArroganceHelper.UpdateAllStats(Owner, (int)SpellHandler.Spell.Value, true);                     
            
        if (Owner is GamePlayer player)
        {
            ArroganceHelper.UpdatePlayer(player);
        }
    }

    public ArrogancePenaltyECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
    }

    public override bool HasPositiveEffect => false;
}

static class ArroganceHelper
{
    public static void UpdatePlayer(GamePlayer player)
    {
        player.Out.SendCharStatsUpdate();
        player.UpdateEncumberance();
        player.UpdatePlayerStatus();
        player.Out.SendUpdatePlayer();    
    }

    public static void UpdateAllStats(GameLiving owner,int value, bool add)
    {
        if (add)
        {
            owner.BaseBuffBonusCategory[(int)eProperty.Dexterity] += value;
            owner.BaseBuffBonusCategory[(int)eProperty.Strength] += value;
            owner.BaseBuffBonusCategory[(int)eProperty.Constitution] += value;
            owner.BaseBuffBonusCategory[(int)eProperty.Acuity] += value;
            owner.BaseBuffBonusCategory[(int)eProperty.Piety] += value;
            owner.BaseBuffBonusCategory[(int)eProperty.Empathy] += value;
            owner.BaseBuffBonusCategory[(int)eProperty.Quickness] += value;
            owner.BaseBuffBonusCategory[(int)eProperty.Intelligence] += value;
            owner.BaseBuffBonusCategory[(int)eProperty.Charisma] += value;
            owner.BaseBuffBonusCategory[(int)eProperty.ArmorAbsorption] += value;
        }
        else
        {
            owner.BaseBuffBonusCategory[(int)eProperty.Dexterity] -= value;
            owner.BaseBuffBonusCategory[(int)eProperty.Strength] -= value;
            owner.BaseBuffBonusCategory[(int)eProperty.Constitution] -= value;
            owner.BaseBuffBonusCategory[(int)eProperty.Acuity] -= value;
            owner.BaseBuffBonusCategory[(int)eProperty.Piety] -= value;
            owner.BaseBuffBonusCategory[(int)eProperty.Empathy] -= value;
            owner.BaseBuffBonusCategory[(int)eProperty.Quickness] -= value;
            owner.BaseBuffBonusCategory[(int)eProperty.Intelligence] -= value;
            owner.BaseBuffBonusCategory[(int)eProperty.Charisma] -= value;
            owner.BaseBuffBonusCategory[(int)eProperty.ArmorAbsorption] -= value;
        }
    }
}