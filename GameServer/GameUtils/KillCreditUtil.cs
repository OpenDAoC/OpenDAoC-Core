using DOL.Database;

namespace DOL.GS;

public class KillCreditUtil
{
    public static string GetRequiredKillMob(string id_nb)
    {
        var mobToKill = CoreDb<DbItemXKillCredits>.SelectObject(DB.Column("ItemTemplate")
            .IsEqualTo(id_nb));

        return mobToKill?.m_mobName;
    }
}