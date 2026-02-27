using DOL.GS.Effects;
using DOL.GS.PlayerClass;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Maddening Scalars Defensive proc spell handler
    /// </summary>
    [SpellHandler(eSpellType.MaddeningScalars)]
    public class MaddeningScalars : OffensiveProcSpellHandler
    {   	
   		public override void OnEffectStart(GameSpellEffect effect)
		{
            base.OnEffectStart(effect);
            if(effect.Owner is GamePlayer)
            {
	            GamePlayer player = effect.Owner as GamePlayer;
				foreach (GameSpellEffect Effect in player.EffectList.GetAllOfType<GameSpellEffect>())
                {
                    if (Effect.SpellHandler.Spell.SpellType.Equals("ShadesOfMist") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("TraitorsDaggerProc") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("DreamMorph") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("DreamGroupMorph") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("AtlantisTabletMorph") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("AlvarusMorph"))
                    {
                        player.Out.SendMessage("You already have an active morph!", DOL.GS.PacketHandler.eChatType.CT_SpellResisted, DOL.GS.PacketHandler.eChatLoc.CL_ChatWindow);
                        return;
                    }
                }
	            if(player.CharacterClass is not ClassDisciple && (ushort)Spell.LifeDrainReturn > 0)
                    player.Model = (ushort)Spell.LifeDrainReturn; // 102 official model
	   			player.Out.SendUpdatePlayer();
   			}
   		}
   		
  		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
  			if(effect.Owner is GamePlayer)
            {
	            GamePlayer player = effect.Owner as GamePlayer;
  				if(player.CharacterClass is ClassDisciple)
                    player.Model = player.CreationModel;
    			player.Out.SendUpdatePlayer();
    		}	
    		return base.OnEffectExpires(effect,noMessages);
  		}
        
        public MaddeningScalars(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
