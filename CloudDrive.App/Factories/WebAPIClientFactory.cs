using CloudDrive.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CloudDrive.App.Factories
{
    public class WebAPIClientFactory
    {
        private readonly IAccessTokenHolder _authTokenHolder;
        private readonly IUserSettingsService _userSettingsService;

        public WebAPIClientFactory(IAccessTokenHolder authTokenHolder, IUserSettingsService userSettingsService)
        {
            _authTokenHolder = authTokenHolder;
            _userSettingsService = userSettingsService;
        }


        public WebAPIClient Create()
        {
            var serverUrl = _userSettingsService.ServerUrl;
            var token = _authTokenHolder.GetAccessToken();

            var httpClient = new HttpClient
            {
                DefaultRequestHeaders = { 
                    Authorization = (token != null) ? new AuthenticationHeaderValue("Bearer", _authTokenHolder.GetAccessToken()) : null 
                }
            };

            return new WebAPIClient(serverUrl?.ToString(), httpClient);
        }
    }
}
