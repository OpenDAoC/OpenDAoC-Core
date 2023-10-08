using System.Collections.Generic;
using DOL.Language;

namespace DOL.GS.Effects
{
	/// <summary>
	/// Base for all GuildBannerEffects. Use CreateEffectOfClass to get instances.
	/// </summary>
	public abstract class GuildBannerEffect : TimedEffect
	{
		//Reference:
		//http://support.darkageofcamelot.com/kb/article.php?id=786

		// - Spells on banners will emit >SHORT< duration PBAOE buff spells around the banner's carrier.
		// Pulsing every 9 seconds with a duration of 9 seconds - Tolakram
		protected const int duration = 9000;


		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		public virtual void SendUpdates(GameLiving owner)
		{
			GamePlayer player = owner as GamePlayer;

			if (player != null)
			{
				player.Out.SendCharStatsUpdate();
				player.Out.SendCharResistsUpdate();
				player.Out.SendStatusUpdate();
			}
		}

		/// <summary>
		/// Starts the matching GuildBannerEffect with type (by carrierCharacterClass) and effectiveness
		/// </summary>
		/// <param name="carrier"></param>
		/// <param name="target"></param>
		public static GuildBannerEffect CreateEffectOfClass (GamePlayer carrier, GamePlayer target) {
			
			//calculate effectiveness to target
			double effectiveness = 0;
			if (carrier == target) effectiveness = 1;
			else if (carrier.Guild != null && target.Guild != null && carrier.Guild == target.Guild)
				effectiveness = 1;
			else if (carrier.Group != null && target.Group != null
				&& carrier.Group.IsInTheGroup(target))
				effectiveness = 0.5;

			#region Get new classdependend effect
			switch ((ECharacterClass)carrier.CharacterClass.ID) {
				case ECharacterClass.Wizard: 
				case ECharacterClass.Theurgist:
				case ECharacterClass.Sorcerer:
				case ECharacterClass.Cabalist:
				case ECharacterClass.Spiritmaster:
				case ECharacterClass.Bonedancer:
				case ECharacterClass.Runemaster:
				case ECharacterClass.Warlock:
                case ECharacterClass.Animist:
                case ECharacterClass.Eldritch:
                case ECharacterClass.Enchanter:
                case ECharacterClass.Mentalist:
					return new BannerOfWardingEffect(effectiveness);
				case ECharacterClass.Armsman:
				case ECharacterClass.Mercenary:
				case ECharacterClass.Reaver:
				case ECharacterClass.Paladin:
				case ECharacterClass.Warrior:
				case ECharacterClass.Berserker:
				case ECharacterClass.Savage:
                case ECharacterClass.Hero:
                case ECharacterClass.Champion:
                case ECharacterClass.Vampiir:
					return new BannerOfShieldingEffect(effectiveness);					
				case ECharacterClass.Necromancer:
				case ECharacterClass.Friar:
				case ECharacterClass.Infiltrator:
				case ECharacterClass.Scout:
				case ECharacterClass.Shadowblade:
				case ECharacterClass.Hunter:
				case ECharacterClass.Valkyrie:
				case ECharacterClass.Thane:
				case ECharacterClass.Ranger:
				case ECharacterClass.Nightshade:
				case ECharacterClass.Valewalker:
				case ECharacterClass.Warden:
					return new BannerOfFreedomEffect(effectiveness);					
				case ECharacterClass.Cleric:
				case ECharacterClass.Heretic:
				case ECharacterClass.Minstrel:
				case ECharacterClass.Healer:
				case ECharacterClass.Shaman:
				case ECharacterClass.Skald:
				case ECharacterClass.Druid:
				case ECharacterClass.Bard:
				case ECharacterClass.Bainshee:
					return new BannerOfBesiegingEffect(effectiveness);
				default: return null;
			#endregion
			}

		}

		#region Properties
		double m_effectiveness;
		/// <summary>
		/// Returns the effectiveness of this GuildBannerEffect to be compareable.
		/// </summary>
		public double Effectiveness
		{
			get { return m_effectiveness; }
		}
		#endregion

		#region Delve
		protected abstract string Description { get; }

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>(4);
				list.Add(Description);
				list.AddRange(base.DelveInfo);

