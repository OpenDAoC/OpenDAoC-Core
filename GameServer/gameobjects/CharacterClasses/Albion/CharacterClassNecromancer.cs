using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerClass;

namespace DOL.GS
{
	/// <summary>
	/// The necromancer character class.
	/// </summary>
	public class CharacterClassNecromancer : ClassDisciple
	{
		public override void Init(GamePlayer player)
		{
			base.Init(player);

			// Force caster form when creating this player in the world.
			player.Model = (ushort)player.Client.Account.Characters[player.Client.ActiveCharIndex].CreationModel;
			player.Shade(false);
		}

		
		//private String m_petName = "";
		private int m_savedPetHealthPercent = 0;

		/// <summary>
		/// Sets the controlled object for this player
		/// </summary>
		/// <param name="controlledNpc"></param>
		public override void SetControlledBrain(IControlledBrain controlledNpcBrain)
		{
			m_savedPetHealthPercent = (Player.ControlledBrain != null)
				? (int)Player.ControlledBrain.Body.HealthPercent : 0;

			base.SetControlledBrain(controlledNpcBrain);

			if (controlledNpcBrain == null)
			{
				OnPetReleased();
			}
		}

		/// <summary>
		/// Releases controlled object
		/// </summary>
		public override void CommandNpcRelease()
		{
			m_savedPetHealthPercent = (Player.ControlledBrain != null) ? (int)Player.ControlledBrain.Body.HealthPercent : 0;

			base.CommandNpcRelease();
			OnPetReleased();
		}

		/// <summary>
		/// Invoked when pet is released.
		/// </summary>
		public override void OnPetReleased()
		{
			if (Player.IsShade)
				Player.Shade(false);

			Player.InitControlledBrainArray(0);
		}

		/// <summary>
		/// Necromancer can only attack when it's not a shade.
		/// </summary>
		/// <param name="attackTarget"></param>
		public override bool StartAttack(GameObject attackTarget)
		{
			if (!Player.IsShade)
			{
				return true;
			}
			else
			{
				Player.Out.SendMessage("You cannot enter combat while in shade form!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				return false;
			}
		}

		/// <summary>
		/// If the pet is up, show the pet's health in the group window.
		/// </summary>
		public override byte HealthPercentGroupWindow
		{
			get
			{
				if (Player.ControlledBrain == null) 
					return Player.HealthPercent;

				return Player.ControlledBrain.Body.HealthPercent;
			}
		}

		/// <summary>
		/// Create a necromancer shade effect for this player.
		/// </summary>
		/// <returns></returns>
		public override ShadeECSGameEffect CreateShadeEffect()
		{
			return new NecromancerShadeECSGameEffect(new ECSGameEffectInitParams(Player, 0, 1));
		}

		/// <summary>
		/// Changes shade state of the player
		/// </summary>
		/// <param name="state">The new state</param>
		public override void Shade(bool makeShade)
		{
			bool wasShade = Player.IsShade;
			base.Shade(makeShade);

			if (wasShade == makeShade)
				return;

			if (makeShade)
			{
				// Necromancer has become a shade. Have any previous NPC 
				// attackers aggro on pet now, as they can't attack the 
				// necromancer any longer.

				if (Player.ControlledBrain != null && Player.ControlledBrain.Body != null)
				{
					GameNPC pet = Player.ControlledBrain.Body;

					foreach (GameObject attacker in Player.attackComponent.Attackers.Keys)
					{
						if (attacker is GameNPC npcAttacker)
						{
							if (npcAttacker.TargetObject == Player && npcAttacker.attackComponent.AttackState)
							{
								if (npcAttacker.Brain is IOldAggressiveBrain npcAttackerBrain)
								{
									npcAttacker.StopAttack();
									npcAttackerBrain.AddToAggroList(pet, npcAttackerBrain.GetBaseAggroAmount(Player));
								}
							}
						}
					}
				}
			}
			else
			{
				// Necromancer has lost shade form, release the pet if it
				// isn't dead already and update necromancer's current health.

				if (Player.ControlledBrain != null)
					(Player.ControlledBrain as ControlledMobBrain).Stop();

				Player.Health = Math.Min(Player.Health, Player.MaxHealth * Math.Max(10, m_savedPetHealthPercent) / 100);
			}
			Player.Out.SendUpdatePlayer();
		}

		/// <summary>
		/// Called when player is removed from world.
		/// </summary>
		/// <returns></returns>
		public override bool RemoveFromWorld()
		{
			// Force caster form.

			if (Player.IsShade)
				Player.Shade(false);

			return base.RemoveFromWorld();
		}

        /// <summary>
        /// Drop shade first, this in turn will release the pet.
        /// </summary>
        /// <param name="killer"></param>
        public override void Die(GameObject killer)
        {
            Player.Shade(false);

            base.Die(killer);
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            if (Player.ControlledBrain != null)
            {
				GameNPC pet = Player.ControlledBrain.Body;

                if (pet != null && sender == pet && e == GameLivingEvent.CastStarting && args is CastingEventArgs)
                {
       //             ISpellHandler spellHandler = (args as CastingEventArgs).SpellHandler;

       //             if (spellHandler != null)
       //             {
       //                 int powerCost = spellHandler.PowerCost(Player);

       //                 if (powerCost > 0)
							//Player.ChangeMana(Player, eManaChangeType.Spell, -powerCost);
       //             }

                    return;
                }
            }

            base.Notify(e, sender, args);
        }
	}
}
