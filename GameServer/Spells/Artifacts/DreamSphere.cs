using System;
using Core.Events;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
    [SpellHandler("DreamMorph")]
    public class DreamMorph : OffensiveProcSpell
	{   	
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			if(effect.Owner is GamePlayer)
			{
				GamePlayer player=effect.Owner as GamePlayer;
				foreach (GameSpellEffect Effect in player.EffectList.GetAllOfType<GameSpellEffect>())
                {
                    if (Effect.SpellHandler.Spell.SpellType.Equals("ShadesOfMist") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("TraitorsDaggerProc") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("DreamGroupMorph") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("MaddeningScalars") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("AtlantisTabletMorph") || 
                        Effect.SpellHandler.Spell.SpellType.Equals("AlvarusMorph"))
                    {
                        player.Out.SendMessage("You already have an active morph!", EChatType.CT_SpellResisted, EChatLoc.CL_ChatWindow);
                        return;
                    }
                }
				if(player.PlayerClass.ID!=(byte)EPlayerClass.Necromancer && (ushort)Spell.LifeDrainReturn > 0) 
                    player.Model = (ushort)Spell.LifeDrainReturn;
				player.Out.SendUpdatePlayer();
			}
		}

		public override int OnEffectExpires(GameSpellEffect effect,bool noMessages)
		{
			if(effect.Owner is GamePlayer)
			{
				GamePlayer player=effect.Owner as GamePlayer; 				
				if(player.PlayerClass.ID!=(byte)EPlayerClass.Necromancer) player.Model = player.CreationModel;
				player.Out.SendUpdatePlayer();
			}	
			return base.OnEffectExpires(effect,noMessages);
		}

        public DreamMorph(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    
    /// <summary>
    /// Dream Sphere group morph spell handler
    /// The DoT proc is a subspell, affects only caster
    /// </summary> 

    //http://support.darkageofcamelot.com/kb/article.php?id=745
    //- The Panther Form level 10 ability of the Dreamsphere artifact has been changed.
    //When a character in panther form is attacked, they revert to normal form and lose all associated bonuses. 
    //This change is specific to the Dreamsphere only and does not affect other shapechange forms

    //http://www.daoc-toa.net/img/dreamPrey.jpg
    //http://www.daoc-toa.net/img/dreamCat.jpg
    [SpellHandler("DreamGroupMorph")]
    public class DreamGroupMorph : DreamMorph
    {
    	private GameSpellEffect m_effect = null;
        public override void OnEffectStart(GameSpellEffect effect)
        {
         	m_effect = effect;    
        	base.OnEffectStart(effect);
            GamePlayer player = effect.Owner as GamePlayer;
			foreach (GameSpellEffect Effect in player.EffectList.GetAllOfType<GameSpellEffect>())
            {
                if (Effect.SpellHandler.Spell.SpellType.Equals("ShadesOfMist") ||
                    Effect.SpellHandler.Spell.SpellType.Equals("TraitorsDaggerProc") ||
                    Effect.SpellHandler.Spell.SpellType.Equals("DreamMorph") ||
                    Effect.SpellHandler.Spell.SpellType.Equals("MaddeningScalars") ||
                    Effect.SpellHandler.Spell.SpellType.Equals("AtlantisTabletMorph") || 
                    Effect.SpellHandler.Spell.SpellType.Equals("AlvarusMorph"))
                {
                    player.Out.SendMessage("You already have an active morph!", EChatType.CT_SpellResisted, EChatLoc.CL_ChatWindow);
                    return;
                }
            }
            if(player == null) return;
            //GameEventMgr.AddHandler(player, GamePlayerEvent.TakeDamage, new DOLEventHandler(LivingTakeDamage));
			GameEventMgr.AddHandler(player, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(LivingTakeDamage));
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            if(player == null) return base.OnEffectExpires(effect, noMessages);
            //GameEventMgr.RemoveHandler(player, GamePlayerEvent.TakeDamage, new DOLEventHandler(LivingTakeDamage));
			GameEventMgr.RemoveHandler(player, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(LivingTakeDamage));
            return base.OnEffectExpires(effect, noMessages);
        }
        // Event : player takes damage, effect cancels
        private void LivingTakeDamage(CoreEvent e, object sender, EventArgs args)
        {
            OnEffectExpires(m_effect, true);
        }
        public DreamGroupMorph(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
