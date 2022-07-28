using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class OreyEyedOghamist : GameEpicBoss
	{
		public OreyEyedOghamist() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Orey-eyed Oghamist Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164703);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

            RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			OreyEyedOghamistBrain sbrain = new OreyEyedOghamistBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
		public override void WalkToSpawn(short speed)
		{
			if (IsAlive)
				return;
			base.WalkToSpawn(speed);
		}
	}
}
namespace DOL.AI.Brain
{
	public class OreyEyedOghamistBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public OreyEyedOghamistBrain() : base()
		{
			AggroLevel = 0;//he is neutral
			AggroRange = 800;
			ThinkInterval = 1500;
		}
        #region checks
		private bool point1check = false;
		private bool point2check = false;
		private bool point3check = false;
		private bool point4check = false;
		private bool point5check = false;
		private bool point6check = false;
		private bool point7check = false;
		private bool point8check = false;
		private bool point9check = false;
		private bool point10check = false;
		private bool point11check = false;
		private bool point12check = false;
		private bool point13check = false;
        #endregion
        public override void Think()
		{
			if (Body.IsAlive)
			{
				Point3D point1 = new Point3D(504754, 505351, 4939);
				Point3D point2 = new Point3D(506042, 507443, 4945);
				Point3D point3 = new Point3D(505194, 509109, 5052);
				Point3D point4 = new Point3D(505401, 510887, 5382);
				Point3D point5 = new Point3D(503682, 512545, 5423);
				Point3D point6 = new Point3D(502505, 513021, 5204);
				Point3D point7 = new Point3D(501470, 515019, 5120);
				Point3D point8 = new Point3D(500667, 516402, 4848);
				Point3D point9 = new Point3D(498310, 516387, 4923);
				Point3D point10 = new Point3D(495586, 513451, 5510);
				Point3D point11 = new Point3D(495006, 509166, 4991);
				Point3D point12 = new Point3D(498351, 507021, 5059);
				Point3D point13 = new Point3D(500833, 506164, 5074);

				if (!Body.Spells.Contains(OreyBomb))
					Body.Spells.Add(OreyBomb);

				if (!Body.InCombat && !HasAggro)
				{
					short pathSpeed = Body.MaxSpeedBase;
					#region Orey path
					if (!Body.IsWithinRadius(point1, 30) && !point1check)
						Body.WalkTo(point1, pathSpeed);
					else
					{
						point1check = true;
						if (!Body.IsWithinRadius(point2, 30) && point1check && !point2check)
							Body.WalkTo(point2, pathSpeed);
						else
						{
							point2check = true;
							point13check = false;
							if (!Body.IsWithinRadius(point3, 30) && point1check && point2check && !point3check)
								Body.WalkTo(point3, pathSpeed);
							else
							{
								point3check = true;
								if (!Body.IsWithinRadius(point4, 30) && point1check && point2check && point3check && !point4check)
									Body.WalkTo(point4, pathSpeed);
								else
								{
									point4check = true;
									if (!Body.IsWithinRadius(point5, 30) && point1check && point2check && point3check && point4check && !point5check)
										Body.WalkTo(point5, pathSpeed);
									else
									{
										point5check = true;
										if (!Body.IsWithinRadius(point6, 30) && point1check && point2check && point3check && point4check && point5check && !point6check)
											Body.WalkTo(point6, pathSpeed);
										else
										{
											point6check = true;
											if (!Body.IsWithinRadius(point7, 30) && point1check && point2check && point3check && point4check && point5check && point6check && !point7check)
												Body.WalkTo(point7, pathSpeed);
											else
											{
												point7check = true;
												if (!Body.IsWithinRadius(point8, 30) && point1check && point2check && point3check && point4check && point5check && point6check && point7check && !point8check)
													Body.WalkTo(point8, pathSpeed);
												else
												{
													point8check = true;
													if (!Body.IsWithinRadius(point9, 30) && point1check && point2check && point3check && point4check && point5check && point6check && point7check && point8check
														&& !point9check)
														Body.WalkTo(point9, pathSpeed);
													else
													{
														point9check = true;
														if (!Body.IsWithinRadius(point10, 30) && point1check && point2check && point3check && point4check && point5check && point6check && point7check && point8check
															&& point9check && !point10check)
															Body.WalkTo(point10, pathSpeed);
														else
														{
															point10check = true;
															if (!Body.IsWithinRadius(point11, 30) && point1check && point2check && point3check && point4check && point5check && point6check && point7check && point8check
																&& point9check && point10check && !point11check)
																Body.WalkTo(point11, pathSpeed);
															else
															{
																point11check = true;
																if (!Body.IsWithinRadius(point12, 30) && point1check && point2check && point3check && point4check && point5check && point6check && point7check && point8check
																&& point9check && point10check && point11check && !point12check)
																	Body.WalkTo(point12, pathSpeed);
																else
																{
																	point12check = true;
																	if (!Body.IsWithinRadius(point13, 30) && point1check && point2check && point3check && point4check && point5check && point6check && point7check && point8check
																		&& point9check && point10check && point11check && point12check && !point13check)
																		Body.WalkTo(point13, pathSpeed);
																	else
																	{
																		point13check = true;
																		point1check = false;
																		point2check = false;
																		point3check = false;
																		point4check = false;
																		point5check = false;
																		point6check = false;
																		point7check = false;
																		point8check = false;
																		point9check = false;
																		point10check = false;
																		point11check = false;
																		point12check = false;
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
					#endregion
				}
			}
			if(Body.TargetObject != null && HasAggro)
            {
				GameLiving target = Body.TargetObject as GameLiving;
				if (!Body.IsWithinRadius(Body.TargetObject, 300))
				{
					if (!Body.IsCasting && Util.Chance(100))
						Body.CastSpell(OreyDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				}
				else
				{
					if (target != null && target.IsAlive)
					{
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.StrConDebuff) && !Body.IsCasting && Util.Chance(25))
							Body.CastSpell(Orey_SC_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					}
				}
            }
            base.Think();
		}
		#region Spells
		private Spell m_OreyBomb;
		private Spell OreyBomb
		{
			get
			{
				if (m_OreyBomb == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 5;
					spell.Power = 0;
					spell.RecastDelay = Util.Random(20, 30);
					spell.ClientEffect = 4369;
					spell.Icon = 4369;
					spell.Damage = 800;
					spell.DamageType = (int)eDamageType.Energy;
					spell.Name = "Energy Blast";
					spell.Range = 0;
					spell.Radius = 1000;
					spell.SpellID = 12012;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_OreyBomb = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OreyBomb);
				}
				return m_OreyBomb;
			}
		}
		private Spell m_Orey_SC_Debuff;
		private Spell Orey_SC_Debuff
		{
			get
			{
				if (m_Orey_SC_Debuff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 30;
					spell.Duration = 60;
					spell.ClientEffect = 5408;
					spell.Icon = 5408;
					spell.Name = "Greater Infirmity";
					spell.TooltipId = 5408;
					spell.Range = 1000;
					spell.Value = 73;
					spell.Radius = 400;
					spell.SpellID = 12013;
					spell.Target = "Enemy";
					spell.Type = eSpellType.StrengthConstitutionDebuff.ToString();
					spell.DamageType = (int)eDamageType.Body;
					m_Orey_SC_Debuff = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Orey_SC_Debuff);
				}
				return m_Orey_SC_Debuff;
			}
		}
		private Spell m_OreyDD;
		private Spell OreyDD
		{
			get
			{
				if (m_OreyDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 3;
					spell.ClientEffect = 0;
					spell.Icon = 0;
					spell.Damage = 500;
					spell.DamageType = (int)eDamageType.Slash;
					spell.Name = "Ranged Melee Swing";
					spell.Range = 2200;
					spell.SpellID = 12014;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_OreyDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OreyDD);
				}
				return m_OreyDD;
			}
		}
		#endregion
	}
}

