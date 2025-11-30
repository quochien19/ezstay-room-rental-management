using AutoMapper;
using AutoMapper.QueryableExtensions;
using UtilityBillAPI.DTO;
using UtilityBillAPI.Models;
using UtilityBillAPI.Repository.Interface;
using UtilityBillAPI.Service.Interface;
using Shared.DTOs;
using Shared.DTOs.UtilityReadings.Responses;
using Shared.Enums;
using MongoDB.Driver;

namespace UtilityBillAPI.Service
{
    public class UtilityBillService : IUtilityBillService
    {
        private readonly IUtilityBillRepository _utilityBillRepo;
        private readonly IUtilityReadingService _utilityReadingService;
        private readonly IContractService _contractService;
        private readonly IRoomInfoService _roomInfoService;
        private readonly IMapper _mapper;

        public UtilityBillService(IUtilityBillRepository utilityBillRepo, IMapper mapper,
            IContractService contractService, IUtilityReadingService utilityReadingService, 
            IRoomInfoService roomInforService)
        {
            _utilityBillRepo = utilityBillRepo;
            _mapper = mapper;
            _contractService = contractService;
            _utilityReadingService = utilityReadingService;
            _roomInfoService = roomInforService; 
        }

        public IQueryable<UtilityBillDTO> GetAll()
        {
            var bills = _utilityBillRepo.GetAll();
            return bills.ProjectTo<UtilityBillDTO>(_mapper.ConfigurationProvider);
        }

        public IQueryable<UtilityBillDTO> GetAllByOwnerId(Guid ownerId)
        {
            var bills = _utilityBillRepo.GetAllByOwner(ownerId).OrderByDescending(b => b.CreatedAt);

            return bills.ProjectTo<UtilityBillDTO>(_mapper.ConfigurationProvider);
        }

        public IQueryable<UtilityBillDTO> GetAllByTenantId(Guid tenantId)
        {
            var bills = _utilityBillRepo.GetAllByTenant(tenantId).Where(b => b.Status != UtilityBillStatus.Cancelled);
            return bills.ProjectTo<UtilityBillDTO>(_mapper.ConfigurationProvider);
        }

        public async Task<UtilityBillDTO?> GetByIdAsync(Guid id)
        {
            var bill = await _utilityBillRepo.GetByIdAsync(id);
            return bill == null ? throw new KeyNotFoundException("Bill not found!") : _mapper.Map<UtilityBillDTO>(bill);
        }

        public async Task<ApiResponse<UtilityBillDTO>> GenerateMonthlyBillAsync(Guid contractId, Guid ownerId)
        {
            // Check if spam bill
            //var recentBill = _utilityBillRepo.GetAll()
            //    .Where(b => b.ContractId == contractId && b.CreatedAt >= DateTime.UtcNow.AddMinutes(-1))
            //    .OrderByDescending(b => b.CreatedAt)
            //    .FirstOrDefault();

            //if (recentBill != null)
            //{
            //    return ApiResponse<UtilityBillDTO>.Fail("Please wait a few minutes before creating a new bill.");
            //}          

            var contract = await _contractService.GetContractAsync(contractId);
            if (contract == null)
                return ApiResponse<UtilityBillDTO>.Fail("Contract not found.");

            if (contract.ContractStatus != ContractStatus.Active)
            {
                string message = contract.ContractStatus switch
                {
                    ContractStatus.Pending => "Contract is pending approval. Cannot generate a bill.",
                    ContractStatus.Cancelled => "Contract is cancelled. Cannot generate a bill.",
                    ContractStatus.Expired => "Contract is expired. Cannot generate a bill.",
                    ContractStatus.Evicted => "Contract has been terminated. Cannot generate a bill.",
                    _ => "Contract is not valid for bill generation."
                };

                return ApiResponse<UtilityBillDTO>.Fail(message);
            }

            DateTime today = DateTime.UtcNow;
            int billingMonth = today.Month;
            int billingYear = today.Year;
            var electric = await _utilityReadingService.GetElectricityReadingAsync(contract.Id, billingMonth, billingYear);
            var water = await _utilityReadingService.GetWaterReadingAsync(contract.Id, billingMonth, billingYear);

            if (electric == null && water == null)
                return ApiResponse<UtilityBillDTO>.Fail($"Missing both electricity and water readings for {billingMonth}/{billingYear}.");

            if (electric == null)
                return ApiResponse<UtilityBillDTO>.Fail($"Missing electricity reading for {billingMonth}/{billingYear}.");

            if (water == null)
                return ApiResponse<UtilityBillDTO>.Fail($"Missing water reading for {billingMonth}/{billingYear}.");

            var readings = new[] { electric, water };

            var details = readings
               .Select(r => new UtilityBillDetailDTO
               {
                   Type = r.Type.ToString(),
                   UnitPrice = r.Price,
                   PreviousIndex = r.PreviousIndex,
                   CurrentIndex = r.CurrentIndex,
                   Consumption = r.Consumption,
                   Total = r.Total
               }).ToList();

            if (contract.ServiceInfors != null)
            {
                details.AddRange(
                    contract.ServiceInfors.Select(s => new UtilityBillDetailDTO
                    {
                        Type = "Service",
                        ServiceName = s.ServiceName,
                        ServicePrice = s.Price,
                        Total = s.Price
                    })
                );
            }

            var roomInfo = await _roomInfoService.GetRoomInfoAsync(contract.RoomId);

            var totalAmount = contract.RoomPrice + details.Sum(r => r.Total);

            var bill = new UtilityBillDTO
            {
                Id = Guid.NewGuid(),
                TenantId = contract.IdentityProfiles.FirstOrDefault(s => s.IsSigner == true && s.UserId != ownerId)?.UserId ?? Guid.Empty,
                OwnerId = ownerId,                
                ContractId = contract.Id,
                RoomName = roomInfo.RoomName,
                HouseName = roomInfo.HouseName,
                RoomPrice = contract.RoomPrice,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow,
                Status = UtilityBillStatus.Unpaid,
                BillType = UtilityBillType.Monthly,              
                Note = $"Monthly bill for {billingMonth}/{billingYear}",
                Details = details
            };

            await _utilityBillRepo.CreateAsync(_mapper.Map<UtilityBill>(bill));
            return ApiResponse<UtilityBillDTO>.Success(bill, "Monthly bill created successfully.");
        }