				return list;
			}
		}
		#endregion

		#region ctor
		public GuildBannerEffect(double effectiveness) : base(duration)
		{
			m_effectiveness = effectiveness;
		}
		#endregion
	}

	
	/// <summary>
	/// Banner of Warding Effect
	/// </summary>
	public class BannerOfWardingEffect : GuildBannerEffect
	{
		// - Spell Resist Banner - Banner of Warding: 10% bonus to all magic resistances. (Note: This stacks with other effects.)

		#region visual overrides
		public override string Name
		{
			get
			{
				return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.BannerOfWardingEffect.Name");
			}
		}

		protected override string Description
		{
			get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.BannerOfWardingEffect.Description"); }
		}

		//5949,Spell Resist Banner,54,0,0,0,0,0,0,0,0,0,0,0,13,0,332,,,
		public override ushort Icon
		{
			get
			{
				return 54;
			}
		}
		#endregion

		#region effect
		public override void Start(GameLiving m_owner)
		{
			int effValue = (int)(Effectiveness*10);
			base.Start(m_owner);
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Body] += effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Cold] += effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Energy] += effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Heat] += effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Matter] += effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Spirit] += effValue;
			SendUpdates(m_owner);
		}

		public override void Stop()
		{
			int effValue = (int)(Effectiveness * 10);
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Body] -= effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Cold] -= effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Energy] -= effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Heat] -= effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Matter] -= effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Spirit] -= effValue;
			base.Stop();
			SendUpdates(m_owner);
		}
		#endregion

		#region ctor
		public BannerOfWardingEffect(double effectiveness) : base(effectiveness) { }
		#endregion
	}

	/// <summary>
	/// Banner of Shielding
	/// </summary>
	public class BannerOfShieldingEffect : GuildBannerEffect
	{
		//- Melee Resist Banner - Banner of Shielding: 6% bonus to all melee resistances. (Note: This stacks with other effects.)


		#region visual overrides
		public override string Name
		{
			get
			{
				return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.BannerOfShieldingEffect.Name");
			}
		}

		protected override string Description
		{
			get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.BannerOfShieldingEffect.Description"); }
		}

		//5950,Melee Resist Banner,49,0,0,0,0,0,0,0,0,0,0,0,13,0,332,,,
		public override ushort Icon
		{
			get
			{
				return 49;
			}
		}
		#endregion

		#region effect
		public override void Start(GameLiving target)
		{
			base.Start(target);
			int effValue = (int)(Effectiveness * 6);			
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Crush] += effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Slash] += effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Thrust] += effValue;
			SendUpdates(m_owner);
		}

		public override void Stop()
		{
			int effValue = (int)(Effectiveness * 6);
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Crush] -= effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Slash] -= effValue;
			m_owner.BuffBonusCategory4[(int)EProperty.Resist_Thrust] -= effValue;
			base.Stop();
			SendUpdates(m_owner);
		}
		#endregion

		#region ctor
		public BannerOfShieldingEffect(double effectiveness) : base(effectiveness) { }
		#endregion
	}


	/// <summary>
	/// Banner of Freedom
	/// </summary>
	public class BannerOfFreedomEffect : GuildBannerEffect
	{
		//- Crowd Control Duration Banner - Banner of Freedom: -6% reduction to the time effect of all Crowd Control.

		#region visual overrides
		public override string Name
		{
			get
			{
				return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.BannerOfFreedomEffect.Name");
			}
		}

		protected override string Description
		{
			get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.BannerOfFreedomEffect.Description"); }
		}

		//5951,CC Duration Banner,2309,0,0,0,0,0,0,0,0,0,0,0,13,0,332,,,
		public override ushort Icon
		{
			get
			{
				return 2309;
			}
		}
		#endregion

		#region effect
		public override void Start(GameLiving target)
		{
			base.Start(target);
			int effValue = (int)(Effectiveness * 6);
			m_owner.BaseBuffBonusCategory[(int)EProperty.MesmerizeDurationReduction] += effValue;
			m_owner.BaseBuffBonusCategory[(int)EProperty.SpeedDecreaseDurationReduction] += effValue;
			m_owner.BaseBuffBonusCategory[(int)EProperty.StunDurationReduction] += effValue;
			SendUpdates(m_owner);
		}

		public override void Stop()
		{
			int effValue = (int)(Effectiveness * 6);
			m_owner.BaseBuffBonusCategory[(int)EProperty.MesmerizeDurationReduction] -= effValue;
			m_owner.BaseBuffBonusCategory[(int)EProperty.SpeedDecreaseDurationReduction] -= effValue;
			m_owner.BaseBuffBonusCategory[(int)EProperty.StunDurationReduction] -= effValue;			
			base.Stop();
			SendUpdates(m_owner);
		}
		#endregion

		#region ctor
		public BannerOfFreedomEffect(double effectiveness) : base(effectiveness) { }
		#endregion
	}

	/// <summary>
	/// Banner of Freedom
	/// </summary>
	public class BannerOfBesiegingEffect : GuildBannerEffect
	{
		// - Haste - Banner of Besieging: 20% reduction in siege firing speed. (Note that this effect does NOT stack with Warlord.)
		#region visual overrides
		public override string Name
		{
			get
			{
				return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.BannerOfBesiegingEffect.Name");
			}
		}

		protected override string Description
		{
			get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.BannerOfBesiegingEffect.Description"); }
		}

		//5952,Siege Banner,1419,0,0,0,0,0,0,0,0,0,0,0,13,0,332,,,
		public override ushort Icon
		{
			get
			{
				return 1419;
			}
		}
		#endregion

		#region effect
		// done in GameSiegeWeapon.GetActionDelay
		#endregion

		#region ctor
		public BannerOfBesiegingEffect(double effectiveness) : base(effectiveness) { }
		#endregion
	}

}