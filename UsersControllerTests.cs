using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;

namespace Server.Tests
{
    [TestFixture]
    public class UsersControllerTests
    {
        private HttpClient? _client;
        private string? _token;

        [SetUp]
        public async Task Setup()
        {
            var factory = new ServerApplication();
            _client = factory.CreateClient();
            _token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        }

        [Test]
        public async Task Register_ValidUser_ReturnsSuccess()
        {
            var user = new { Username = $"testuser_{Guid.NewGuid()}", Password = "testpassword" };
            var content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");

            var response = await _client!.PostAsync("/api/users/register", content);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("token");
        }

        [Test]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            var username = $"testuser_{Guid.NewGuid()}";
            var password = "testpassword";

            var registerContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var registerResponse = await _client!.PostAsync("/api/users/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();

            var loginContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");

            var loginResponse = await _client.PostAsync("/api/users/login", loginContent);

            loginResponse.EnsureSuccessStatusCode();
            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            loginResult.Should().Contain("token");
        }

        [Test]
        public async Task ChangePassword_ValidRequest_ReturnsNewToken()
        {
            var username = $"testuser_{Guid.NewGuid()}";
            var password = "testpassword";

            var registerContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var registerResponse = await _client!.PostAsync("/api/users/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();

            var loginContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/users/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<JsonElement>(loginResult).GetProperty("token").GetString();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var newPassword = "newpassword";
            var changePasswordContent = new StringContent(JsonSerializer.Serialize(new { NewPassword = newPassword }), Encoding.UTF8, "application/json");

            var changePasswordResponse = await _client.PatchAsync("/api/users/password", changePasswordContent);

            changePasswordResponse.EnsureSuccessStatusCode();
            var changePasswordResult = await changePasswordResponse.Content.ReadAsStringAsync();
            changePasswordResult.Should().Contain("token");
        }

        [Test]
        public async Task GetHistory_ReturnsHistory()
        {
            var username = $"testuser_{Guid.NewGuid()}";
            var password = "testpassword";

            var registerContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var registerResponse = await _client!.PostAsync("/api/users/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();

            var loginContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/users/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<JsonElement>(loginResult).GetProperty("token").GetString();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var historyResponse = await _client.GetAsync("/api/users/history");

            historyResponse.EnsureSuccessStatusCode();
            var historyResult = await historyResponse.Content.ReadAsStringAsync();
            historyResult.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task DeleteHistory_ReturnsSuccess()
        {
            var username = $"testuser_{Guid.NewGuid()}";
            var password = "testpassword";

            var registerContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var registerResponse = await _client!.PostAsync("/api/users/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();

            var loginContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/users/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<JsonElement>(loginResult).GetProperty("token").GetString();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var deleteHistoryResponse = await _client.DeleteAsync("/api/users/history");

            deleteHistoryResponse.EnsureSuccessStatusCode();
        }

        private async Task<string> GetTokenAsync()
        {
            var username = $"testuser_{Guid.NewGuid()}";
            var password = "testpassword";

            var registerContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var registerResponse = await _client!.PostAsync("/api/users/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();

            var loginContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/users/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<JsonElement>(loginResult).GetProperty("token").GetString();

            return token!;
        }
    }
}