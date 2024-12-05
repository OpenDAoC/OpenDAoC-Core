namespace DOL.GS
{
	/// <summary>
	/// GameMovingObject is a base class for boats and siege weapons.
	/// </summary>		
	public abstract class GameMovingObject : GameNPC
	{
		public GameMovingObject() : base()
		{
		}

		public virtual ushort Type()
		{
			return 2;
		}

		private ushort m_emblem;
		public ushort Emblem
		{
			get { return m_emblem; }
			set { m_emblem = value; }
		}

		public override ushort Model
		{
			get
			{
				return base.Model;
			}
			set
			{
				base.Model = value;
				if(ObjectState==eObjectState.Active)
					ClientService.UpdateNpcForPlayers(this);
			}
		}

		public override bool IsWorthReward => false;

		/// <summary>
		/// This methode is override to remove XP system
		/// </summary>
		/// <param name="source">the damage source</param>
		/// <param name="damageType">the damage type</param>
		/// <param name="damageAmount">the amount of damage</param>
		/// <param name="criticalAmount">the amount of critical damage</param>
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			//Work around the XP system
			if (IsAlive)
			{
				Health -= (damageAmount + criticalAmount);
				if (!IsAlive)
				{
					Health = 0;
					Die(source);
				}
			}
		}
	}
}
