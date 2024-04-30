using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
/// <summary>
/// Represents an in-game GameHealer NPC
/// </summary>
public class BPMob : GameNPC
{

    public override void Die(GameObject killer)

    {

        GamePlayer player = killer as GamePlayer;
        int basebp = 0;
        if (Level <= 44) { basebp = 5; }
        if (Level == 45) { basebp = 10; }
        if (Level == 46) { basebp = 15; }
        if (Level == 47) { basebp = 20; }
        if (Level == 48) { basebp = 25; }
        if (Level == 49) { basebp = 30; }
        if (Level == 50) { basebp = 35; }

        int rewardbp;
        bool isjackpot;

        int multiplier = Util.Random(2, 3);
        int bonus = Util.Random(1, 3);
        int chance = Util.Random(1, 25);
        
        if (chance == 25)
        {
            isjackpot = true;
        }
        else
        {
            isjackpot = false;
        }
        if (isjackpot)
        {
            rewardbp = ((basebp + bonus) * multiplier);
        }
        else
        {
            rewardbp = (basebp + bonus);
        }

        int playersonline = ClientService.GetNonGmPlayers().Count;

        if (player != null && IsWorthReward)
        {
            if (player.Group != null)
            {
                if (player.Group.MemberCount  == 1) { rewardbp = (rewardbp); }
                if (player.Group.MemberCount  == 2) { rewardbp = (rewardbp / 2); }
                if (player.Group.MemberCount  == 3) { rewardbp = (rewardbp / 3); }
                if (player.Group.MemberCount  == 4) { rewardbp = (rewardbp / 4); }
                if (player.Group.MemberCount  == 5) { rewardbp = (rewardbp / 5); }
                if (player.Group.MemberCount  == 6) { rewardbp = (rewardbp / 6); }
                if (player.Group.MemberCount  == 7) { rewardbp = (rewardbp / 7); }
                if (player.Group.MemberCount  >= 8) { rewardbp = (rewardbp / 8); }
                              
                foreach (GamePlayer player2 in player.Group.GetMembersInTheGroup())
                {

                    if (player2.RealmPoints >= 1755250)
                    {
                        player2.Out.SendMessage("You are RR7 or higher, you will not be rewarded here anymore!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        player2.MoveTo(79, 32401, 12245, 17413, 1902);

                    }
                    if (playersonline >= 50)
                    {
                        if ((player2.Client.Account.PrivLevel == 1) && (player2.CurrentRegionID == 249))
                        {
                            player2.Out.SendMessage("There are " + playersonline + " players online and your in the farmzone, why don't you go play with them!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            player2.MoveTo(79, 32401, 12245, 17413, 1902);
                        }
                    }

                    if (player2.CurrentRegionID == 249) { player2.BountyPoints += rewardbp; }
                    if (isjackpot) { player2.Out.SendMessage("JACKPOT!!!", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow); player2.Out.SendPlaySound(eSoundType.Craft, 0x04); player2.Out.SendMessage("You just got " + multiplier + "x multiplier bonus points!  Woot!", eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow); }
                    player2.Out.SendMessage("You Get " + rewardbp + " bounty points!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    player2.Out.SendMessage("You Get " + rewardbp + " bounty points!", eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);

                }


            }

            else
            {
                if (player.RealmPoints >= 1755250)
                {
                    player.Out.SendMessage("You are RR7 or higher, you will not be rewarded here anymore!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    player.MoveTo(79, 32401, 12245, 17413, 1902);

                }
                else
                {
                    if (playersonline >= 50)
                    {
                        if ((player.Client.Account.PrivLevel == 1) && (player.CurrentRegionID == 249))
                        {
                            player.Out.SendMessage("There are " + playersonline + " players online and your in the farmzone, why don't you go play with them!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            player.MoveTo(79, 32401, 12245, 17413, 1902);
                        }
                    }

                    if (player.CurrentRegionID == 249) { player.BountyPoints += rewardbp; }
                    if (isjackpot) { player.Out.SendMessage("JACKPOT!!!", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow); player.Out.SendPlaySound(eSoundType.Craft, 0x04); player.Out.SendMessage("You just got " + multiplier + "x multiplier bonus points!  Woot!", eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow); }
                    player.Out.SendMessage("You Get " + rewardbp + " bounty points!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage("You Get " + rewardbp + " bounty points!", eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);

                }
            }

            //DropLoot(killer);

        }

        base.Die(killer);

        if ((Faction != null) && (killer is GamePlayer))

        {

            GamePlayer player3 = killer as GamePlayer;

            Faction.OnMemberKilled(player3);

        }

        StartRespawn();

    }

}

}