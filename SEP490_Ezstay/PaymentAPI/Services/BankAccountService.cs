using System.Net.Http.Headers;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.Extensions.Options;
using PaymentAPI.Config;
using PaymentAPI.DTOs.Requests;
using PaymentAPI.Model;
using PaymentAPI.Repository.Interface;
using PaymentAPI.Services.Interfaces;
using Shared.DTOs;
using Shared.DTOs.Payments.Responses;

namespace PaymentAPI.Services;

public class BankAccountService:IBankAccountService
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IMapper _mapper;
    private readonly IBankGatewayRepository _bankGatewayRepository;
  

    public BankAccountService(IBankAccountRepository bankAccountRepository, IMapper mapper, IBankGatewayRepository bankGatewayRepository)
    {
        _bankAccountRepository = bankAccountRepository;
        _mapper = mapper;
        _bankGatewayRepository = bankGatewayRepository;
      
    }

    public async Task<BankAccountResponse> GetById(Guid id)
    {
        var bankAccount=   await _bankAccountRepository.GetById(id); 
     
        var bankGateway = await _bankGatewayRepository.GetById(bankAccount.BankGatewayId);
     
        var result = _mapper.Map<BankAccountResponse>(bankAccount);
        result.BankGateway = _mapper.Map<BankGatewayResponse>(bankGateway);
        return result;
    }

    public IQueryable<BankAccountResponse> GetAll(Guid userId)
    {
        var bankAccount = _bankAccountRepository.GetAll(userId);
        return bankAccount.ProjectTo<BankAccountResponse>(_mapper.ConfigurationProvider);
    }
    public IQueryable<BankAccountResponse> GetBankAccountsWithAmount(Guid ownerId, decimal amount, string? description)
    {
        
        
        var bankAccounts = _bankAccountRepository.GetDefaultByUserId(ownerId).ToList();
        var result= bankAccounts.Select(acc => new BankAccountResponse
        {
            UserId = acc.UserId,
            BankGateway = _mapper.Map<BankGatewayResponse>(acc.BankGatewayId),
            AccountNumber = acc.AccountNumber,
            Amount = amount, 
            Description = description, 
            ImageQR = $"https://qr.sepay.vn/img?acc={acc.AccountNumber}&bank={_bankGatewayRepository.GetById(acc.BankGatewayId).Result.BankName}&amount={amount}&des={description}",
            CreatedAt = acc.CreatedAt,
            UpdatedAt = acc.UpdatedAt
        });
        return result.AsQueryable();
    }

    public IQueryable<BankAccountResponse> GetDefaultByUserId(Guid userId)
    {
        var bankAccount = _bankAccountRepository.GetDefaultByUserId(userId);
        // foreach (var c in bankAccount)
        // {
        //  c.ImageQR = $"https://qr.sepay.vn/img?acc={c.AccountNumber}&bank={c.BankName}";   
        // }
        return bankAccount.ProjectTo<BankAccountResponse>(_mapper.ConfigurationProvider);
    }
    public async Task<ApiResponse<BankAccountResponse>> AddBankAccount(Guid userId, CreateBankAccount request)
    {
        bool existed = await _bankAccountRepository.CheckExistsBankAccount(userId, request.BankGatewayId, request.AccountNumber);
        if (existed)
        {
            return ApiResponse<BankAccountResponse>.Fail("Bank account already exists");
        }
        var encodedDes = Uri.EscapeDataString(request.Description ?? "");
        var bankGateway = await _bankGatewayRepository.GetById(request.BankGatewayId);
        if (bankGateway == null) 
        {
            return ApiResponse<BankAccountResponse>.Fail("Bank Gateway not found"); // Trả lỗi rõ ràng
        }
         var bankAccount = _mapper.Map<BankAccount>(request);
        bankAccount.ImageQR = $"https://qr.sepay.vn/img?acc={request.AccountNumber}&bank={bankGateway.BankName}&des={encodedDes}";
        bankAccount.UserId = userId;
        bankAccount.CreatedAt = DateTime.UtcNow;
        bankAccount.AccountNumber = request.AccountNumber.Trim();
        await _bankAccountRepository.Add(bankAccount);
        return ApiResponse<BankAccountResponse>.Success(_mapper.Map<BankAccountResponse>(bankAccount), "Bank Account created");
    }
    public async Task<ApiResponse<bool>> UpdateBankAccount(Guid id, UpdateBankAccount request)
    {
        var bankAccount = await _bankAccountRepository.GetById(id);
        var bankGateway = await _bankGatewayRepository.GetById(request.BankGatewayId);
       
        bool isDuplicated = await _bankAccountRepository.CheckExistsBankAccount(bankAccount.UserId, request.BankGatewayId, request.AccountNumber);
        
        if (isDuplicated && (request.BankGatewayId != bankAccount.BankGatewayId || request.AccountNumber != bankAccount.AccountNumber))
        {
            return ApiResponse<bool>.Fail("Bank account already exists");
        } 
        var encodedDes = Uri.EscapeDataString(request.Description ?? "");
        var qrUrl = $"https://qr.sepay.vn/img?acc={request.AccountNumber}&bank={bankGateway.BankName}&des={encodedDes}"; 
        _mapper.Map(request, bankAccount);
        bankAccount.ImageQR = qrUrl;
        bankAccount.UpdatedAt = DateTime.UtcNow;
        await _bankAccountRepository.Update(bankAccount);
      
        return ApiResponse<bool>.Success(true, "Update Bank Account Successfully");
    }

    public async Task<ApiResponse<bool>> DeleteBankAccount(Guid id)
    {
        var bankAccount =await _bankAccountRepository.GetById(id);
        if (bankAccount == null)
            throw new KeyNotFoundException("BankAccount not found");
        await _bankAccountRepository.Delete(bankAccount);
        return ApiResponse<bool>.Success(true, "Delete Successfully");
    }

    // public async Task<JsonElement?> GetTransactionsAsync()
    // {
    //     var req = new HttpRequestMessage(HttpMethod.Get, "https://my.sepay.vn/userapi/transactions/list");
    //     req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SecretKey);
    //
    //     var resp = await _http.SendAsync(req);
    //     if (!resp.IsSuccessStatusCode)
    //     {
    //         var err = await resp.Content.ReadAsStringAsync();
    //         throw new Exception($"SePay API lỗi: {resp.StatusCode} - {err}");
    //     }
    //
    //     var json = await resp.Content.ReadAsStringAsync();
    //     return JsonSerializer.Deserialize<JsonElement>(json);
    // }
    
  
}