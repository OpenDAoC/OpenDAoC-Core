using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Keeps;
using Core.GS.Packets.Server;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS;

/// <summary>
/// Class to deal with spell casting for the guards
/// </summary>
public class GuardSpellMgr
{
	/// <summary>
	/// Method to check the area for heals
	/// </summary>
	/// <param name="guard">The guard object</param>
	public static void CheckAreaForHeals(GameKeepGuard guard)
	{
		GameLiving target = null;
		foreach (GamePlayer player in guard.GetPlayersInRadius(2000))
		{
			if(!player.IsAlive) continue;
			if (GameServer.ServerRules.IsSameRealm(player, guard, true))
			{
				if (player.HealthPercent < 60)
				{
					target = player;
					break;
				}
			}
		}

		if (target == null)
		{
			foreach (GameNpc npc in guard.GetNPCsInRadius(2000))
			{
				if (npc is GameSiegeWeapon) continue;
				if (GameServer.ServerRules.IsSameRealm(npc, guard, true))
				{
					if (npc.HealthPercent < 60)
					{
						target = npc;
						break;
					}
				}
			}
		}

		if (target != null)
		{
			GamePlayer losChecker = null;

			if (target is GamePlayer playerTarget)
				losChecker = playerTarget;
			else if (target is GameNpc npcTarget)
			{
				if (npcTarget.Brain is IControlledBrain npcTargetBrain)
					losChecker = npcTargetBrain.GetPlayerOwner();
				else
				{
					foreach (GamePlayer playerInRadius in guard.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						if (playerInRadius?.ObjectState == GameObject.eObjectState.Active)
							losChecker = playerInRadius;

						break;
					}
				}
			}

			if (losChecker == null)
				return;

			if (!target.IsAlive)
				return;

			guard.TargetObject = target;

			losChecker.Out.SendCheckLOS(guard, target, new CheckLOSResponse(guard.GuardStartSpellHealCheckLOS));
		}
	}

	/// <summary>
	/// Method for a lord to cast a heal spell
	/// </summary>
	/// <param name="lord">The lord object</param>
	public static void LordCastHealSpell(GameKeepGuard lord)
	{
		//decide which healing spell
		Spell spell = GetLordHealSpell((ERealm)lord.Realm);
		//cast the healing spell
		if (spell != null && !lord.IsStunned && !lord.IsMezzed)
		{
			lord.StopAttack();
			lord.TargetObject = lord;
			lord.CastSpell(spell, GuardSpellMgr.GuardSpellLine);
		}
	}

	/// <summary>
	/// Method to cast a heal spell
	/// </summary>
	/// <param name="guard">The guard object</param>
	/// <param name="target">The spell target</param>
	public static void CastHealSpell(GameNpc guard, GameLiving target)
	{
		//decide which healing spell
		Spell spell = GetGuardHealSmallSpell((ERealm)guard.Realm);
		//cast the healing spell
		if (spell != null && !guard.IsStunned && !guard.IsMezzed  )
		{
			guard.StopAttack();
			guard.TargetObject = target;
			guard.CastSpell(spell, GuardSpellMgr.GuardSpellLine);
		}
	}

	public static Spell GetLordHealSpell(ERealm realm)
	{
		switch (realm)
		{
			case ERealm.None:
			case ERealm.Albion:
					return AlbLordHealSpell;
			case ERealm.Midgard:
					return MidLordHealSpell;
			case ERealm.Hibernia:
					return HibLordHealSpell;
		}
		return null;
	}

	public static Spell GetGuardHealSmallSpell(ERealm realm)
	{
		switch (realm)
		{ 
			case ERealm.None:
			case ERealm.Albion:
				return AlbGuardHealSmallSpell;
			case ERealm.Midgard:
				return MidGuardHealSmallSpell;
			case ERealm.Hibernia:
				return HibGuardHealSmallSpell;
		}
		return null;
	}

	#region Spells and Spell Line

	private static SpellLine m_GuardSpellLine;
	/// <summary>
	/// Spell line used by guards
	/// </summary>
	public static SpellLine GuardSpellLine
	{
		get
		{
			if (m_GuardSpellLine == null)
                m_GuardSpellLine = new SpellLine("GuardSpellLine", "Guard Spells", "unknown", false);

			return m_GuardSpellLine;
		}
	}


