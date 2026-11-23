namespace HardwareStore.Services;

/// <summary>Reads and writes the logged-in identity stored in the session.</summary>
public class CurrentUserSession
{
    private const string UserIdKey = "App_UserId";
    private const string RoleKey = "App_Role";
    private const string UserNameKey = "App_UserName";

    private readonly IHttpContextAccessor _accessor;

    public CurrentUserSession(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext.Session;

    public int? UserId => Session.GetInt32(UserIdKey);

    public string Role => Session.GetString(RoleKey);

    public string UserName => Session.GetString(UserNameKey);

    public bool IsLoggedIn => UserId.HasValue;

    public bool IsInRole(string role) => Role == role;

    public void SignIn(int userId, string role, string userName)
    {
        Session.SetInt32(UserIdKey, userId);
        Session.SetString(RoleKey, role);
        Session.SetString(UserNameKey, userName);
    }

    public void SignOut()
    {
        Session.Remove(UserIdKey);
        Session.Remove(RoleKey);
        Session.Remove(UserNameKey);
    }
}
