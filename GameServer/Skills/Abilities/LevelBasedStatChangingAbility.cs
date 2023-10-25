using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.Skills;

/// <summary>
/// Ability which Level is based on Owner Level instead of Skill Level
/// Each time Level is modified, it's enforced to User's Level if Applicable
/// </summary>
public class LevelBasedStatChangingAbility : StatChangingAbility
{
	/// <summary>
	/// Override Level Setter/Getter to Report Living Level instead of Skill Level.
	/// </summary>
	public override int Level
	{
		get
		{
			// Report Max Value if no living assigned to trigger the ability override
			if (m_activeLiving != null)
				return m_activeLiving.Level;
			
			return int.MaxValue;
		}
		set
		{
			// Override Setter to have Living Level Updated if available.
			base.Level = m_activeLiving == null ? value : m_activeLiving.Level;
		}
	}
	
	/// <summary>
	/// Name with stat amount appended
	/// </summary>
	public override string Name {
		get { return m_activeLiving != null ? string.Format("{0} +{1}", base.Name, GetAmountForLevel(Level)) : base.Name; }
		set { base.Name = value; }
	}
	
	/// <summary>
	/// Activate method Enforcing Living Level for Ability Level  
	/// </summary>
	/// <param name="living">Living Activating this Ability</param>
	/// <param name="sendUpdates">Update Flag for Packets Sending</param>
	public override void Activate(GameLiving living, bool sendUpdates)
	{
		// Set Base level Before Living is set.
		Level = living.Level;
		base.Activate(living, sendUpdates);
	}
	
	public LevelBasedStatChangingAbility(DbAbility dba, int level, EProperty property)
		: base(dba, level, property)
	{
	}
}