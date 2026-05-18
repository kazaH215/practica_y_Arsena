using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AgrochemOperator.Models;

namespace AgrochemOperator.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://localhost:7093";

        public ApiService()
        {
            _httpClient = new HttpClient();
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        public void SetToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
            var content = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"GET {endpoint} -> {response.StatusCode}: {content}");
            return JsonConvert.DeserializeObject<T>(content);
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"POST {endpoint} -> {response.StatusCode}: {responseContent}");
            return JsonConvert.DeserializeObject<T>(responseContent);
        }

        // --- Авторизация ---
        public async Task<LoginResponse> Login(string username, string password)
        {
            try
            {
                var result = await PostAsync<JObject>("/api/Auth/login", new { username, password });
                if (result != null)
                {
                    var token = result["token"]?.Value<string>();
                    if (!string.IsNullOrEmpty(token))
                    {
                        return new LoginResponse
                        {
                            UserId = result["userId"]?.Value<int>() ?? 0,
                            Username = result["username"]?.Value<string>(),
                            FullName = result["fullName"]?.Value<string>(),
                            Role = result["role"]?.Value<string>(),
                            Token = token
                        };
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка логина: {ex.Message}");
                return null;
            }
        }

        // --- Активные партии ---
        public async Task<List<ProductionBatch>> GetActiveBatches()
        {
            var result = await GetAsync<JObject>("/api/Batches/active");
            if (result?["success"]?.Value<bool>() == true)
            {
                return result["data"].ToObject<List<ProductionBatch>>();
            }
            return new List<ProductionBatch>();
        }

        // --- Шаги техкарты (исправленный метод) ---
        public async Task<List<TechStep>> GetBatchSteps(int batchId)
        {
            var batchResult = await GetAsync<JObject>($"/api/Batches/{batchId}");
            if (batchResult?["success"]?.Value<bool>() != true) return null;
            var batch = batchResult["data"].ToObject<ProductionBatch>();
            if (batch == null) return null;

            var orderResult = await GetAsync<JObject>($"/api/ProductionOrders/{batch.OrderId}");
            if (orderResult?["success"]?.Value<bool>() != true) return null;
            var order = orderResult["data"];
            int? techCardId = order["techCardId"]?.Value<int>();
            if (!techCardId.HasValue) return null;

            var techCardResult = await GetAsync<JObject>($"/api/TechCards/{techCardId}");
            if (techCardResult?["success"]?.Value<bool>() != true) return null;
            var techCard = techCardResult["data"];
            var steps = techCard["steps"]?.ToObject<List<TechStep>>();
            return steps ?? new List<TechStep>();
        }

        // --- Выполнения шагов для партии ---
        public async Task<List<BatchStepExecution>> GetBatchStepExecutions(int batchId)
        {
            var result = await GetAsync<JObject>($"/api/BatchSteps/batch/{batchId}");
            if (result?["success"]?.Value<bool>() == true)
            {
                return result["data"].ToObject<List<BatchStepExecution>>();
            }
            return new List<BatchStepExecution>();
        }

        public async Task<bool> StartStep(int batchId, int stepId, int userId)
        {
            var result = await PostAsync<JObject>("/api/BatchSteps/start", new { batchId, stepId, userId });
            return result?["success"]?.Value<bool>() == true;
        }

        public async Task<bool> CompleteStep(int executionId, int userId, string actualParams, string comment)
        {
            var result = await PostAsync<JObject>("/api/BatchSteps/complete", new { executionId, userId, actualParams, comment });
            return result?["success"]?.Value<bool>() == true;
        }

        public async Task<bool> ReportDeviation(Deviation deviation)
        {
            var result = await PostAsync<JObject>("/api/Deviations", deviation);
            return result?["success"]?.Value<bool>() == true;
        }
    }

    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
    }
}