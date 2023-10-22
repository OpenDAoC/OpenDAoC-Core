using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.Artifacts;

[SpellHandler("Arrogance")]
public class Arrogance : SpellHandler
{
    GamePlayer playertarget = null;
    
    /// <summary>
    /// The timer that will cancel the effect
    /// </summary>
    protected EcsGameTimer m_expireTimer;
    public override void OnEffectStart(GameSpellEffect effect)
    {
        base.OnEffectStart(effect);
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Dexterity] += (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Strength] += (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Constitution] += (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Acuity] += (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Piety] += (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Empathy] += (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Quickness] += (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Intelligence] += (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Charisma] += (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.ArmorAbsorption] += (int)m_spell.Value;                       
        
        if (effect.Owner is GamePlayer)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            player.Out.SendCharStatsUpdate();
            player.UpdateEncumberance();
            player.UpdatePlayerStatus();
            player.Out.SendUpdatePlayer();       
        }
    }

    public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
    {
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Dexterity] -= (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Strength] -= (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Constitution] -= (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Acuity] -= (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Piety] -= (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Empathy] -= (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Quickness] -= (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Intelligence] -= (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.Charisma] -= (int)m_spell.Value;
        effect.Owner.BaseBuffBonusCategory[(int)EProperty.ArmorAbsorption] -= (int)m_spell.Value;
         
        if (effect.Owner is GamePlayer)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            player.Out.SendCharStatsUpdate();
            player.UpdateEncumberance();
            player.UpdatePlayerStatus();
            player.Out.SendUpdatePlayer();  
            Start(player);
        }
        return base.OnEffectExpires(effect,noMessages);
    }

    protected virtual void Start(GamePlayer player)
    {
        playertarget = player;
        StartTimers();
        player.DebuffCategory[(int)EProperty.Dexterity] += (int)m_spell.Value;
        player.DebuffCategory[(int)EProperty.Strength] += (int)m_spell.Value;
        player.DebuffCategory[(int)EProperty.Constitution] += (int)m_spell.Value;
        player.DebuffCategory[(int)EProperty.Acuity] += (int)m_spell.Value;
        player.DebuffCategory[(int)EProperty.Piety] += (int)m_spell.Value;
        player.DebuffCategory[(int)EProperty.Empathy] += (int)m_spell.Value;
        player.DebuffCategory[(int)EProperty.Quickness] += (int)m_spell.Value;
        player.DebuffCategory[(int)EProperty.Intelligence] += (int)m_spell.Value;
        player.DebuffCategory[(int)EProperty.Charisma] += (int)m_spell.Value;
        player.DebuffCategory[(int)EProperty.ArmorAbsorption] += (int)m_spell.Value;
        
        player.Out.SendCharStatsUpdate();
        player.UpdateEncumberance();
        player.UpdatePlayerStatus();
        player.Out.SendUpdatePlayer(); 
    }

    protected virtual void Stop()
    {
        if (playertarget != null)
        {     
            playertarget.DebuffCategory[(int)EProperty.Dexterity] -= (int)m_spell.Value;;
            playertarget.DebuffCategory[(int)EProperty.Strength] -= (int)m_spell.Value;;
            playertarget.DebuffCategory[(int)EProperty.Constitution] -= (int)m_spell.Value;;
            playertarget.DebuffCategory[(int)EProperty.Acuity] -= (int)m_spell.Value;;
            playertarget.DebuffCategory[(int)EProperty.Piety] -= (int)m_spell.Value;;
            playertarget.DebuffCategory[(int)EProperty.Empathy] -= (int)m_spell.Value;;
            playertarget.DebuffCategory[(int)EProperty.Quickness] -= (int)m_spell.Value;;
            playertarget.DebuffCategory[(int)EProperty.Intelligence] -= (int)m_spell.Value;;
            playertarget.DebuffCategory[(int)EProperty.Charisma] -= (int)m_spell.Value;;
            playertarget.DebuffCategory[(int)EProperty.ArmorAbsorption] -= (int)m_spell.Value;;
            
            playertarget.Out.SendCharStatsUpdate();
            playertarget.UpdateEncumberance();
            playertarget.UpdatePlayerStatus();
          	playertarget.Out.SendUpdatePlayer(); 
        }
        StopTimers();
    }
    protected virtual void StartTimers()
    {
        StopTimers();
        m_expireTimer = new EcsGameTimer(playertarget, new EcsGameTimer.EcsTimerCallback(ExpiredCallback), 10000);
    }
    protected virtual void StopTimers()
    {
        if (m_expireTimer != null)
        {
            m_expireTimer.Stop();
            m_expireTimer = null;
        }
    }
    protected virtual int ExpiredCallback(EcsGameTimer callingTimer)
    {
        Stop();
        return 0;
    }

    public Arrogance(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}