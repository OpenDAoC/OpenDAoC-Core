using System;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&cast",
		ePrivLevel.GM,
		"GMCommands.Cast.Description",
		"/cast loadspell <spellid> Load a spell from the DB into the global spell cache",
		"GMCommands.Cast.Usage")]
	public class CastCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			string type = args[1].ToLower();

			int id = 0;
			try
			{
				id = Convert.ToInt32(args[2]);
			}
			catch
			{
				DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Cast.InvalidId"));
				return;
			}
			if (id < 0)
			{
				DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Cast.IdNegative"));
				return;
			}

			if (type == "loadspell")
			{
				if (id != 0)
				{
					if (SkillBase.UpdateSpell(id))
					{
						Log.DebugFormat("Spell ID {0} added / updated in the global spell list", id);
						DisplayMessage(client, "Spell ID {0} added / updated in the global spell list", id);
						return;
					}
				}

				DisplayMessage(client, "Error loading Spell ID {0}!", id);
				return;
			}

			GameLiving target = client.Player.TargetObject as GameLiving;
			if (target == null)
				target = client.Player as GameLiving;

			switch (type)
			{
					#region Effect
				case "effect":
					{
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Cast.EffectExecuted", id.ToString()));

						DummyEffect effect = new DummyEffect((ushort)id);
						effect.Start(client.Player);

						foreach (GamePlayer player in client.Player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
							player.Out.SendSpellEffectAnimation(client.Player, target, (ushort)id, 0, false, 1);

						break;
					}
					#endregion Effect
					#region Cast
				case "cast":
					{
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Cast.CastExecuted", id.ToString()));
						foreach (GamePlayer player in client.Player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
							player.Out.SendSpellCastAnimation(client.Player, (ushort)id, 30);
						break;
					}
					#endregion Cast
					#region Spell
				case "spell":
					{
						Spell spell = SkillBase.GetSpellByID(id);
						SpellLine line = new("GMCast", "GM Cast", "unknown", false);

						if (spell != null)
						{
							if (target is GamePlayer targetPlayer && targetPlayer != client.Player && spell.Target != eSpellTarget.SELF)
							{
								DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Cast.Spell.CastOnLiving", spell.Name, target.Name));
								DisplayMessage(targetPlayer.Client, LanguageMgr.GetTranslation(targetPlayer.Client, "GMCommands.Cast.Spell.GMCastOnYou", client.Account.PrivLevel == 2 ? "GM" : "Admin", client.Player.Name, spell.Name));
							}
							else if (target == client.Player || spell.Target == eSpellTarget.SELF)
								DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Cast.Spell.CastOnSelf", spell.Name));

							ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(client.Player, spell, line);

							if (spellHandler != null)
								spellHandler.StartSpell(target);
						}
						else
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Cast.Spell.Inexistent", id.ToString()));

						break;
					}
					#endregion Spell
					#region Style
				case "style":
					{
						if (client.Player.ActiveWeapon == null)
						{
							DisplayMessage(client, "You need an active weapon to play style animation.");
							return;
						}

						client.Player.Out.SendCombatAnimation(client.Player, null, (ushort)client.Player.ActiveWeapon.Model, 0, id, 0, (byte)11, 100);
						break;
					}
					#endregion Style
					#region Sound
				case "sound":
					DisplayMessage(client,
					               LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Cast.SoundPlayed", id.ToString()));
					client.Player.Out.SendSoundEffect((ushort)id, 0, 0, 0, 0, 0);
					break;
					#endregion
					#region Default
				default:
					{
						DisplaySyntax(client);
						break;
					}
					#endregion Default
			}
		}
	}
}
