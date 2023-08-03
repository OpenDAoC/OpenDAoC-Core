using System;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Maddening Scalars Defensive proc spell handler
    /// </summary>
    [SpellHandlerAttribute("MaddeningScalars")]
    public class MaddeningScalarsHandler : OffensiveProcSpellHandler
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
                        player.Out.SendMessage("You already have an active morph!", DOL.GS.PacketHandler.EChatType.CT_SpellResisted, DOL.GS.PacketHandler.EChatLoc.CL_ChatWindow);
                        return;
                    }
                }
	            if(player.CharacterClass.ID!=(byte)ECharacterClass.Necromancer && (ushort)Spell.LifeDrainReturn > 0)
                    player.Model = (ushort)Spell.LifeDrainReturn; // 102 official model
	   			player.Out.SendUpdatePlayer();
   			}
   		}
   		
  		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
  			if(effect.Owner is GamePlayer)
            {
	            GamePlayer player = effect.Owner as GamePlayer; 				
  				if(player.CharacterClass.ID!=(byte)ECharacterClass.Necromancer)
                    player.Model = player.CreationModel;
    			player.Out.SendUpdatePlayer();
    		}	
    		return base.OnEffectExpires(effect,noMessages);
  		}
        
        public MaddeningScalarsHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}