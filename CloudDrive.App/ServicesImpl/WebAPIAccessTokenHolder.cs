using CloudDrive.App.Services;

namespace CloudDrive.App.ServicesImpl
{
    public class WebAPIAccessTokenHolder : IAccessTokenHolder
    {
        private string? AuthToken {  get; set; }


        public void HoldAccessToken(string token)
        {
            AuthToken = token;
        }

        public string? GetAccessToken()
        {
            return AuthToken;
        }

        public void DiscardAccessToken()
        {
            AuthToken = null;
        }
    }
}
