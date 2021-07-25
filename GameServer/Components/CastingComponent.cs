using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.Language;

namespace DOL.GS
{
    public class CastingComponent
    {
	    
        public GamePlayer owner;

        public ISpellHandler spellHandler;

        public CastingComponent(GamePlayer owner)
        {
            this.owner = owner;
        }
        
        public void Tick(long time)
        {
	        spellHandler?.Tick(time);
        }

        public void StartCastSpell(Spell spell, SpellLine line)
        {
	        spellHandler = ScriptMgr.CreateSpellHandler(owner, spell, line);
        }

    }
}