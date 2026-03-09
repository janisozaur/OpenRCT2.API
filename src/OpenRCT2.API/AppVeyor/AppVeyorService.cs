using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OpenRCT2.API.AppVeyor
{
    public class AppVeyorService : IAppVeyorService
    {
        private const string ApiUrl = "https://ci.appveyor.com/api/";
        private readonly HttpClient _httpClient;

        public AppVeyorService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public class JAppVeyorBuildResponse
        {
            public JProject project { get; set; }
            public JBuild build { get; set; }
        }

        public class JAppVeyorMessagesResponse
        {
            public JMessage[] list { get; set; }
        }

        public Task<JBuild> GetLastBuildAsync(string account, string project)
        {
            return GetLastBuildAsync(account, project, null);
        }

        public async Task<JBuild> GetLastBuildAsync(string account, string project, string branch)
        {
            string url = $"{ApiUrl}projects/{account}/{project}";
            if (branch != null)
            {
                url += $"/branch/{branch}";
            }

            var responseJson = await _httpClient.GetStringAsync(url);
            var response = JsonConvert.DeserializeObject<JAppVeyorBuildResponse>(responseJson);
            return response.build;
        }

        public async Task<string> GetLastBuildJobIdAsync(string account, string project, string branch)
        {
            JBuild build = await GetLastBuildAsync(account, project, branch);
            if (build.jobs.Length > 0)
            {
                JJob job = build.jobs[0];
                return job.jobId;
            }
            else
            {
                return null;
            }
        }

        public async Task<JMessage[]> GetMessagesAsync(string jobId)
        {
            string url = $"{ApiUrl}buildjobs/{jobId}/messages";
            var responseJson = await _httpClient.GetStringAsync(url);
            var response = JsonConvert.DeserializeObject<JAppVeyorMessagesResponse>(responseJson);
            return response.list;
        }
    }
}
