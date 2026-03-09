using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using OpenRCT2.API;
using Xunit;

namespace ApiIntegrationTests
{
    public class TestLocalisation : System.IDisposable
    {
        private readonly IHost _host;
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public TestLocalisation()
        {
            _host = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseEnvironment(OpenRCT2.API.Environments.Testing)
                        .UseStartup<Startup>()
                        .UseTestServer();
                })
                .Start();

            _server = _host.GetTestServer();
            _client = _server.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
            _host.Dispose();
        }

        [Theory(Skip = "progressed.io is down")]
        [InlineData("unk")]
        [InlineData("en-GB")]
        [InlineData("nl-NL")]
        public async Task TestBadgeAsync(string langId)
        {
            var response = await _client.GetAsync($"/localisation/status/badges/{langId}");
            response.EnsureSuccessStatusCode();

            Assert.Equal("image/svg+xml", response.Content.Headers.ContentType.MediaType);
            Assert.StartsWith("<svg ", await response.Content.ReadAsStringAsync());
        }
    }
}
