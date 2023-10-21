using System;

namespace Core.Events;

public class UseSlotEventArgs : EventArgs
{
	private int m_slot;
	private int m_type;

	/// <summary>
	/// Constructs new UseSlotEventArgs
	/// </summary>
	/// <param name="slot">The used slot</param>
	/// <param name="type">The type of 'use' used (0=simple click on icon, 1=/use, 2=/use2)</param>
	public UseSlotEventArgs(int slot, int type)
	{
		this.m_slot = slot;
		this.m_type = type;
	}

	/// <summary>
	/// Gets the slot that was used
	/// </summary>
	public int Slot
	{
		get { return m_slot; }
	}

	/// <summary>
	/// Gets the type of 'use' used (0=simple click on icon, 1=/use, 2=/use2)
	/// </summary>
	public int Type
	{
		get { return m_type; }
	}
}