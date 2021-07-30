using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.Language;

namespace DOL.GS
{
    //this component will hold all data related to casting spells
    public class CastingComponent
    {
	    //entity casting the spell
        public GameLiving owner;
        
		/// Multiplier for melee and magic.
		public double Effectiveness
        {
            get { return 1.0; }
            set { }
        }
    
        public  bool IsCasting
        {
            get { return spellHandler != null && spellHandler.IsCasting; }
        }


        //data for the spell that they are casting
        public ISpellHandler spellHandler;


        public CastingComponent(GameLiving owner)
        {
            this.owner = owner;
        }
        

        //move Tick and StartCastSpell to Systems
        public void Tick(long time)
        {
	        spellHandler?.Tick(time);
        }

        public void StartCastSpell(Spell spell, SpellLine line)
        {
            System.Console.WriteLine("Creating spell handler for spell: " + spell.ToString());
	        spellHandler = ScriptMgr.CreateSpellHandler(owner, spell, line);
        }

    }
}