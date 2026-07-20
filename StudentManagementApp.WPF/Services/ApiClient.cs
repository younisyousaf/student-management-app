using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using StudentManagementApp.WPF.DTOs;

namespace StudentManagementApp.WPF.Services
{
    public class ApiClient
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "https://localhost:7202/api/";

        public ApiClient()
        {
            _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

        private void AuthenticateRequest()
        {
            if (AuthService.Instance.IsAuthenticated)
            {
                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", AuthService.Instance.JwtToken);
            }
        }

        public async Task<HttpResponseMessage> LoginAsync(LoginRequest dto)
        {
            return await _client.PostAsJsonAsync("Auth/login", dto);
        }

        public async Task<HttpResponseMessage> RegisterAsync(RegisterRequest dto)
        {
            return await _client.PostAsJsonAsync("Auth/register", dto);
        }

        /// <summary>
        /// Upgraded generic fetcher with strict HTTP validation to properly trip catch blocks on API failure.
        /// </summary>
        public async Task<T?> GetAsync<T>(string endpoint)
        {
            AuthenticateRequest();

            // Fetch the raw response first
            var response = await _client.GetAsync(endpoint);

            // Explicitly throw an exception if it's a 401, 404, or 500 error code
            response.EnsureSuccessStatusCode();

            // If the DB returns a 204 No Content, return default/null to trigger empty state UI loops cleanly
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return default;
            }

            // Safely parse JSON payload mapping
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T payload)
        {
            AuthenticateRequest();
            return await _client.PostAsJsonAsync(endpoint, payload);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T data)
        {
            AuthenticateRequest();
            return await _client.PutAsJsonAsync(endpoint, data);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            AuthenticateRequest();
            return await _client.DeleteAsync(endpoint);
        }
    }
}