using System;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
	/// <summary>
	/// Handler for Stealth Spec clicks
	/// </summary>
	[SkillHandlerAttribute(Specs.Stealth)]
	public class StealthSpecHandler : ISpecActionHandler
	{
		/// <summary>
		/// Executes the stealth ability
		/// </summary>
		/// <param name="spec"></param>
		/// <param name="player"></param>
		public void Execute(Specialization spec, GamePlayer player)
		{
			bool isStealthed = player.IsStealthed;

			// Can't stealth while in combat
			if(player.InCombat && !isStealthed && player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Stealth.CannotUseInCombat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
			}
			ECSGameEffect volley = EffectListService.GetEffectOnTarget(player, eEffect.Volley);
			if (volley != null)
			{
				player.Out.SendMessage("You can't stealth while you have active Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			long stealthChangeTick = player.TempProperties.GetProperty<long>(GamePlayer.STEALTH_CHANGE_TICK);
			long changeTime = player.CurrentRegion.Time - stealthChangeTick;
			if(changeTime < 2000)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Stealth.CannotUseStealthChangeTick", ((2000 - changeTime) / 1000).ToString()), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
			}
			player.TempProperties.SetProperty(GamePlayer.STEALTH_CHANGE_TICK, player.CurrentRegion.Time);

			if (!isStealthed)
			{
				// Dead can't stealth
				if(!player.IsAlive)
				{
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Stealth.CannotUseDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
				}

				// Can't stealth if in attack mode
				if(player.attackComponent.AttackState || player.IsCasting)
				{
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Stealth.CannotUseCombatState"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
				}

				//TODO: more checks in this order

				//"You can't hide with a relic!"

				if (player.effectListComponent.GetAllPulseEffects().Count > 0)
				{
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Stealth.CannotUseActivePulsingSpell"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
				}

				//HasVanishRealmAbilityActivated -> Allow stealthing, Stop further checks ...

				if (player.IsMezzed)
				{
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Stealth.CannotUseMezzed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
				}

				if (player.IsStunned)
				{
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Stealth.CannotUseStunned"), eChatType.CT_System, eChatLoc.CL_SystemWindow); 
                    return;
				}

				// Check if enemy player is close
				foreach (GamePlayer ply in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					if (ply.ObjectState != GameObject.eObjectState.Active) continue;

					//Dead players don't prevent stealth!
					if (!ply.IsAlive) continue;

					//TODO: "True Seeing" realm ability check
					//True Seeing denies restealthing for VISIBILITY_DISTANCE!
					//There was/is a huge discussion about this on live servers since
					//all Assessins found this quite unfair!
					//But the discussion has died down since True Seeing etc.

					//Friendly players/mobs don't prevent stealth!
					if (!GameServer.ServerRules.IsAllowedToAttack(ply, player, true)) continue;

					//GM's don't prevent stealth
					if (ply.Client.Account.PrivLevel > 1) continue;

					//Range check
					if (!IsObjectTooClose(ply, player)) continue;

                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Stealth.CannotUseToCloseAnEnemy"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}

				// Check if enemy NPC is close
				foreach (GameNPC npc in player.GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					if (npc.ObjectState != GameObject.eObjectState.Active) continue;

					//Dead npc don't prevent stealth!
					if (!npc.IsAlive) continue;

					//Friendly players/mobs don't prevent stealth!
					if (!GameServer.ServerRules.IsAllowedToAttack(npc, player, true)) continue;

					//Range check
					if (!IsObjectTooClose(npc, player)) continue;

					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Stealth.CannotUseToCloseAnEnemy"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}
			}

			//since 1.88 (?), players which stealth, doesn't be followed by mobs [by Suncheck]
			//TODO: Some further checks need?
			foreach (GameObject attacker in player.attackComponent.Attackers.Keys)
			{
				if (attacker is not GameLiving livingAttacker)
					continue;

				if (livingAttacker.TargetObject == player)
				{
					livingAttacker.TargetObject = null;

					if (attacker is GamePlayer playerAttacker)
						playerAttacker.Out.SendChangeTarget(null);
					if (attacker is GameNPC npcAttacker)
					{
						if (npcAttacker.Brain is IOldAggressiveBrain npcAttackerBrain)
						{
							npcAttackerBrain.RemoveFromAggroList(player);
						}

						npcAttacker.attackComponent.StopAttack();
					}
				}
			}

			player.Stealth(!isStealthed);
		}

		/// <summary>
		/// Checks whether object is too close to player
		/// </summary>
		/// <param name="obj">object</param>
		/// <param name="player">player</param>
		/// <returns>true if object prevents player from hiding</returns>
		private bool IsObjectTooClose(GameObject obj, GamePlayer player)
		{
			float enemyLevel = Math.Max(1f, obj.Level);
			float stealthLevel = player.GetModifiedSpecLevel(Specs.Stealth);
			if(stealthLevel > 50)
				stealthLevel = 50;
			float radius;

			if(obj is GamePlayer && ((GamePlayer)obj).HasAbility(Abilities.DetectHidden))
			{
				//1792.0 = 2048.0 - 256.0 <- Detect Hidden doubles the range
				radius = 2048f - (1792f * stealthLevel / enemyLevel); 
			}
			else
			{
				//1024.0 = 1024.0 - 128.0 <- normal Range
				radius = 1024f - (896f * stealthLevel / enemyLevel); 
			}

			//If we are so skilled we can hide right under the nose of
			//this enemy player, we continue the checks for the next 
			//player without any redundant distance calculations
			if(radius <= 0) return false;

			//Test if the stealthing player is in range of this 
			//player's "impossible to hide" circle
			return obj.IsWithinRadius(player,(int)radius);
		}
	}
}
