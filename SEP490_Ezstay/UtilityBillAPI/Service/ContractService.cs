using Shared.DTOs.Contracts.Responses;
using UtilityBillAPI.Service.Interface;
using System.Net.Http.Headers; 

namespace UtilityBillAPI.Service
{
    public class ContractService : IContractService
    {
        private readonly HttpClient _httpClient;
        public ContractService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;

            var token = httpContextAccessor.HttpContext?.Request?.Headers["Authorization"].FirstOrDefault();            
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
            }
        }
        public async Task<ContractResponse?> GetContractAsync(Guid contractId)
        {
            var response = await _httpClient.GetAsync($"api/Contract/{contractId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ContractResponse>();
            }
            return null;
        }
    }
}
