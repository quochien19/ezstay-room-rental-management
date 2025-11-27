using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using PaymentAPI.DTOs.Requests;
using PaymentAPI.Services.Interfaces;
using Shared.DTOs.Payments.Responses;

namespace PaymentAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BankController : ControllerBase
    {
        private readonly IBankAccountService _bankAccountService;
        private readonly ITokenService _tokenService;
        private readonly IBankGatewayService _bankGatewayService;

        public BankController(IBankAccountService bankAccountService, ITokenService tokenService, IBankGatewayService bankGatewayService)
        {
            _bankAccountService = bankAccountService;
            _tokenService = tokenService;
            _bankGatewayService = bankGatewayService;
        }


        // [HttpPost("sync")]
        // public async Task<IActionResult> LoadBankGateway()
        // {
        //     var result = await _bankGatewayService.SyncFromVietQR();
        //     return Ok(result);
        // }
        [HttpPut("gateway/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HideBankGateway(Guid id,[FromQuery]  bool isActive)
        {
            return Ok(await _bankGatewayService.HiddenBankGateway(id, isActive));
        }
        [HttpGet("gateway/{id}")]
        public async Task<IActionResult> GetGatewayById(Guid id)
        {
            return Ok(await _bankGatewayService.GetById(id));
        }
        [HttpGet("gateway")]
        [EnableQuery]
        public IQueryable<BankGatewayResponse> GetAllBankGateway()
        {
            return  _bankGatewayService.GetAllBankGateway();
        }
        
        
        [HttpPost("bank-account")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> Add([FromBody] CreateBankAccount request)
        {
            var userId = _tokenService.GetUserIdFromClaims(User);
            var result = await _bankAccountService.AddBankAccount(userId, request);
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result);
        }
        
        [Authorize(Roles = "Admin, Owner")]
        [HttpPut("bank-account/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBankAccount request)
        {
            var result = await _bankAccountService.UpdateBankAccount(id,request);
            if (!result.IsSuccess)
                return BadRequest();
            return NoContent();
        }
        [Authorize(Roles = "Admin, Owner")]
        [HttpDelete("bank-account/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _bankAccountService.DeleteBankAccount(id);
            return Ok(result);
        }
        
        
      
      
        
        [HttpGet("bank-account/all-by-user")]
        [EnableQuery]
        [Authorize(Roles = "Admin, Owner")]
        public IQueryable<BankAccountResponse> GetAll()
        {
            var userId = _tokenService.GetUserIdFromClaims(User);
          return   _bankAccountService.GetAll(userId);
        }
       
        // [HttpGet("transactions")]
        // public async Task<IActionResult> GetTransactions()
        // {
        //     var result = await _bankAccountService.GetTransactionsAsync();
        //     return Ok(result);
        // }
        // [HttpGet("owner/{ownerId}")]
        // [EnableQuery]
        // [Authorize(Roles = "User, Owner, Admin")]
        // public IQueryable<BankAccountResponse> GetByOwnerId(Guid ownerId)
        // {
        //     return _bankAccountService.GetAll(ownerId);
        // }
        
        // chauw vẽ sequence
        
        [HttpGet("gateway/active")]
        [EnableQuery]
        public IQueryable<BankGatewayResponse> GetAllBankGatewayActive()
        {
            return _bankGatewayService.GetAllActiveBankGateway();
        }
        [HttpGet("bank-account/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _bankAccountService.GetById(id);
            return Ok(result);
        }
        [HttpGet("{ownerId}/getDefault")]
        [EnableQuery]
        public IQueryable<BankAccountResponse> GetAllD(Guid ownerId)
        {
            return  _bankAccountService.GetDefaultByUserId(ownerId);
        }
        
        [HttpGet("bank-account/owner/{ownerId}/active")]
        [EnableQuery]
        [Authorize(Roles = "User, Owner, Admin")]
        public IQueryable<BankAccountResponse> GetByOwnerIdForBill(Guid ownerId, [FromQuery] decimal amount, [FromQuery] string? description)
        {
            return _bankAccountService.GetBankAccountsWithAmount(ownerId, amount, description);
        }
        
       //  [HttpGet("owner/{ownerId}/bill/{billId}")]
       //  [EnableQuery]
       // // [Authorize(Roles = "User, Owner, Admin")]
       // public IQueryable<BankAccountResponse> GetByOwnerIdForBill(Guid ownerId, Guid billId, [FromQuery] decimal amount, [FromQuery] string? description)
       //  {
       //    return _bankAccountService.GetBankAccountsWithAmount(ownerId, billId, amount, description);
       //  }
    }
}
