using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.Skills;

public class PropertyChangingAbility : Ability
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	// property to modify
	protected EProperty[] m_property;
	public EProperty[] Properties
	{
		get { return m_property; }
	}

	public PropertyChangingAbility(DbAbility dba, int level, EProperty[] property)
		: base(dba, level)
	{
		m_property = property;
	}

	public PropertyChangingAbility(DbAbility dba, int level, EProperty property)
		: base(dba, level)
	{
		m_property = new EProperty[] { property };
	}

	/// <summary>
	/// Get the Amount of Bonus for this ability at a particular level
	/// </summary>
	/// <param name="level">The level</param>
	/// <returns>The amount</returns>
	public virtual int GetAmountForLevel(int level)
	{
		return 0;
	}

	/// <summary>
	/// The bonus amount at this abilities level
	/// </summary>
	public int Amount
	{
		get
		{
			return GetAmountForLevel(Level);
		}
	}

	/// <summary>
	/// send updates about the changes
	/// </summary>
	/// <param name="target"></param>
	public virtual void SendUpdates(GameLiving target)
	{
	}

	/// <summary>
	/// Unit for values like %
	/// </summary>
	protected virtual string ValueUnit { get { return ""; } }

	public override void Activate(GameLiving living, bool sendUpdates)
	{
		if (m_activeLiving == null)
		{
			m_activeLiving = living;
			foreach (EProperty property in m_property)
			{
				living.AbilityBonus[(int)property] += GetAmountForLevel(living.CalculateSkillLevel(this));
			}
			
			if (sendUpdates)
				SendUpdates(living);
		}
		else
		{
			log.WarnFormat("ability {0} already activated on {1}", Name, living.Name);
		}
	}

	public override void Deactivate(GameLiving living, bool sendUpdates)
	{
		if (m_activeLiving != null)
		{
			foreach (EProperty property in m_property)
			{
				living.AbilityBonus[(int)property] -= GetAmountForLevel(living.CalculateSkillLevel(this));
			}
			if (sendUpdates) SendUpdates(living);
			m_activeLiving = null;
		}
		else
		{
			log.Warn("ability " + Name + " already deactivated on " + living.Name);
		}
	}

	public override void OnLevelChange(int oldLevel, int newLevel = 0)
	{
		if (newLevel == 0)
			newLevel = Level;

		foreach (EProperty property in m_property)
		{
			m_activeLiving.AbilityBonus[(int)property] += GetAmountForLevel(newLevel) - GetAmountForLevel(oldLevel);
		}

		SendUpdates(m_activeLiving);
	}
}