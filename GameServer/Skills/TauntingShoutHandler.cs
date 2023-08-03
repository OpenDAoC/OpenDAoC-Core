using System;
using DOL.GS.PacketHandler;
using DOL.GS;

namespace DOL.GS.SkillHandler
{
    /// <summary>
    /// Handler for Fury shout
    /// </summary>
    [SkillHandlerAttribute(Abilities.TauntingShout)]
    public class TauntingShoutHandler : SpellCastingHandler
    {
		public override long Preconditions
		{
			get
			{
				return DEAD | SITTING | MEZZED | STUNNED;
			}
		}
		public override int SpellID
		{
			get
			{
				return 14377;
			}
		}     
    }
}