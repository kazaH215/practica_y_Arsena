using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AgrochemApp.Models;
using Newtonsoft.Json;

namespace AgrochemApp.Services
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
            return JsonConvert.DeserializeObject<T>(content);
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseContent);
        }

        // =====================================================
        // АВТОРИЗАЦИЯ
        // =====================================================

        public async Task<LoginResponse> Login(string username, string password)
        {
            try
            {
                var result = await PostAsync<dynamic>("/api/Auth/login", new { username, password });

                if (result != null && result.token != null)
                {
                    return new LoginResponse
                    {
                        UserId = result.userId,
                        Username = result.username,
                        FullName = result.fullName,
                        Role = result.role,
                        Token = result.token
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка логина: {ex.Message}");
                return null;
            }
        }

        public async Task<dynamic> Register(RegisterRequest request)
        {
            return await PostAsync<dynamic>("/api/Auth/register", request);
        }

        // =====================================================
        // ПРОДУКЦИЯ
        // =====================================================

        public async Task<List<ProductModel>> GetProducts()
        {
            var result = await GetAsync<ApiResponse<List<ProductModel>>>("/api/Reference/products");
            if (result != null && result.success)
            {
                return result.data ?? new List<ProductModel>();
            }
            return new List<ProductModel>();
        }

        // =====================================================
        // РЕЦЕПТУРЫ
        // =====================================================

        public async Task<List<RecipeModel>> GetRecipes()
        {
            var result = await GetAsync<ApiResponse<List<RecipeModel>>>("/api/Recipes");
            if (result != null && result.success)
            {
                return result.data ?? new List<RecipeModel>();
            }
            return new List<RecipeModel>();
        }

        public async Task<RecipeModel> GetRecipe(int id)
        {
            var result = await GetAsync<ApiResponse<RecipeModel>>($"/api/Recipes/{id}");
            if (result != null && result.success)
            {
                return result.data;
            }
            return null;
        }

        public async Task<bool> CreateRecipe(CreateRecipeDto dto)
        {
            var result = await PostAsync<dynamic>("/api/Recipes", dto);
            return result?.success == true;
        }

        public async Task<bool> AddComponent(int recipeId, AddComponentDto dto)
        {
            var result = await PostAsync<dynamic>($"/api/Recipes/{recipeId}/components", dto);
            return result?.success == true;
        }

        public async Task<bool> ApproveRecipe(int recipeId, int userId)
        {
            var result = await PostAsync<dynamic>($"/api/Recipes/{recipeId}/approve", userId);
            return result?.success == true;
        }

        // =====================================================
        // ТЕХНОЛОГИЧЕСКИЕ КАРТЫ
        // =====================================================

        public async Task<List<TechCardModel>> GetTechCards()
        {
            var result = await GetAsync<ApiResponse<List<TechCardModel>>>("/api/TechCards");
            if (result != null && result.success)
            {
                return result.data ?? new List<TechCardModel>();
            }
            return new List<TechCardModel>();
        }

        public async Task<TechCardModel> GetTechCard(int id)
        {
            var result = await GetAsync<ApiResponse<TechCardModel>>($"/api/TechCards/{id}");
            if (result != null && result.success)
            {
                return result.data;
            }
            return null;
        }

        public async Task<bool> CreateTechCard(CreateTechCardDto dto)
        {
            var result = await PostAsync<dynamic>("/api/TechCards", dto);
            return result?.success == true;
        }

        public async Task<bool> AddStep(int techCardId, CreateTechStepDto dto)
        {
            var result = await PostAsync<dynamic>($"/api/TechCards/{techCardId}/steps", dto);
            return result?.success == true;
        }

        public async Task<bool> ApproveTechCard(int techCardId, int userId)
        {
            var result = await PostAsync<dynamic>($"/api/TechCards/{techCardId}/approve", userId);
            return result?.success == true;
        }

        // =====================================================
        // ПРОИЗВОДСТВЕННЫЕ ЗАКАЗЫ
        // =====================================================

        public async Task<List<ProductionOrderModel>> GetOrders()
        {
            var result = await GetAsync<ApiResponse<List<ProductionOrderModel>>>("/api/ProductionOrders");
            if (result != null && result.success)
            {
                return result.data ?? new List<ProductionOrderModel>();
            }
            return new List<ProductionOrderModel>();
        }

        public async Task<bool> CreateOrder(CreateOrderDto dto)
        {
            var result = await PostAsync<dynamic>("/api/ProductionOrders", dto);
            return result?.success == true;
        }

        public async Task<bool> CancelOrder(int orderId)
        {
            var result = await PostAsync<dynamic>($"/api/ProductionOrders/{orderId}/cancel", null);
            return result?.success == true;
        }
    }
}