	private static Spell m_albLordHealSpell;
	private static Spell m_midLordHealSpell;
	private static Spell m_hibLordHealSpell;

	/// <summary>
	/// The spell the Albion Lord uses to heal itself
	/// </summary>
	public static Spell AlbLordHealSpell
	{
		get
		{
			if (m_albLordHealSpell == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 2;
				spell.ClientEffect = 1340;
				spell.Value = 225; //350;
				spell.Name = "Guard Heal";
                spell.Range = 2000;
				spell.SpellID = 90001;
				spell.Target = "Realm";
				spell.Type = "Heal";
				spell.Uninterruptible = true;
				m_albLordHealSpell = new Spell(spell, 50);
			}
			return m_albLordHealSpell;
		}
	}

	/// <summary>
	/// The spell the Midgard Lord uses to heal itself
	/// </summary>
	public static Spell MidLordHealSpell
	{
		get
		{
			if (m_midLordHealSpell == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 2;
				spell.ClientEffect = 3011;
				spell.Value = 225;//350;
				spell.Name = "Guard Heal";
                spell.Range = 2000;
				spell.SpellID = 90002;
				spell.Target = "Realm";
				spell.Type = "Heal";
				spell.Uninterruptible = true;
				m_midLordHealSpell = new Spell(spell, 50);
			}
			return m_midLordHealSpell;
		}
	}

	/// <summary>
	/// The spell the Hibernia Lord uses to heal itself
	/// </summary>
	public static Spell HibLordHealSpell
	{
		get
		{
			if (m_hibLordHealSpell == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 2;
				spell.ClientEffect = 3030;
				spell.Value = 225;//350;
				spell.Name = "Guard Heal";
                spell.Range = 2000;
				spell.SpellID = 90003;
				spell.Target = "Realm";
				spell.Type = "Heal";
				spell.Uninterruptible = true;
				m_hibLordHealSpell = new Spell(spell, 50);
			}
			return m_hibLordHealSpell;
		}
	}

	private static Spell m_albGuardHealSmallSpell;
	private static Spell m_midGuardHealSmallSpell;
	private static Spell m_hibGuardHealSmallSpell;

	/// <summary>
	/// The spell that Albion Guards use to heal small amounts
	/// </summary>
	public static Spell AlbGuardHealSmallSpell
	{
		get
		{
			if (m_albGuardHealSmallSpell == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.ClientEffect = 1340;
				spell.Value = 100;
				spell.Name = "Guard Heal";
				spell.Range = 2000;
				spell.SpellID = 90004;
				spell.Target = "Realm";
				spell.Type = "Heal";
				m_albGuardHealSmallSpell = new Spell(spell, 50);
			}
			return m_albGuardHealSmallSpell;
		}
	}

	/// <summary>
	/// The spell that Midgard Guards use to heal small amounts
	/// </summary>
	public static Spell MidGuardHealSmallSpell
	{
		get
		{
			if (m_midGuardHealSmallSpell == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.ClientEffect = 3011;
				spell.Value = 100;
				spell.Name = "Guard Heal";
                spell.Range = 2000;
				spell.SpellID = 90005;
				spell.Target = "Realm";
				spell.Type = "Heal";
				m_midGuardHealSmallSpell = new Spell(spell, 50);
			}
			return m_midGuardHealSmallSpell;
		}
	}

	/// <summary>
	/// The spell that Hibernian Guards use to heal small amounts
	/// </summary>
	public static Spell HibGuardHealSmallSpell
	{
		get
		{
			if (m_hibGuardHealSmallSpell == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.ClientEffect = 3030;
				spell.Value = 100;
				spell.Name = "Guard Heal";
                spell.Range = 2000;
				spell.SpellID = 90006;
				spell.Target = "Realm";
				spell.Type = "Heal";
				m_hibGuardHealSmallSpell = new Spell(spell, 50);
			}
			return m_hibGuardHealSmallSpell;
		}
	}

    private static Spell m_albGuardBoltSpellPortalKeep;
    private static Spell m_midGuardBoltSpellPortalKeep;
    private static Spell m_hibGuardBoltSpellPortalKeep;

