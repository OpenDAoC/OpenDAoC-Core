namespace DOL.GS
{
	/// <summary>
	/// Holds a static item in the world that will disappear after some interval
	/// </summary>
	public class GameStaticItemTimed : GameStaticItem
	{
		/// <summary>
		/// How long this object can stay in the world without being removed
		/// </summary>
		protected uint m_removeDelay = 120000; //Currently 2 mins
		/// <summary>
		/// The timer that will remove this object from the world after a delay
		/// </summary>
		protected RemoveItemAction m_removeItemAction;

		/// <summary>
		/// Creates a new static item that will disappear after 2 minutes
		/// </summary>
		public GameStaticItemTimed() : base()
		{
		}

		/// <summary>
		/// Creates a new static item that will disappear after the given
		/// tick-count
		/// </summary>
		/// <param name="vanishTicks">milliseconds after which the item will vanish</param>
		public GameStaticItemTimed(uint vanishTicks): this()
		{
			if(vanishTicks > 0)
				m_removeDelay = vanishTicks;
		}

		/// <summary>
		/// Gets or Sets the delay in gameticks after which this object is removed
		/// </summary>
		public uint RemoveDelay
		{
			get 
			{
				return m_removeDelay;
			}
			set
			{
				if(value>0)
					m_removeDelay=value;
				if(m_removeItemAction.IsAlive)
					m_removeItemAction.Start((int)m_removeDelay);
			}
		}

		/// <summary>
		/// Adds this object to the world
		/// </summary>
		/// <returns>true if successfull</returns>
		public override bool AddToWorld()
		{
			if(!base.AddToWorld()) return false;
			if (m_removeItemAction == null)
				m_removeItemAction = new RemoveItemAction(this);
			m_removeItemAction.Start((int)m_removeDelay);
			return true;
		}

		public override bool RemoveFromWorld()
		{
			if (RemoveFromWorld(RespawnInterval))
			{
				m_removeItemAction?.Stop();
				return true;
			}

			return false;
		}

		/// <summary>
		/// The callback function that will remove this bag after some time
		/// </summary>
		protected class RemoveItemAction : ECSGameTimerWrapperBase
		{
			/// <summary>
			/// Constructs a new remove action
			/// </summary>
			/// <param name="item"></param>
			public RemoveItemAction(GameStaticItemTimed item) : base(item) { }

			/// <summary>
			/// The callback function that will remove this bag after some time
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				GameStaticItem item = (GameStaticItem) timer.Owner;
				//remove this object from the world after some time
				item.Delete();
				return 0;
			}
		}
	}
}
