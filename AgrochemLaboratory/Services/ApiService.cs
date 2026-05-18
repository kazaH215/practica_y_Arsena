using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AgrochemLaboratory.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgrochemLaboratory.Services
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
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"GET {endpoint} -> {response.StatusCode}: {content}");
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GET Error: {ex.Message}");
                return default;
            }
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                Debug.WriteLine($"POST {endpoint} body: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"POST {endpoint} -> {response.StatusCode}: {responseContent}");
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"POST Error: {ex.Message}");
                return default;
            }
        }

        public async Task<T> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_baseUrl}{endpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"PUT {endpoint} -> {response.StatusCode}: {responseContent}");
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PUT Error: {ex.Message}");
                return default;
            }
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
                Debug.WriteLine($"Ошибка логина: {ex.Message}");
                return null;
            }
        }

        // --- Лаборатория ---
        public async Task<List<RawMaterialBatchModel>> GetPendingRawMaterials()
        {
            var result = await GetAsync<ApiResponse<List<RawMaterialBatchModel>>>("/api/LabTests/pending/raw-materials");
            if (result != null && result.success)
                return result.data ?? new List<RawMaterialBatchModel>();
            return new List<RawMaterialBatchModel>();
        }

        public async Task<List<ProductionBatchModel>> GetPendingBatches()
        {
            var result = await GetAsync<ApiResponse<List<ProductionBatchModel>>>("/api/LabTests/pending/batches");
            if (result != null && result.success)
                return result.data ?? new List<ProductionBatchModel>();
            return new List<ProductionBatchModel>();
        }

        public async Task<LabTestModel> GetLabTest(int testId)
        {
            var result = await GetAsync<ApiResponse<LabTestModel>>($"/api/LabTests/{testId}");
            if (result != null && result.success)
                return result.data;
            return null;
        }

        public async Task<int> CreateLabTest(CreateLabTestDto dto)
        {
            try
            {
                var result = await PostAsync<JObject>("/api/LabTests", dto);
                if (result != null)
                {
                    bool success = result["success"]?.Value<bool>() ?? false;
                    if (success)
                    {
                        return result["data"]?["id"]?.Value<int>() ?? 0;
                    }
                    else
                    {
                        string error = result["message"]?.Value<string>() ?? "Неизвестная ошибка";
                        Debug.WriteLine($"CreateLabTest error: {error}");
                        throw new Exception(error);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CreateLabTest exception: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> AddTestParameters(int testId, List<LabTestParameterDto> parameters)
        {
            var result = await PostAsync<JObject>($"/api/LabTests/{testId}/parameters", parameters);
            return result != null && result["success"]?.Value<bool>() == true;
        }

        public async Task<bool> EnterLabResults(EnterLabResultDto dto)
        {
            var result = await PutAsync<JObject>("/api/LabTests/results", dto);
            return result != null && result["success"]?.Value<bool>() == true;
        }

        public async Task<bool> MakeDecision(LabDecisionDto dto)
        {
            var result = await PostAsync<JObject>("/api/LabTests/decision", dto);
            return result != null && result["success"]?.Value<bool>() == true;
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