    /// <summary>
    /// The spell that Albion Portal Keeps Guards use for Bolt
    /// </summary>
    public static Spell AlbGuardBoltSpellPortalKeep
    {
        get
        {
            if (m_albGuardBoltSpellPortalKeep == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 2;
                spell.DamageType = (int)EDamageType.Heat;
                spell.ClientEffect = 378;
                spell.Value = 850;
                spell.Damage = 850;
                spell.Name = "Bolt";
                spell.Range = 2000;
                spell.SpellID = 90017;
                spell.Target = "Enemy";
                spell.Type = "Bolt";
                spell.AllowBolt = true;
                m_albGuardBoltSpellPortalKeep = new Spell(spell, 50);
            }
            return m_albGuardBoltSpellPortalKeep;
        }
    }

    /// <summary>
    /// The spell that Midgard Portal Keeps Guards use for Bolt
    /// </summary>
    public static Spell MidGuardBoltSpellPortalKeep
    {
        get
        {
            if (m_midGuardBoltSpellPortalKeep == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 2;
                spell.DamageType = (int)EDamageType.Matter;
                spell.ClientEffect = 2901;
                spell.Value = 850;
                spell.Damage = 850;
                spell.Name = "Bolt";
                spell.Range = 2000;
                spell.SpellID = 90018;
                spell.Target = "Enemy";
                spell.Type = "Bolt";
                spell.AllowBolt = true;
                m_midGuardBoltSpellPortalKeep = new Spell(spell, 50);
            }
            return m_midGuardBoltSpellPortalKeep;
        }
    }

    /// <summary>
    /// The spell that Hibernian Portal Keeps Guards use for Bolt
    /// </summary>
    public static Spell HibGuardBoltSpellPortalKeep
    {
        get
        {
            if (m_hibGuardBoltSpellPortalKeep == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 2;
                spell.DamageType = (int)EDamageType.Cold;
                spell.ClientEffect = 4559;
                spell.Value = 850;
                spell.Damage = 850;
                spell.Name = "Bolt";
                spell.Range = 2000;
                spell.SpellID = 90019;
                spell.Target = "Enemy";
                spell.Type = "Bolt";
                spell.AllowBolt = true;
                m_hibGuardBoltSpellPortalKeep = new Spell(spell, 50);
            }
            return m_hibGuardBoltSpellPortalKeep;
        }
    }

    private static Spell m_albGuardNukeSpell;
    private static Spell m_midGuardNukeSpell;
    private static Spell m_hibGuardNukeSpell;

    /// <summary>
    /// The spell that Albion Guards use for dd
    /// </summary>
    public static Spell AlbGuardNukeSpell
    {
        get
        {
            if (m_albGuardNukeSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.DamageType = 13;
                spell.ClientEffect = 368;
                spell.Value = 158;
                spell.Damage = 158;
                spell.Name = "Nuke";
                spell.Range = 1500;
                spell.SpellID = 90014;
                spell.Target = "Enemy";
                spell.Type = "DirectDamage";
                m_albGuardNukeSpell = new Spell(spell, 50);
            }
            return m_albGuardNukeSpell;
        }
    }

    /// <summary>
    /// The spell that Midgard Guards use for dd
    /// </summary>
    public static Spell MidGuardNukeSpell
    {
        get
        {
            if (m_midGuardNukeSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.DamageType = 12;
                spell.ClientEffect = 2919;
                spell.Value = 158;
                spell.Damage = 158;
                spell.Name = "Nuke";
                spell.Range = 1500;
                spell.SpellID = 90015;
                spell.Target = "Enemy";
                spell.Type = "DirectDamage";
                m_midGuardNukeSpell = new Spell(spell, 50);
            }
            return m_midGuardNukeSpell;
        }
    }

    /// <summary>
    /// The spell that Hibernian Guards use for dd
    /// </summary>
    public static Spell HibGuardNukeSpell
    {
        get
        {
            if (m_hibGuardNukeSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.DamageType = 11;
                spell.ClientEffect = 4510;
                spell.Value = 158;
                spell.Damage = 158;
                spell.Name = "Nuke";
                spell.Range = 1500;
                spell.SpellID = 90016;
                spell.Target = "Enemy";
                spell.Type = "DirectDamage";
                m_hibGuardNukeSpell = new Spell(spell, 50);
            }
            return m_hibGuardNukeSpell;
        }
    }

	#endregion
}