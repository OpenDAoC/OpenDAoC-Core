using System;

namespace DOL.Events;
	
class QuestRewardChosenEventArgs : EventArgs
{
	private int m_questGiverID;
	private int m_questID;
	private int m_countChosen;
	private int[] m_itemsChosen;

	/// <summary>
	/// Constructs arguments for a QuestRewardChosen event.
	/// </summary>
	public QuestRewardChosenEventArgs(int questGiverID, int questID, int countChosen, int[] itemsChosen)
	{
		m_questGiverID = questGiverID;
		m_questID = questID;
		m_countChosen = countChosen;
		m_itemsChosen = itemsChosen;
	}

	/// <summary>
	/// ID of the NPC that gave the quest.
	/// </summary>
	public int QuestGiverID
	{
		get { return m_questGiverID; }
	}

	/// <summary>
	/// ID of the quest.
	/// </summary>
	public int QuestID
	{
		get { return m_questID; }
	}

	/// <summary>
	/// Number of rewards picked from the quest window.
	/// </summary>
	public int CountChosen
	{
		get { return m_countChosen; }
	}

	/// <summary>
	/// List of items (0-7) that were picked.
	/// </summary>
	public int[] ItemsChosen
	{
		get { return m_itemsChosen; }
	}
}