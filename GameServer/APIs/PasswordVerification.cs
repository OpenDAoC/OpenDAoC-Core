using DOL.GS.ServerProperties;

namespace DOL.GS.API;

public class PasswordVerification
{
    public PasswordVerification()
    {

    }

    public bool VerifyAPIPassword(string password)
    {
        var apiPassword = Properties.API_PASSWORD;
        if (apiPassword is (null or "")) return false;
        if (password is (null or "")) return false;
        if (password != apiPassword) return false;
        return true;
    }
}
