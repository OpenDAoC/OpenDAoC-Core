/*
 *
 * Atlas -  Summon Siege Ram
 *
 */

using System.Collections.Generic;

namespace DOL.GS.Spells
{
    [SpellHandler("SummonSiegeRam")]
    public class SummonSiegeRam : SpellHandler
    {
	    public SummonSiegeRam(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }


        public override bool StartSpell(GameLiving target)
        {
            if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon)
            {
			    MessageToCaster("You cannot use siege weapons here!", PacketHandler.eChatType.CT_SpellResisted);
			    return false;
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

            GameSiegeRam ram = new GameSiegeRam();
            ram.X = Caster.X;
            ram.Y = Caster.Y;
            ram.Z = Caster.Z;
            ram.CurrentRegion = Caster.CurrentRegion;
            ram.Realm = Caster.Realm;

            //determine the ram level based on Spell Damage
            switch(this.Spell.Damage)
            {
                case 0:
                    ram.Level = 0;
                    ram.Name = "mini siege ram";
                    ram.Model = 2605;
                    break;
                case 1:
                    ram.Level = 1;
                    ram.Name = "light siege ram";
                    ram.Model = 2600;
                    break;
                case 2:
                    ram.Level = 2;
                    ram.Name = "medium siege ram";
                    ram.Model = 2601;
                    break;
                case 3:
                    ram.Level = 3;
                    ram.Name = "heavy siege ram";
                    ram.Model = 2602;
                    break;
            }

            ram.AddToWorld();
            if(Caster is GamePlayer player)
            {
                player.MountSteed(ram,true);
                ram.TakeControl(player);
            }
        }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon)
            {
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
