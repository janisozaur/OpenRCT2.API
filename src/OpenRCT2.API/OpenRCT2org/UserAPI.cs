using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenRCT2.API.Extensions;

namespace OpenRCT2.API.OpenRCT2org
{
    public class UserApi : IUserApi
    {
        private const string ApiUrl = "https://openrct2.org/forums/jsonapi.php";

        private readonly ILogger<UserApi> _logger;
        private readonly UserApiOptions _options;
        private readonly HttpClient _httpClient;

        private string AppToken => _options.ApplicationToken;

        public UserApi(ILogger<UserApi> logger, IOptions<UserApiOptions> options, HttpClient httpClient)
        {
            _logger = logger;
            _options = options.Value;
            _httpClient = httpClient;
        }

        public async Task<JUser> GetUserAsync(int id)
        {
            _logger.LogInformation("[OpenRCT2.org] Get user id {0}", id);

            var payload = new
            {
                key = AppToken,
                command = "getUser",
                userId = id
            };

            string responseJson = await PostJsonAsync(payload);
            var jResponse = JsonConvert.DeserializeObject<JResponse>(responseJson);
            if (jResponse.error != 0)
            {
                throw new OpenRCT2orgException(jResponse);
            }

            var user = JsonConvert.DeserializeObject<JUser>(responseJson);
            return user;
        }

        public async Task<JUser> AuthenticateUserAsync(string userName, string password)
        {
            _logger.LogInformation("[OpenRCT2.org] Authenticate user '{0}'", userName);

            var payload = new
            {
                key = AppToken,
                command = "authenticate",
                name = userName,
                password = password
            };

            string responseJson = await PostJsonAsync(payload);
            var jResponse = JsonConvert.DeserializeObject<JResponse>(responseJson);
            if (jResponse.error != 0)
            {
                _logger.LogInformation("[OpenRCT2.org] Authentication failed for user '{0}': {1}", userName, jResponse.errorMessage);
                throw new OpenRCT2orgException(jResponse);
            }

            var user = JsonConvert.DeserializeObject<JUser>(responseJson);
            return user;
        }

        private async Task<string> PostJsonAsync(object payload)
        {
            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, MimeTypes.ApplicationJson);

            var response = await _httpClient.PostAsync(ApiUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new OpenRCT2orgException(ErrorCodes.InternalError, "Unsuccessful response from server.");
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
