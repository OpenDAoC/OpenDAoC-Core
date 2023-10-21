using Core.GS;

namespace Core.Events;

public class NextCraftingTierReachedEventArgs : System.EventArgs
{
	private ECraftingSkill m_skill;
	private int m_points;
	public ECraftingSkill Skill
	{
		get
		{
			return m_skill;
		}
	}
	public int Points
	{
		get
		{
			return m_points;
		}
	}
	public NextCraftingTierReachedEventArgs(ECraftingSkill skill, int points)
		: base()
	{
		m_skill = skill;
		m_points = points;
	}
}