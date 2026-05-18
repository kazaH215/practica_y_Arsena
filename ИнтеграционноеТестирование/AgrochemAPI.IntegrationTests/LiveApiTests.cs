using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace AgrochemAPI.IntegrationTests
{
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public string Token { get; set; } = "";
    }

    public class LiveApiTests
    {
        private readonly HttpClient _client = new HttpClient { BaseAddress = new System.Uri("https://localhost:7093") };

        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            var response = await _client.PostAsJsonAsync("/api/Auth/login", new { username = "tech.ivanov", password = "123" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
        }

        [Fact]
        public async Task GetProducts_WithToken_ReturnsOk()
        {
            // Логинимся
            var login = await _client.PostAsJsonAsync("/api/Auth/login", new { username = "tech.ivanov", password = "123" });
            var token = (await login.Content.ReadFromJsonAsync<LoginResponse>()).Token;
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/Reference/products");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var products = await response.Content.ReadFromJsonAsync<dynamic>();
            Assert.NotNull(products);
        }

        [Fact]
        public async Task GetRecipes_WithoutToken_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null;
            var response = await _client.GetAsync("/api/Recipes");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}