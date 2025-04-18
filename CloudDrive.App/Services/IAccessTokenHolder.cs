namespace CloudDrive.App.Services
{
    public interface IAccessTokenHolder
    {
        void HoldAccessToken(string token);
        string? GetAccessToken();
        void DiscardAccessToken();
    }
}
