/*
 *
 * Atlas -  Summon Siege Trebuchet
 *
 */

using System.Collections.Generic;
using System.Linq;
using DOL.GS.Keeps;

namespace DOL.GS.Spells
{
    [SpellHandler("SummonSiegeTrebuchet")]
    public class SummonSiegeTrebuchet : SpellHandler
    {
	    public SummonSiegeTrebuchet(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override bool StartSpell(GameLiving target)
        {
            if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon)
            {
			    MessageToCaster("You cannot use siege weapons here!", PacketHandler.eChatType.CT_SpellResisted);
			    return false;
		    }

            var currentAreas = Caster.CurrentAreas.ToList();
            
            foreach (var area1 in currentAreas)
            {
	            var area = (AbstractArea) area1;
	            if (area is KeepArea {Keep: {IsPortalKeep: true}})
	            {
		            MessageToCaster("You cannot use siege weapons here (PK)!", PacketHandler.eChatType.CT_SpellResisted);
		            return false;
	            }
            }

            //Only allow one treb/catapult in the radius
            int trebSummonRadius = 500;
			foreach (GameNPC npc in Caster.CurrentRegion.GetNPCsInRadius(Caster.X, Caster.Y, Caster.Z, (ushort)(trebSummonRadius), false, false))
			{
				if (npc is GameSiegeCatapult)
				{
					MessageToCaster("You are too close to another trebuchet or catapult and cannot summon here!", PacketHandler.eChatType.CT_SpellResisted);
                    return false;
				}
			}

            return base.StartSpell(target);
        }
        
	    public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
	        if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon){
		        MessageToCaster("You cannot use siege weapons here!", PacketHandler.eChatType.CT_SpellResisted);
		        return;
	        }
	        
            base.ApplyEffectOnTarget(target, effectiveness);
            
            GameSiegeTrebuchet tre = new GameSiegeTrebuchet();
            tre.X = Caster.X;
            tre.Y = Caster.Y;
            tre.Z = Caster.Z;
            tre.Heading = Caster.Heading;
            tre.CurrentRegion = Caster.CurrentRegion;
            tre.Model = 0xA2E;
            tre.Level = 3;
            tre.Name = "trebuchet";
            tre.Realm = Caster.Realm;
            tre.AddToWorld();
            if(Caster is GamePlayer player)
                tre.TakeControl(player);
        }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
	        if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon){
		        MessageToCaster("You cannot use siege weapons here!", PacketHandler.eChatType.CT_SpellResisted);
		        return false;
	        }

            return base.CheckBeginCast(selectedTarget);
        }

        public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add(string.Format("  {0}", Spell.Description));

				return list;
			}
		}
    }
}
