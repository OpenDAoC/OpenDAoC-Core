using Core.GS.AI;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Artifacts;

/// <summary>
/// Lore debuff spell handler (Magic resist debuff)
/// </summary>
[SpellHandler("LoreDebuff")]
public class LoreDebuffSpell : SpellHandler
{
 	public override int CalculateSpellResistChance(GameLiving target)
	{
		return 0;
	}
    public override void OnEffectStart(GameSpellEffect effect)
    {
        base.OnEffectStart(effect);      
        effect.Owner.DebuffCategory[(int)EProperty.SpellDamage] += (int)Spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Resist_Heat] += (int)Spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Resist_Cold] += (int)Spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Resist_Matter] += (int)Spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Resist_Spirit] += (int)Spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Resist_Energy] += (int)Spell.Value;
        
        if(effect.Owner is GamePlayer)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            player.Out.SendCharResistsUpdate(); 
            player.UpdatePlayerStatus();
        }                       
    }
    public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
    {
        effect.Owner.DebuffCategory[(int)EProperty.SpellDamage] -= (int)Spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Resist_Heat] -= (int)Spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Resist_Cold] -= (int)Spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Resist_Matter] -= (int)Spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Resist_Spirit] -= (int)Spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Resist_Energy] -= (int)Spell.Value;
        
        if(effect.Owner is GamePlayer)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            player.Out.SendCharResistsUpdate(); 
            player.UpdatePlayerStatus();
        }           
        
        return base.OnEffectExpires(effect, noMessages);
    }

	public override void ApplyEffectOnTarget(GameLiving target)
	{
		base.ApplyEffectOnTarget(target);
		if (target.Realm == 0 || Caster.Realm == 0)
		{
			target.LastAttackedByEnemyTickPvE = GameLoopMgr.GameLoopTime;
            Caster.LastAttackTickPvE = GameLoopMgr.GameLoopTime;
        }
		else
		{
			target.LastAttackedByEnemyTickPvP = GameLoopMgr.GameLoopTime;
            Caster.LastAttackTickPvP = GameLoopMgr.GameLoopTime;
        }
		if(target is GameNpc) 
		{
			IOldAggressiveBrain aggroBrain = ((GameNpc)target).Brain as IOldAggressiveBrain;
			if (aggroBrain != null)
				aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
		}
	}	
    public LoreDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Strength/Constitution drain spell handler
/// </summary>
[SpellHandler("StrengthConstitutionDrain")]
public class StrConDrainSpell : StrConDebuff
{  	
	public override int CalculateSpellResistChance(GameLiving target)
	{
		return 0;
	}
    public override void OnEffectStart(GameSpellEffect effect)
    {
        base.OnEffectStart(effect);         
        Caster.BaseBuffBonusCategory[(int)EProperty.Strength] += (int)m_spell.Value;
        Caster.BaseBuffBonusCategory[(int)EProperty.Constitution] += (int)m_spell.Value;

        if(Caster is GamePlayer)
        {
            GamePlayer player = Caster as GamePlayer;          	
            player.Out.SendCharStatsUpdate(); 
            player.UpdateEncumberance();
            player.UpdatePlayerStatus();
        } 
    }

    public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
    {           
        Caster.BaseBuffBonusCategory[(int)EProperty.Strength] -= (int)m_spell.Value;
        Caster.BaseBuffBonusCategory[(int)EProperty.Constitution] -= (int)m_spell.Value;          

        if(Caster is GamePlayer)
        {
            GamePlayer player = Caster as GamePlayer;          	
            player.Out.SendCharStatsUpdate(); 
            player.UpdateEncumberance();
            player.UpdatePlayerStatus();
        } 
        return base.OnEffectExpires(effect,noMessages);
    } 
    
    public StrConDrainSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// ABS Damage shield spell handler
/// </summary>
[SpellHandler("ABSDamageShield")]
public class AbsDamageShieldSpell : AblativeArmorSpell
{
    public override void OnDamageAbsorbed(AttackData ad, int DamageAmount)
    {
        AttackData newad = new AttackData();
        newad.Attacker = ad.Target;
        newad.Target = ad.Attacker;
        newad.Damage = DamageAmount;
        newad.DamageType = Spell.DamageType;
        newad.AttackType = EAttackType.Spell;
        newad.AttackResult = EAttackResult.HitUnstyled;
        newad.SpellHandler = this;
        newad.Target.OnAttackedByEnemy(newad);
        newad.Attacker.DealDamage(newad);
    }
    public AbsDamageShieldSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Morph spell handler
/// </summary>
[SpellHandler("Morph")]
public class MorphSpell : SpellHandler
{
    public override void OnEffectStart(GameSpellEffect effect)
    {    
       if(effect.Owner is GamePlayer)
        {
            GamePlayer player = effect.Owner as GamePlayer;  
            player.Model = (ushort)Spell.LifeDrainReturn;     
            player.Out.SendUpdatePlayer();  
        }       	
     	base.OnEffectStart(effect); 
    }
    
    public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
    {
       if(effect.Owner is GamePlayer)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            GameClient client = player.Client;
 			player.Model = (ushort)client.Account.Characters[client.ActiveCharIndex].CreationModel;            	
 			player.Out.SendUpdatePlayer();  
        }                       
        return base.OnEffectExpires(effect, noMessages);         	
    }    	
    public MorphSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}   

/// <summary>
/// Arcane leadership spell handler (range+resist pierce)
/// </summary>
[SpellHandler("ArcaneLeadership")]
public class ArcaneLeadershipSpell : CloudsongAuraSpellHandler
{
    public ArcaneLeadershipSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}   