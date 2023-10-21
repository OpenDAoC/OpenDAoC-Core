using Core.Database;

namespace Core.GS;

public class KillCreditUtil
{
    public static string GetRequiredKillMob(string id_nb)
    {
        var mobToKill = CoreDb<DbItemXKillCredit>.SelectObject(DB.Column("ItemTemplate")
            .IsEqualTo(id_nb));

        return mobToKill?.m_mobName;
    }
}