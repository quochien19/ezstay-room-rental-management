using System.Text.Json;
using PaymentAPI.DTOs.Requests;
using Shared.DTOs;
using Shared.DTOs.Payments.Responses;

namespace PaymentAPI.Services.Interfaces;

public interface IBankAccountService
{
  Task<BankAccountResponse> GetById(Guid id);
  IQueryable<BankAccountResponse> GetAll(Guid userId);
  
  // IQueryable<BankAccountResponse> GetBankAccountsWithAmount(Guid ownerId, Guid billId, decimal amount, string? description);
  IQueryable<BankAccountResponse> GetBankAccountsWithAmount(Guid ownerId, decimal amount, string? description);

  IQueryable<BankAccountResponse> GetDefaultByUserId(Guid userId);
  Task<ApiResponse<BankAccountResponse>> AddBankAccount(Guid userId, CreateBankAccount request);
  Task<ApiResponse<bool>> UpdateBankAccount(Guid id,UpdateBankAccount request);
  Task<ApiResponse<bool>> DeleteBankAccount(Guid id);

  //Task<JsonElement?> GetTransactionsAsync();
  
 // Task<List<BankAccountResponse>> GetBankAccountsWithAmount(Guid ownerId, Guid billId, decimal amount, string? description = null);

}