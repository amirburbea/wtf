namespace Beacon.Excel.Objects.User
{
    public interface IUser
    {
        string FirstName { get; }

        string LastName { get; }

        string Token { get; }

        string UserName { get; }
    }
}