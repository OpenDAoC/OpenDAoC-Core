using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    /// <summary>
    /// All stats debuff spell handler
    /// </summary>
    [SpellHandler(eSpellType.AllStatsDebuff)]
    public class AllStatsDebuff : SpellHandler
    {
		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

        public override void OnEffectStart(GameSpellEffect effect)
        {    
     		base.OnEffectStart(effect);            
            effect.Owner.DebuffCategory[eProperty.Dexterity] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Strength] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Constitution] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Acuity] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Piety] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Empathy] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Quickness] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Intelligence] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Charisma] += (int)m_spell.Value;   
            effect.Owner.DebuffCategory[eProperty.PhysicalAbsorption] += (int)m_spell.Value; 
            effect.Owner.DebuffCategory[eProperty.MagicAbsorption] += (int)m_spell.Value; 
            
            if(effect.Owner is GamePlayer)
            {
            	GamePlayer player = effect.Owner as GamePlayer;  
                player.Out.SendCharStatsUpdate();
                player.UpdateEncumbrance();
                player.UpdatePlayerStatus();
            	player.Out.SendUpdatePlayer();             	
            }
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {  
            effect.Owner.DebuffCategory[eProperty.Dexterity] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Strength] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Constitution] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Acuity] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Piety] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Empathy] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Quickness] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Intelligence] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[eProperty.Charisma] -= (int)m_spell.Value;        
            effect.Owner.DebuffCategory[eProperty.PhysicalAbsorption] -= (int)m_spell.Value; 
            effect.Owner.DebuffCategory[eProperty.MagicAbsorption] -= (int)m_spell.Value; 
 
            if(effect.Owner is GamePlayer)
            {
            	GamePlayer player = effect.Owner as GamePlayer;    
                player.Out.SendCharStatsUpdate();
                player.UpdateEncumbrance();
                player.UpdatePlayerStatus();
            	player.Out.SendUpdatePlayer();  
            }                       
            return base.OnEffectExpires(effect, noMessages);
        }

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(target);
			if (target.Realm == 0 || Caster.Realm == 0)
			{
				target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
            }
			else
			{
				target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
                Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
            }
		}		
        public AllStatsDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
 
    /// <summary>
    /// Lore debuff spell handler (Magic resist debuff)
    /// </summary>
    [SpellHandler(eSpellType.LoreDebuff)]
    public class LoreDebuff : SpellHandler
    {
 		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

        public override void OnEffectStart(GameSpellEffect effect)
        {
        	base.OnEffectStart(effect);      
        	effect.Owner.DebuffCategory[eProperty.SpellDamage] += (int)Spell.Value;
            effect.Owner.DebuffCategory[eProperty.Resist_Heat] += (int)Spell.Value;
            effect.Owner.DebuffCategory[eProperty.Resist_Cold] += (int)Spell.Value;
            effect.Owner.DebuffCategory[eProperty.Resist_Matter] += (int)Spell.Value;
            effect.Owner.DebuffCategory[eProperty.Resist_Spirit] += (int)Spell.Value;
            effect.Owner.DebuffCategory[eProperty.Resist_Energy] += (int)Spell.Value;
            
            if(effect.Owner is GamePlayer)
            {
            	GamePlayer player = effect.Owner as GamePlayer;
             	player.Out.SendCharResistsUpdate(); 
             	player.UpdatePlayerStatus();
            }                       
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            effect.Owner.DebuffCategory[eProperty.SpellDamage] -= (int)Spell.Value;
            effect.Owner.DebuffCategory[eProperty.Resist_Heat] -= (int)Spell.Value;
            effect.Owner.DebuffCategory[eProperty.Resist_Cold] -= (int)Spell.Value;
            effect.Owner.DebuffCategory[eProperty.Resist_Matter] -= (int)Spell.Value;
            effect.Owner.DebuffCategory[eProperty.Resist_Spirit] -= (int)Spell.Value;
            effect.Owner.DebuffCategory[eProperty.Resist_Energy] -= (int)Spell.Value;
            
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
				target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
            }
			else
			{
				target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
                Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
            }
		}	
        public LoreDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Strength/Constitution drain spell handler
    /// </summary>
    [SpellHandler(eSpellType.StrengthConstitutionDrain)]
    public class StrengthConstitutionDrain : StrengthConDebuff
    {
		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

        public override void OnEffectStart(GameSpellEffect effect)
        {
        	base.OnEffectStart(effect);         
            Caster.BaseBuffBonusCategory[eProperty.Strength] += (int)m_spell.Value;
            Caster.BaseBuffBonusCategory[eProperty.Constitution] += (int)m_spell.Value;
 
            if(Caster is GamePlayer)
            {
            	GamePlayer player = Caster as GamePlayer;          	
             	player.Out.SendCharStatsUpdate(); 
             	player.UpdateEncumbrance();
             	player.UpdatePlayerStatus();
            } 
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {           
            Caster.BaseBuffBonusCategory[eProperty.Strength] -= (int)m_spell.Value;
            Caster.BaseBuffBonusCategory[eProperty.Constitution] -= (int)m_spell.Value;          
 
            if(Caster is GamePlayer)
            {
            	GamePlayer player = Caster as GamePlayer;          	
             	player.Out.SendCharStatsUpdate(); 
             	player.UpdateEncumbrance();
             	player.UpdatePlayerStatus();
            } 
            return base.OnEffectExpires(effect,noMessages);
        } 
        
        public StrengthConstitutionDrain(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// ABS Damage shield spell handler
    /// </summary>
    [SpellHandler(eSpellType.ABSDamageShield)]
    public class ABSDamageShield : AblativeArmorSpellHandler
    {
        public override void OnDamageAbsorbed(AttackData ad, int DamageAmount)
        {
            AttackData newad = new AttackData();
            newad.Attacker = ad.Target;
            newad.Target = ad.Attacker;
            newad.Damage = DamageAmount;
            newad.DamageType = Spell.DamageType;
            newad.AttackType = AttackData.eAttackType.Spell;
            newad.AttackResult = eAttackResult.HitUnstyled;
            newad.SpellHandler = this;
            newad.Target.OnAttackedByEnemy(newad);
            newad.Attacker.DealDamage(newad);
        }
        public ABSDamageShield(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    
    /// <summary>
    /// Morph spell handler
    /// </summary>
    [SpellHandler(eSpellType.Morph)]
    public class Morph : SpellHandler
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
    	public Morph(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }   
 
    /// <summary>
    /// Arcane leadership spell handler (range+resist pierce)
    /// </summary>
    [SpellHandler(eSpellType.ArcaneLeadership)]
    public class ArcaneLeadership : CloudsongAuraSpellHandler
    {
    	public ArcaneLeadership(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }   
}
