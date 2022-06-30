/*
 *
 * Atlas -  Summon Siege Ram
 *
 */

using System.Collections.Generic;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler("SummonSiegeRam")]
    public class SummonSiegeRam : SpellHandler
    {
	    public SummonSiegeRam(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }


        public override bool StartSpell(GameLiving target)
        {
	        if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon){
		        MessageToCaster("You cannot use siege weapons here!", eChatType.CT_SpellResisted);
		        return false;
	        }

	        if (Caster is not GamePlayer player)
		        return false;

	        if (!player.CurrentZone.IsOF || player.CurrentRegion.IsDungeon)
	        {
		        MessageToCaster("You cannot use siege weapons here!", eChatType.CT_SpellResisted);
		        return false;
	        }
	        
	        if (target is not (GameKeepDoor or GameRelicDoor))
	        {
		        MessageToCaster("You need to target a door!", eChatType.CT_SpellResisted);
		        return false;
	        }
            
	        if (!target.IsAttackable)
	        {
		        MessageToCaster("You cannot attack your target.", eChatType.CT_SpellResisted);
		        return false;
	        }
	        
	        if (!Caster.IsWithinRadius(target, 500))
	        {
		        player.Out.SendMessage("You are too far away to attack " + Caster.TargetObject.Name, eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
		        return false;
	        }
	        
	        if (Caster.GetDistanceTo(target) < 200)
	        {
		        player.Out.SendMessage("You are too close to attack " + Caster.TargetObject.Name, eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
		        return false;
	        }
	        
	        //Limit 2 Rams in a certain radius
			int ramSummonRadius = 200;
            int ramsInRadius = 0;
			foreach (GameNPC npc in Caster.CurrentRegion.GetNPCsInRadius(Caster.X, Caster.Y, Caster.Z, (ushort)(ramSummonRadius), false, false))
			{
				if(npc is GameSiegeRam ram && ram.Realm == Caster.Realm)
					ramsInRadius++;	
			}

			if (ramsInRadius >= 2)
			{
				MessageToCaster("Too many rams in this area and you cannot summon another ram here!", eChatType.CT_SpellResisted);
                return false;
			}

            return base.StartSpell(target);
        }


	    public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
	    {
		    base.ApplyEffectOnTarget(target, effectiveness);

		    if (Caster is not GamePlayer player) return;

		    GameSiegeRam ram = new GameSiegeRam();
            ram.X = Caster.X;
            ram.Y = Caster.Y;
            ram.Z = Caster.Z;
            ram.Heading = Caster.Heading;
            ram.CurrentRegion = Caster.CurrentRegion;
            ram.Realm = Caster.Realm;

            //determine the ram level based on Spell Damage
            switch(Spell.Damage)
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

            player.MountSteed(ram,true);
            ram.TakeControl(player);
	    }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
	        if (selectedTarget is not (GameKeepDoor or GameRelicDoor))
	        {
		        MessageToCaster("You need to target a door!", eChatType.CT_SpellResisted);
		        return false;
	        }
	        
            return base.CheckBeginCast(selectedTarget);
        }

        public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add(Spell.Description);

				return list;
			}
		}
    }
}
