/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Language;
using System.Linq;
using DOL.GS.Keeps;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Charms target NPC for the spell duration
    /// 
    /// Caster.GetModifiedSpecLevel * 1.1 is used for hard NPC level cap
    /// </summary>
    [SpellHandlerAttribute("Charm")]
    public class CharmSpellHandler : SpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Holds the charmed GameNPC for pulsing spells
        /// </summary>
        public GameNPC m_charmedNpc;

        /// <summary>
        /// Holds the new controlled NPC's brain
        /// </summary>
        public ControlledNpcBrain m_controlledBrain;

        /// <summary>
        /// Tells pulsing spells to not add a controlled brain if it has not been previously removed by OnStopEffect()
        /// </summary>
        public bool m_isBrainSet;
        
        /// <summary>
        /// Specifies the type of mobs this spell can charm, based on Spell.AmnesiaChance values
        /// </summary>        
        public enum eCharmType : ushort
        {
	        All = 0,
	        Humanoid = 1,
	        Animal = 2,
	        Insect = 3,
	        HumanoidAnimal = 4,
	        HumanoidAnimalInsect = 5,
	        HumanoidAnimalInsectMagical = 6,
	        HumanoidAnimalInsectMagicalUndead = 7,
	        Reptile = 8
        }

        public override void CreateECSEffect(ECSGameEffectInitParams initParams)
        {
	        new CharmECSGameEffect(initParams);
        }

        /// <summary>
        /// Specifies actions to perform after CheckEndCast() and before ApplyEffectOnTarget()
        /// </summary>
        public override void FinishSpellCast(GameLiving target)
        {
	        Caster.Mana -= PowerCost(target);
	        base.FinishSpellCast(target);
        }
        
        /// <summary>
        /// Sets the seed for random results
        /// </summary>
        private static Random randomizer = new Random();
        
        /// <summary>
        /// Sets a call for random results to encourage more varied resist chances
        /// </summary>
        /// <returns>A value between 0 and 1</returns>
        public static double Roll()
        {
	        var roll1 = randomizer.NextDouble() * 100;
	        var roll1String = String.Format("{0:0.##}", roll1);
	        var roll2 = randomizer.NextDouble() * 100;
	        var roll2String = String.Format("{0:0.##}", roll2);
	        
	        // log.Warn("Roll1=" + roll1String + "; Roll2=" + roll2String);
	        return Math.Max(roll1, roll2);
        }

        /// <summary>
        /// Specifies whether the effect is resisted or applied to the target after OnSpellPulse() or FinishSpellCast()
        /// </summary>
        /// <param name="target">The target the charm is being applied to</param>
        /// <returns>'true' if the effect should be applied to the target</returns>
        public override bool StartSpell(GameLiving target)
        {
	        if (m_charmedNpc == null)
		        // Save target on first tick
		        m_charmedNpc = target as GameNPC;
	        else
		        // Reuse target for pulsing spells
		        target = m_charmedNpc;

	        if (target == null) 
		        return false;

	        if (Caster == null)
		        return false;

	        // Ignore SpellResisted values by returning 0
            if (Util.Chance(CalculateSpellResistChance(target)))
            {
                OnSpellResisted(target);
            }
            // If resist chance > 0, apply effect
            else
            {
                ApplyEffectOnTarget(target, 1);
            }
            return true;
        }

        /// <summary>
        /// Calculates chance of spell getting resisted
        /// </summary>
        /// <param name="target">The target mob for the spell</param>
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        /// <summary>
        ///  Performs checks after casting finishes and before power is deducted
        /// </summary>
        /// <param name="selectedTarget"></param>
        /// <returns></returns>
        public override bool CheckEndCast(GameLiving selectedTarget)
        {
        	var casterPlayer = Caster as GamePlayer;
	        var charmMob = selectedTarget as GameNPC;
	        
	        if (Caster == null || casterPlayer == null)
		        return false;
	        
	        // If there's no target, then don't cast
	        if (charmMob == null)
	        {
		        // Message: You must select a target for this spell!
		        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "GamePlayer.Target.Spell.SelectATarget"), eChatType.CT_SpellResisted);
		        return false;
	        }
	        
	        if (!base.CheckEndCast(charmMob)) 
		        return false;

	        if (Caster is GamePlayer && casterPlayer.ControlledBrain != null && casterPlayer.ControlledBrain != charmMob.Brain)
	        {
		        // Message: You already have a charmed creature, release it first!
		        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.AlreadyOwnCharmed"), eChatType.CT_SpellResisted);
		        return false;
	        }

	        // Apply following logic only if the target is a mob/NPC
	        // If the mob is not controlled and the caster
	        if (m_controlledBrain == null && Caster.ControlledBrain == null)
	        {
		        // Target is already controlled
		        if(charmMob.Brain != null && charmMob.Brain is IControlledBrain && (((IControlledBrain)(charmMob).Brain).Owner as GamePlayer) != Caster)
		        {
			        // Message: {0} is currently being controlled.
			        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.CurrentlyControlled", charmMob.GetName(0, true)), eChatType.CT_SpellResisted);
			        return false;
		        }

		        // If Caster already has a pet
		        if (Caster.ControlledBrain != null)
		        {
			        // Message: You already have a charmed creature, release it first!
			        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.AlreadyOwnCharmed"), eChatType.CT_SpellResisted);
			        return false;
		        }
	                
		        // Make sure the target is alive
		        if (!charmMob.IsAlive)
		        {
			        // Message: {0} is dead!
			        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "GamePlayer.Target.Fail.IsDead", charmMob.GetName(0, true)), eChatType.CT_SpellResisted);
			        return false;
		        }
		        
		        // Make sure the pet is in the same zone
		        if (charmMob.CurrentRegion != Caster.CurrentRegion)
		        {
			        return false;
		        }
	                
		        // If the mob is "friendly"
		        if (charmMob.Realm != 0)
		        {
			        // Message: {0) can't be charmed!
			        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.CantBeCharmed", charmMob.GetName(0, true)), eChatType.CT_SpellResisted);
			        return false;
		        }
	                
		        // To make a mob uncharmable, give it a BodyType of 0
		        if (charmMob.BodyType is < 1 or > 11)
		        {
			        // Message: {0) can't be charmed!
			        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.CantBeCharmed", charmMob.GetName(0, true)), eChatType.CT_SpellResisted);
			        return false;
		        }
	                
		        // If the mob has a valid BodyType set and is a GameNPC, prevent charms for the following classess as they could create many opportunities for abuse
		        if (charmMob.BodyType is > 0 and < 12)
		        {
			        var isCharmable = true;

			        if (charmMob is GamePet) // Summoned pets
				        isCharmable = false;
			        if (charmMob is GameMerchant) // Merchant NPCs
				        isCharmable = false;
			        if (charmMob is GameStableMaster) // Horse route NPCs
				        isCharmable = false;
			        if (charmMob is AlchemistsMaster or FletchingMaster or ArmorCraftingMaster or WeaponCraftingMaster or SpellCraftingMaster or TailoringMaster or BasicCraftingMaster or SiegecraftingMaster) // Any Craft Master NPCs
				        isCharmable = false;
			        if (charmMob is GameTrainer) // Class trainers
				        isCharmable = false;
			        if (charmMob is GameGuard) // Realm guards
				        isCharmable = false;
			        if (charmMob is GameKeepGuard) // Keep guards
				        isCharmable = false;
			        if (charmMob is GameTeleporter) // Classic/SI teleporters
				        isCharmable = false;
			        if (charmMob is GameHastener) // Hasteners like Skald of Midgard
				        isCharmable = false;
			        if (charmMob is AccountVaultKeeper) // Account vaultkeeper
				        isCharmable = false;
			        if (charmMob is GameVaultKeeper) // Character vaultkeeper
				        isCharmable = false;
			        if (charmMob is GameHealer) // Healer NPCs
				        isCharmable = false;
			        if (charmMob is Enchanter) // Item enchanter NPCs
				        isCharmable = false;
			        if (charmMob is Recharger) // Item recharge NPCs
				        isCharmable = false;
			        if (charmMob is GameDragon) // Any endgame Dragon (e.g., Gjal)
				        isCharmable = false;
			        if (charmMob is GameEpicBoss or GameEpicNPC) // Any epic mobs or bosses
				        isCharmable = false;

			        // If the mob's ClassType matches any of the above, it cannot be charmed
			        if (isCharmable == false)
			        {
				        // Message: {0) can't be charmed!
				        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.CantBeCharmed", charmMob.GetName(0, true)), eChatType.CT_SpellResisted);
				        return false;
			        }
		        }
                    
		        // If the target has an uppercase first letter in the name
		        if (ServerProperties.Properties.SPELL_CHARM_NAMED_CHECK != 0 && char.IsUpper(charmMob.Name[0]))
		        {
			        // Message: {0) can't be charmed!
			        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.CantBeCharmed", charmMob.GetName(0, true)), eChatType.CT_SpellResisted);
			        return false;
		        }

		        // If the mob's BodyType matches the list below (based on 'Spell.AmnesiaChance'), then the mob may be charmed.
		        // For example, if the spell has an AmnesiaChance value of 4 (HumanoidAnimal), then the Caster may charm any mob with a BodyType of Humanoid or Animal.
		        if (m_spell.AmnesiaChance is > (ushort)eCharmType.All and <= (ushort)eCharmType.Reptile)
		        {
		                
			        bool isCharmable = false;
		                
			        // Returns 'true' only for charmable mobs
			        switch((eCharmType)m_spell.AmnesiaChance) {
			                
				        case eCharmType.HumanoidAnimalInsectMagicalUndead: 
					        if (charmMob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Undead)
						        isCharmable = true;
					        goto case eCharmType.HumanoidAnimalInsectMagical;
				                
				        // Plant and Elemental body types are considered to be inherently Magical
				        case eCharmType.HumanoidAnimalInsectMagical: 
					        if (charmMob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Magical)
						        isCharmable = true; 
					        if (charmMob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Plant)
						        isCharmable = true;
					        if (charmMob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Elemental)
						        isCharmable = true;
					        goto case eCharmType.HumanoidAnimalInsect;
				                
				        case eCharmType.HumanoidAnimalInsect:
					        if (charmMob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Insect)
						        isCharmable = true;
					        if (charmMob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Reptile)
						        isCharmable = true;
					        goto case eCharmType.HumanoidAnimal;

				        case eCharmType.HumanoidAnimal: 
					        if (charmMob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Animal)
						        isCharmable = true;
					        goto case eCharmType.Humanoid;

				        case eCharmType.Humanoid:
					        if (charmMob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Humanoid)
						        isCharmable = true;
					        break;

				        case eCharmType.Animal: 
					        if (charmMob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Animal)
						        isCharmable = true;
					        break;

				        case eCharmType.Insect:
					        if (charmMob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Insect)
						        isCharmable = true;
					        break;
			                
				        // Only available for spells with AmnesiaChance of 'eCharmType.HumanoidAnimalInsect' or higher
				        case eCharmType.Reptile:
					        if (charmMob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Reptile)
						        isCharmable = true;
					        break;
			        }

			        // If the NPC type doesn't match spell charm types specified above
			        if (!isCharmable)
			        {
				        // Message: This spell does not charm that type of monster!
				        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.WrongType"), eChatType.CT_SpellResisted);
				        return false;
			        }
		        }
	                
		        // Mentalist- and Minstrel-specific requirements
		        // Determine whether mob level is too high based on:
		        // 1) Spell.Value
		        // If this check is passed, then try again with:
		        // 2) Caster's modified spec level * 1.1
		        // The second point is based upon the spell limitation where mob level cannot exceed 110% of the Caster's modified spec level.
		        // For example, with a modified spec of 65, the Caster cannot charm a mob above level 71 AT ALL (no 99% resist, just outright return of 'false').
		        if (casterPlayer.CharacterClass.ID is (int)eCharacterClass.Minstrel or (int)eCharacterClass.Mentalist)
		        {
			        // If the target mob's level surpasses Spell.Value
			        if (charmMob.Level > Spell.Value)
			        {
				        // Message: {0} is too strong for you to charm!
				        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.TooStrong", charmMob.GetName(0, true)), eChatType.CT_SpellResisted);
				        return false;
			        }
			        // If the target mob's level surpasses 110% of the Caster's modified skill
			        if (charmMob.Level > casterPlayer.GetModifiedSpecLevel(m_spellLine.Spec) * 1.1)
			        {
				        // Message: {0} is too strong for you to charm!
				        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.TooStrong", charmMob.GetName(0, true)), eChatType.CT_SpellResisted);
				        return false;
			        }
		        }
	                
		        // Hunter- and Sorcerer-specific limitations
		        // Determine whether mob level is too high based on:
		        // 1) Spell.Value
		        // If this check is passed, then try again with:
		        // 2) Caster's Level
		        // The main limitation on Sorcerer charms is that the mob level cannot exceed the Spell.Value or the Caster's level, unlike Minstrel/Mentalist charms.
		        // For example, with a Caster level of 50, the Caster cannot charm a mob above level 50 AT ALL (no 99% resist, just outright return of 'false').
		        // For Spell.Value, each charm spell has a maximum level value for Sorc's below level 50, which means lower-level charms cannot be used to save on power or charm same-level mobs of different body types. The highest-level spell should always be used.
		        if (casterPlayer.CharacterClass.ID is (int)eCharacterClass.Hunter or (int)eCharacterClass.Sorcerer)
		        {
			        // Check first if the target mob's level surpasses the Caster's level
			        // Mob level cannot exceed the Caster's level
			        if (charmMob.Level > Caster.Level)
			        {
				        // Message: {0} is too strong for you to charm!
				        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.TooStrong", charmMob.GetName(0, true)), eChatType.CT_SpellResisted);
				        return false;
			        }
		                
			        // Then check if the target mob's level surpasses Spell.Value
			        // Mob level cannot exceed the Caster's "sweet spot"
			        // The sweet spot formula is ((Caster.Level * 0.66) + (ModifiedSpec * 0.33)
			        if (charmMob.Level > Spell.Value)
			        {
				        // Message: {0} is too strong for you to charm!
				        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "CharmSpell.EndCast.Fail.TooStrong", charmMob.GetName(0, true)), eChatType.CT_SpellResisted);
				        return false;
			        }
		                
			        // If the mob is in combat, it cannot be charmed
			        if (charmMob.InCombat)
			        {
				        // Message: You can't charm {0} while {1} is in combat!
				        MessageToCaster(
					        LanguageMgr.GetTranslation(casterPlayer.Client, "SpellEffect.Charm.Err.InCombat", selectedTarget.GetName(0, false), selectedTarget.GetPronoun(1, false)), eChatType.CT_SpellResisted);
				        return false;
			        }
		        }
	        }


	        return true;
        }

        /// <summary>
        /// Apply effect on target or do spell action if non duration spell
        /// </summary>
        /// <param name="target">target that gets the effect</param>
        /// <param name="effectiveness">factor from 0..1 (0%-100%)</param>
        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            //You should be able to chain pulsing charm on the same mob
            if (Spell.Pulse != 0 && Caster is GamePlayer && (((GamePlayer)Caster).ControlledBrain != null && ((GamePlayer)Caster).ControlledBrain.Body != (GameNPC)target))
            {
                ((GamePlayer)Caster).CommandNpcRelease();
            }

            if (Caster is GamePlayer charmCaster)
            {
            	// base resists for all charm spells
                double resistChance = (short) (100 - (85 + ((Caster.Level - target.Level) / 2)));

                if (this.Spell.Pulse != 0) // not permanent
                {
	                /*
	                 * The Minstrel/Mentalist has an almost certain chance to charm/retain control of
	                 * a creature his level or lower, although there is a small random chance that it
	                 * could fail. The higher the level of the charmed creature compared to the
	                 * Minstrel/Mentalist, the greater the chance the monster has of breaking the charm.
	                 * Please note that your specialization level in the magic skill that contains the
	                 * charm spell will modify your base chance of charming and retaining control.
	                 * The higher your spec level, the greater your chance of controlling.
	                 */
                    
                    // double diffLevel = (Caster.Level / 1.5 + Caster.GetModifiedSpecLevel(m_spellLine.Spec) / 3) - target.Level;
                    double sweetSpot = (Caster.Level * 0.66) + (Caster.GetModifiedSpecLevel(m_spellLine.Spec) * 0.33) - target.Level;
                    
                    if (sweetSpot >= 0)
                    {
	                    resistChance = (10 - sweetSpot * 3);
                        resistChance = Math.Max(resistChance, 1);
                    }
                    else
                    {
	                    resistChance = ((10 + (sweetSpot * -1.5)) * 3);
                        resistChance = Math.Min(resistChance, 99);
                    }
                }

                double spellResistChance = resistChance;
                var resistResult = Roll();
                var resistString = String.Format("{0:0.##}", spellResistChance);
                var rollString = String.Format("{0:0.##}", resistResult);

                // Make sure the pet is in the same zone
                if (target.CurrentRegion != Caster.CurrentRegion)
                {
	                ECSPulseEffect song = EffectListService.GetPulseEffectOnTarget(Caster as GamePlayer);
	                if (song != null && song.SpellHandler.Spell.InstrumentRequirement == 0 && song.SpellHandler.Spell.CastTime == 0)
	                {
		                EffectService.RequestImmediateCancelConcEffect(song);
	                }
	                return;
                }
                if (target.IsWithinRadius(charmCaster, 2000))
                {
	                if (charmCaster.Client.Account.PrivLevel > 1)
						MessageToCaster("Resist Chance=" + resistString + "; Roll=" + rollString, eChatType.CT_SpellResisted);
	                
	                if (resistResult <= spellResistChance)
	                {
		                // Message: {0} resists the charm! ({1}%)
		                MessageToCaster(LanguageMgr.GetTranslation(charmCaster.Client, "GamePlayer.StartCharm.Fail.Resist", target.GetName(0, true), resistString), eChatType.CT_SpellResisted);
		                SendEffectAnimation(GetTarget(), 0, false, 0);
		                return;
	                }
		            
	                SendEffectAnimation(GetTarget(), 0, false, 1);
                }
                if (!target.IsWithinRadius(Caster, 2000))
                {
	                // Message: Your controlled creature is too far away!
	                MessageToCaster(LanguageMgr.GetTranslation(charmCaster.Client, "GamePlayer.GamePet.Movement.TooFarAway"), eChatType.CT_SpellResisted);
	                return;
                }
            }

            base.ApplyEffectOnTarget(target, effectiveness);
        }

        /// <summary>
        /// When an applied effect starts
        /// duration spells only
        /// </summary>
        /// <param name="effect"></param>
        public override void OnEffectStart(GameSpellEffect effect)
        {
            //    base.OnEffectStart(effect);

            // Behaviors moved to CharmECSEffect.cs > OnStopEffect()
        }

        /// <summary>
        /// Handles release commands
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        public void ReleaseEventHandler(DOLEvent e, object sender, EventArgs arguments)
        {
            Console.Write("Charm ReleaseEventHandler Called!");
            IControlledBrain npc = null;
            
            if (e == GameLivingEvent.PetReleased)
                npc = ((GameNPC)sender).Brain as IControlledBrain;
            else if (e == GameLivingEvent.Dying)
                npc = ((GameNPC)sender).Brain as IControlledBrain;

            if (npc == null) 
            	return;

            // Spell does not stop unless canceled manually by player
            //PulsingSpellEffect concEffect = FindPulsingSpellOnTarget(npc.Owner, this);

            //var concEffect = npc.Owner.effectListComponent.GetSpellEffects(eEffect.Pulse).Where(e => e.SpellHandler.Spell.SpellType == (byte)eSpellType.Charm).FirstOrDefault();

            //if (concEffect != null)
            //    EffectService.RequestImmediateCancelConcEffect((ECSPulseEffect)concEffect);
                //concEffect.CancelEffect = true;
                //concEffect.Cancel(false);

            //GameSpellEffect charm = FindEffectOnTarget(npc.Body, this);
            List<ECSGameEffect> charm = new List<ECSGameEffect>();
            npc.Body?.effectListComponent?.Effects?.TryGetValue(eEffect.Charm, out charm);
            
            if (charm?.Count == 0)// == null)
            {
                log.Warn("charm effect is already canceled");
                return;
            }

            //charm.Cancel(false);
            //charm.FirstOrDefault().CancelEffect = true;
            //if (charm?.FirstOrDefault().GetRemainingTimeForClient() < 0)
            if (e == GameLivingEvent.PetReleased)
                EffectService.RequestImmediateCancelEffect(charm?.FirstOrDefault());
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        { return 0; } // Behaviors moved to CharmECSEffect.cs > OnStopEffect()

        /// <summary>
        /// Determines wether this spell is better than given one
        /// </summary>
        /// <param name="oldeffect"></param>
        /// <param name="neweffect"></param>
        /// <returns>true if this spell is better version than compare spell</returns>
        public override bool IsNewEffectBetter(GameSpellEffect oldeffect, GameSpellEffect neweffect)
        {
	        if (oldeffect.Spell.SpellType != neweffect.Spell.SpellType)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Spell effect compare with different types " + oldeffect.Spell.SpellType + " <=> " + neweffect.Spell.SpellType + "\n" + Environment.StackTrace);
                
                return false;
            }
            
            return neweffect.SpellHandler == this;
        }

        /// <summary>
        /// Send the Effect Animation
        /// </summary>
        /// <param name="target">The target object</param>
        /// <param name="boltDuration">The duration of a bolt</param>
        /// <param name="noSound">sound?</param>
        /// <param name="success">spell success?</param>
        public override void SendEffectAnimation(GameObject target, ushort boltDuration, bool noSound, byte success)
        {
            base.SendEffectAnimation(m_charmedNpc, boltDuration, noSound, success);
        }

        /// <summary>
        /// Delve Info
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();

                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpellHandler.DelveInfo.Function", (Spell.SpellType.ToString() == "" ? "(not implemented)" : Spell.SpellType.ToString())));
                list.Add(" "); //empty line
                list.Add(Spell.Description);
                list.Add(" "); //empty line
                var baseMessage = "Attempts to bring the target monster under the caster's control.";
                switch ((eCharmType) Spell.AmnesiaChance)
                {
	                case eCharmType.All:
		                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.AllMonsterTypes"));
		                break;
	                case eCharmType.Animal:
		                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.Animal"));
		                break;
	                case eCharmType.Humanoid:
		                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.Humanoid"));
		                break;
	                case eCharmType.Insect:
		                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.Insect"));
		                break;
	                case eCharmType.Reptile:
		                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimalInsectMagicalUndead"));
		                break;
	                case eCharmType.HumanoidAnimal:
		                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimal"));
		                break;
	                case eCharmType.HumanoidAnimalInsect:
		                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimalInsect"));
		                break;
	                case eCharmType.HumanoidAnimalInsectMagical:
		                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimalInsectMagical"));
		                break;
	                case eCharmType.HumanoidAnimalInsectMagicalUndead:
		                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimalInsectMagicalUndead"));
		                break;
                }
                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.NonNamedEpic"));
                list.Add(" "); //empty line
                if (Spell.InstrumentRequirement != 0)
                    list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.InstrumentRequire", GlobalConstants.InstrumentTypeToName(Spell.InstrumentRequirement)));
                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.Target", Spell.Target));
                if (Spell.Range != 0)
                    list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.Range", Spell.Range));
                if (Spell.Duration >= ushort.MaxValue * 1000)
                    list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.Duration") + " Permanent.");
                else if (Spell.Duration > 60000)
                    list.Add(string.Format(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.Duration") + Spell.Duration / 60000 + ":" + (Spell.Duration % 60000 / 1000).ToString("00") + " min"));
                else if (Spell.Duration != 0)
                    list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.Duration") + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
                if (Spell.Frequency != 0)
                    list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")));
                if (Spell.Power != 0)
                    list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.PowerCost", Spell.Power.ToString("0;0'%'")));
                list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
                if (Spell.RecastDelay > 60000)
                    list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.RecastTime") + Spell.RecastDelay / 60000 + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min");
                else if (Spell.RecastDelay > 0)
                    list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.RecastTime") + (Spell.RecastDelay / 1000).ToString() + " sec");
                if (Spell.Concentration != 0)
                    list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.ConcentrationCost", Spell.Concentration));
                if (Spell.Radius != 0)
                    list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.Radius", Spell.Radius));
                if (Spell.DamageType != eDamageType.Natural)
                    list.Add(LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "DelveInfo.Damage", GlobalConstants.DamageTypeToName(Spell.DamageType)));

                return list;
            }
        }

        // Constructs new Charm spell handler
        public CharmSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }

        /*
         Information covered below includes sources and info regarding the following:
			1. Hunter
			2. Mentalist
			3. Sorcerer
			4. Minstrel
			5. General Considerations
         
		---------------------------------------
		1. HUNTER
		---------------------------------------
		
		-------------
        http://www.camelotherald.com/more/1775.shtml

        Q: Can you please explain what the max level pet a hunter can charm if they are fully Beastcraft specd? The community feels its no higher then 41, but the builder says max level 50.
			A: Sayeth the Oracle: �It's 82% of the caster's level for the highest charm in beastcraft; or level 41 if the caster is 50. Spec doesn't determine the level of the pet - it's purely based on the spell.�
		-------------
		https://web.archive.org/web/20040602231244/http://camelotvault.ign.com/thegame/guides/200305/hunter.shtml
		Q: Why can't I charm animals? How does charming work?
			A: The release of the updated Prima strategy guide actually gives a surprisingly simple (and accurate) theory on how the Beastcraft charm spells work. All but the last version allow a Hunter to charm up to 80% of his character level, rounded down. However, the last versions of each animal/insect charm spell (at 32 and 35 spec respectively) are not 80%, but 82%, explaining the jump when higher level Hunters are able to charm 1 level higher than they expected.

			Confirmed data to support this includes:

			Level 11, Beastcraft 6, level 8 insect (level 7 animal due to outdated charm spell)
			Level 11, Beastcraft 7, level 8 pet
			Level 17, Beastcraft 17, level 13 pet
			Level 19, Beastcraft 18, level 15 pet
			Level 20, Beastcraft 19, level 16 pet
			Level 21, Beastcraft 20, level 16 pet
			Level 22, Beastcraft 23, level 17 pet
			Level 23, Beastcraft 24, level 18 pet
			Level 44, Beastcraft 35, level 36 pet
			Level 47, Beastcraft 43, level 38 pet
			Level 48, Beastcraft 43, level 39 pet
			Level 49, Beastcraft 43, level 40 pet
			Level 50, Beastcraft 35, level 41 pet
			Level 50, Beastcraft 43, level 41 pet

			There is a limit to how high a level each version of the charm spell can charm. However, the limits are not easily confirmable because most Hunters spec relatively high in beastcraft.

			Note that these spells work in rather odd ways. I have found that the Call of Gleipnir will work on anything using a bear, wolf, cat, frog, badger, horse, or rat model, but will not work on humanoids or snakes (although Squabblers in Hibernia work). In addition, Call of Gleipnir has been known to work on Torpor Worms in Uppland/Yggdra forest, but not anything else with a worm model. I have also found the Compel Insect spell to work on ants, crabs, spiders, and even arachites.
		-------------
        http://vnboards.ign.com/message.asp?topic=87170081&start=87173224&search=charm

		---------------------------------------
        2. MENTALIST
        ---------------------------------------
        -------------
        https://web.archive.org/web/20040607170841/http://camelotvault.ign.com/thegame/guides/Hibernia/mentalist.shtml
        Charm takes a mob and makes the mob fight for the caster.  It is limited to 110% of the spec into light. 
        -------------
        
        4 - humanoids
        10 - humanoids, animals
        17 - humanoids, animals, insects
        25 - humanoids, animals, insects, magical
        33 - humanoids, animals, insects, magical, undead
        42 - anything charmable

        Always use lowest charm to save power.
        
        -------------
		https://web.archive.org/web/20031020201314/http://www.classesofcamelot.com/misc/ClassGuides/mentalist2.asp
        The charm in Illusions is rather a fun one, though it’s a bit hard to use at the beginning. It doesn’t permacharm, but it “ticks” every four seconds and every tick charms the pet for ten seconds. The advantage of the charm is that it allows you too charm higher level mobs. (your level*0.6)+(your lightspec*0.4) is the formula for this. So when you’re 50 and you have 50 light spec you can charm mobs up to 50.

		But items (cap for this is [level/5+1] at fifty this is 11) and Realm Ranks (one for every RR, so +5 at RR5 or +10 at RR10) can add to your light spec so you could gain 60+ light spec at 50. Which gives you level 54+ mobs for pets.

		There are downsides to this too. Every tick drains power, though not a lot and with Pot 1 you won’t notice it at all. Also your pet sometimes resists a pulse and when you’re fighting a mob when this happens the mob will come straight for you. When your pet resists two pulses in a row, it will attack you. It will recharm again, but to be on the safe side better throw a mezz at it. There is a max range (2000) on the charm, so it’ll break when your pet is that far away from you. When you’re mezzed or stunned your charm won’t pulse and this has the same effect as the pet resisting a pulse, i.e. attacking you.

		The charm comes in six upgrades each will give more mobs to control, but drain more power.

		You should note that when you control a level 50 human mob you should use the first spell because it drains less power and has the same effect.
		-------------
        <Begin Info: Imaginary Enemy>
        Function: charm
 
        Attempts to bring the target under the caster's control.
 
        Target: Targetted
        Range: 2000
        Duration: 10 sec
        Frequency:      4.8 sec
        Casting time:      3.0 sec
        Damage: Heat
 
        <End Info>

        [16:11:59] You begin casting a Imaginary Enemy spell!
        [16:11:59] You are already casting a spell!  You prepare this spell as a follow up!
        [16:12:01] You are already casting a spell!  You prepare this spell as a follow up!
        [16:12:02] You cast a Imaginary Enemy Spell!
        [16:12:02] The villainous youth is now under your control.
        [16:12:02] You cancel your effect.

        [16:11:42] You can't attack yourself!
        [16:11:42] You lose control of the villainous youth!
        [16:11:42] You lose control of the villainous youth.

		---------------------------------------
		3. SORCERER
		---------------------------------------
        <Begin Info: Coerce Will>
        Function: charm
 
        Attempts to bring the target under the caster's control.
 
        Target: Targetted
        Range: 1000
        Duration: Permanent.
        Power cost: 25%
        Casting time:      4.0 sec
        Damage: Energy
 
        <End Info>
        
        https://web.archive.org/web/20040104082639/http://www.camelotherald.com:80/spells/line.php?line=850
        https://web.archive.org/web/20031021173240/http://www.enygma.net:80/Sorcerer_Guide.html (Basic Charming 101)
        1	Persuade Will	Charm humanoids								*Charms up to 10th lvl mobs
		7	Coerce Will		Charm humans, animals						*Charms up to 15th lvl mobs
		12	Compel Will		Charm humans, animals, insects				*Charms up to 26th lvl mobs
		20	Control Will	Charm humans, animals, insects, magical		*Charms up to 40th lvl mobs
		32	Wrest Will		Charm any creature							*Charms up to 50th lvl mobs

        [06:23:57] You begin casting a Coerce Will spell!
        [06:24:01] The slough serpent attacks you and misses!
        [06:24:01] You cast a Coerce Will Spell!
        [06:24:01] The slough serpent is enthralled!
        [06:24:01] The slough serpent is now under your control.

        [14:30:55] The frost stallion dies!
        [14:30:55] This monster has been charmed recently and is worth no experience.

		---------------------------------------
		4. MINSTREL
		---------------------------------------
		
        [09:00:12] <Begin Info: Attracting Melodies>
        [09:00:12] Function: charm
        [09:00:12]
        [09:00:12] Attempts to bring the target under the caster's control.
        [09:00:12]
        [09:00:12] Target: Targetted
        [09:00:12] Range: 2000
        [09:00:12] Duration: 10 sec
        [09:00:12] Frequency:      5.0 sec    (Post-1.65 level, originally 4.75)
        [09:00:12] Casting time: instant
        [09:00:12] Recast time: 5 sec
        [09:00:12]
        [09:00:12] <End Info>
        -------------
        https://web.archive.org/web/20031218095034/http://www.kungfugeek.com/~powersong/
        https://web.archive.org/web/20031203162501/http://camelotvault.ign.com:80/thegame/guides/april03/minstrel.shtml
        Q: What do the various levels of Minstrel charm do (what can I charm?)
			A: There are six levels of the Minstrel charm chant. Of the first four levels, each new level gives access to a new NPC type, plus all the types from the lower versions. The fifth and sixth songs simply offer improved resist-rates.

		6  Captivating Melodies		Charms Humanoids.
		13 Enchanting Melodies		Charms Humanoids and Animals.
		20 Attracting Melodies		Charms Humanoids, Animals and Insects.
		27 Pleasurable Melodies		Charms Humanoids, Animals, Insects and Magical Creatures.
		34 Enticing Melodies		Charms Humanoids, Animals, Insects and Magical Creatures.
		41 Alluring Melodies		Charms Humanoids, Animals, Insects and Magical Creatures.
		
		Q: What is over-charming, and how does one do it?
			A: Overcharming is the ability to charm an NPC which is higher than your level. Your ability to charm is directly related to your Instrument skill. Generally, it is advisable to keep your Instrument skill-level greater than or equal to the level of the NPC you are attempting to keep charmed.

			Overcharming can be risky. A high Instrument score will help, but you may still experience difficulty with pets that are significantly higher level than you.
		-------------
        [09:05:56] You command the the worker ant to kill your target!
        [09:05:59] The worker ant attacks the worker ant and hits!
        [09:06:00] The worker ant attacks the worker ant and hits!
        [09:06:01] You lose control of the worker ant!
        [09:06:01] You release control of your controlled target.

        [09:06:50] The worker ant is now under your control.
        [09:06:51] The worker ant attacks you and misses!
        [09:06:55] The worker ant attacks the worker ant and hits!
        [09:06:55] The worker ant resists the charm!

		---------------------------------------
		5. GENERAL CONSIDERATIONS
		---------------------------------------
		
		Safety level formula:
        (level * .66) + (modified spec level * .33)
        modified spec level includes: trainings, items, and realm rank

        Mastery of Focus:
        Mastery of Focus affects SPELL level. Notice that SPELL level is not included in the above formula. SPEC level is important. If you raise the lvl 4 charm up to lvl 20 it makes NO difference to what you can charm.

        Current charm bugs:
        - Porting has the chance to completely break your charm if there is a delay in porting. Pet will show up at portal location very very mad.
        - Porting also causes your pet to completely disappear. Walk away and it should reappear. Maybe

        NOT A BUG, working as intended
        - Artifact chants (Cloudsong, Crown, etc.) will interfere and overwrite your charm.

         */
    }
}
