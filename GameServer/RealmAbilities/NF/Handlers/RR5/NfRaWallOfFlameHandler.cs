using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
	public class NfRaWallOfFlameHandler : Rr5RealmAbility
	{
		public NfRaWallOfFlameHandler(DbAbilities dba, int level) : base(dba, level) { }

		private int dmgValue = 400; // 400 Dmg
		private uint duration = 15; // 15 Sec duration

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			base.Execute(living);

			GamePlayer caster = living as GamePlayer;
			if (caster == null)
				return;

			if (caster.IsMoving)
			{
				caster.Out.SendMessage("You must be standing still to use this ability!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			foreach (GamePlayer i_player in caster.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
			{
				if (i_player == caster)
				{
					i_player.MessageToSelf("You cast " + this.Name + "!", EChatType.CT_Spell);
				}
				else
				{
					i_player.MessageFromArea(caster, caster.Name + " casts a spell!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
				}

				i_player.Out.SendSpellCastAnimation(caster, 7028, 20);
			}

			Statics.WallOfFlameBase wof = new Statics.WallOfFlameBase(dmgValue);
			Point3D targetSpot = new Point3D(caster.X, caster.Y, caster.Z);
			wof.CreateStatic(caster, targetSpot, duration, 3, 150);

			DisableSkill(living);
			caster.StopCurrentSpellcast();

		}


		public override int GetReUseDelay(int level)
		{
			return 600;
		}
	}
}