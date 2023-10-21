using Core.Database;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
	public class NfRaResoluteMinionAbility : Rr5RealmAbility
    {
		public const int DURATION = 60000;
		public NfRaResoluteMinionAbility(DbAbility dba, int level) : base(dba, level) { }
        public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			GamePlayer player = living as GamePlayer;
			if (player == null) return;
			if (player.ControlledBrain == null) return;
			if (player.ControlledBrain.Body == null) return;
			player.ControlledBrain.Body.AddAbility(SkillBase.GetAbility(Abilities.CCImmunity));
			new ResoluteMinionEffect().Start(player.ControlledBrain.Body);
			foreach (GamePlayer visPlayer in player.GetPlayersInRadius((ushort)WorldMgr.VISIBILITY_DISTANCE))
				visPlayer.Out.SendSpellEffectAnimation(player, player.ControlledBrain.Body, 7047, 0, false, 0x01);
			DisableSkill(living);
        }
		public override int GetReUseDelay(int level) { return 300; }
    }
}

namespace Core.GS.Effects
{
	public class ResoluteMinionEffect : TimedEffect
	{
		public ResoluteMinionEffect() : base(RealmAbilities.NfRaResoluteMinionAbility.DURATION) { }
		private GameNpc m_pet;
		public void Start(GameNpc controllednpc) { base.Start(controllednpc); m_pet = controllednpc; }
		public override void Stop()
		{
			if (m_pet != null)
			{
				if (m_pet.EffectList.GetOfType<ResoluteMinionEffect>() != null) m_pet.EffectList.Remove(this);
				if (m_pet.HasAbility(Abilities.CCImmunity)) m_pet.RemoveAbility("CCImmunity");
			}
			base.Stop();
		}
		public override ushort Icon { get { return 7047; } }
	}
}