        public async Task<ApiResponse<UtilityBillDTO>> GenerateDepositBillAsync(Guid contractId, Guid ownerId)
        {
            var contract = await _contractService.GetContractAsync(contractId);
            if (contract == null)
                return ApiResponse<UtilityBillDTO>.Fail("Contract not found.");
            if (contract.ContractStatus != ContractStatus.Active)
            {
                string message = contract.ContractStatus switch
                {
                    ContractStatus.Pending => "Contract is pending approval. Cannot generate a bill.",
                    ContractStatus.Cancelled => "Contract is cancelled. Cannot generate a bill.",
                    ContractStatus.Expired => "Contract is expired. Cannot generate a bill.",
                    ContractStatus.Evicted => "Contract has been terminated. Cannot generate a bill.",
                    _ => "Contract is not valid for bill generation."
                };

                return ApiResponse<UtilityBillDTO>.Fail(message);
            }

            var existingDepositBill = _utilityBillRepo.GetAll()
                .FirstOrDefault(b => b.ContractId == contractId
                && b.BillType == UtilityBillType.Deposit
                && b.Status != UtilityBillStatus.Cancelled);

            if (existingDepositBill != null)
            {
                return ApiResponse<UtilityBillDTO>.Fail("A deposit bill for this contract already exists and cannot be created again.");
            }

            var roomInfo = await _roomInfoService.GetRoomInfoAsync(contract.RoomId);

            var bill = new UtilityBillDTO
            {
                Id = Guid.NewGuid(),
                TenantId = contract.IdentityProfiles.FirstOrDefault(s => s.IsSigner && s.UserId != ownerId)?.UserId ?? Guid.Empty,
                OwnerId = ownerId,                
                ContractId = contract.Id,
                RoomName = roomInfo.RoomName,
                HouseName = roomInfo.HouseName,                
                TotalAmount = contract.DepositAmount,
                CreatedAt = DateTime.UtcNow,
                Status = UtilityBillStatus.Unpaid,
                BillType = UtilityBillType.Deposit,
                Note = "Deposit payment",
                Details = new List<UtilityBillDetailDTO>
                {
                    new UtilityBillDetailDTO
                    {
                        Type = "Deposit",
                        Total = contract.DepositAmount,
                        ServiceName = "Deposit Fee",
                        ServicePrice = contract.DepositAmount
                    }
                }
            };

            await _utilityBillRepo.CreateAsync(_mapper.Map<UtilityBill>(bill));

            return ApiResponse<UtilityBillDTO>.Success(bill, "Deposit bill created successfully.");
        }


        public async Task<ApiResponse<bool>> MarkAsPaidAsync(Guid billId)
        {
            var bill = await _utilityBillRepo.GetByIdAsync(billId);
            if (bill.Status == UtilityBillStatus.Cancelled)
            {
                return ApiResponse<bool>.Fail("Unable to pay canceled invoice.");
            }

            await _utilityBillRepo.MarkAsPaidAsync(billId);
            return ApiResponse<bool>.Success(true, "Bill paid successfully!");
        }

        public async Task<ApiResponse<bool>> CancelAsync(Guid billId, string? reason)
        {
            var bill = await _utilityBillRepo.GetByIdAsync(billId);
            if (bill.Status == UtilityBillStatus.Paid)
            {
                return ApiResponse<bool>.Fail("Cannot cancel paid invoice.");
            }

            await _utilityBillRepo.CancelAsync(billId, reason);
            return ApiResponse<bool>.Success(true, "Bill canceled successfully!");
        }

    }


}
