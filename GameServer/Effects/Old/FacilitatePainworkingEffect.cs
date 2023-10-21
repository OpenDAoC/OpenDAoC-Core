using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class FacilitatePainworkingEffect : GameSpellEffect
    {
        public FacilitatePainworkingEffect(ISpellHandler handler, int duration, int pulseFreq, double effectiveness)
            : base(handler, duration, pulseFreq, effectiveness)
        {
        }
    }
}
