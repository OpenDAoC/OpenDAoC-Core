using System.Collections.Generic;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.RealmAbilities
{
	public class NfRaVanishEffect : TimedEffect
	{
		public const string VANISH_BLOCK_ATTACK_TIME_KEY = "vanish_no_attack";

		double m_speedBonus;
		int m_countdown;
		EcsGameTimer m_countDownTimer = null;
		EcsGameTimer m_removeTimer = null;

		public NfRaVanishEffect(int duration, double speedBonus)
			: base(duration)
		{
			m_speedBonus = speedBonus;
			m_countdown = (duration + 500) / 1000;
		}

		public override void Start(GameLiving target)
		{
			base.Start(target);
			GamePlayer player = target as GamePlayer;
			player.attackComponent.StopAttack();
			player.Stealth(true);
			player.Out.SendUpdateMaxSpeed();
			m_countDownTimer = new EcsGameTimer(player, new EcsGameTimer.EcsTimerCallback(CountDown));
			m_countDownTimer.Start(1);
			player.TempProperties.SetProperty(VANISH_BLOCK_ATTACK_TIME_KEY, player.CurrentRegion.Time + 30000);
			m_removeTimer = new EcsGameTimer(player, new EcsGameTimer.EcsTimerCallback(RemoveAttackBlock));
			m_removeTimer.Start(30000);
		}

		public int RemoveAttackBlock(EcsGameTimer timer)
		{
			GamePlayer player = timer.Owner as GamePlayer;
			if (player != null)
				player.TempProperties.RemoveProperty(VANISH_BLOCK_ATTACK_TIME_KEY);
			return 0;
		}

		public override void Stop()
		{
			base.Stop();
			GamePlayer player = Owner as GamePlayer;
			player.Out.SendUpdateMaxSpeed();
			if (m_countDownTimer != null)
			{
				m_countDownTimer.Stop();
				m_countDownTimer = null;
			}
		}

		public int CountDown(EcsGameTimer timer)
		{
			if (m_countdown > 0)
			{
				((GamePlayer)Owner).Out.SendMessage("You are hidden for " + m_countdown + " more seconds!", EChatType.CT_SpellPulse, EChatLoc.CL_SystemWindow);
				m_countdown--;
				return 1000;
			}
			return 0;
		}

		public double SpeedBonus { get { return m_speedBonus; } }

		public override string Name { get { return "Vanish"; } }

		public override ushort Icon { get { return 3019; } }

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add("Vanish effect");
				return list;
			}
		}
	}
}
