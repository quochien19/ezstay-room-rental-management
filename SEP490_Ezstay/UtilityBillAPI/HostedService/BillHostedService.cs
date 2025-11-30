using UtilityBillAPI.Service.Interface;

namespace UtilityBillAPI.HostedService
{
    public class BillHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BillHostedService> _logger;

        public BillHostedService(
            IServiceProvider serviceProvider,
            ILogger<BillHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    /*//using var scope = _serviceProvider.CreateScope();
                    //var settingRepo = scope.ServiceProvider.GetRequiredService<IUtilityBillService>(); //IBillSettingRepository
                    //var utilityBillService = scope.ServiceProvider.GetRequiredService<IUtilityBillService>();

                    //var settings = await settingRepo.GetAllAsync();

                    //foreach (var setting in settings)
                    //{
                    //    // Kiểm tra xem có được phép tạo tự động không
                    //    if (setting.IsAutoGenerateEnabled && DateTime.Now.Day == setting.GenerateDay)
                    //    {
                    //        try
                    //        {
                    //            var dueDate = DateTime.Now.AddDays(setting.DueAfterDays);
                    //            var result = await utilityBillService.GenerateBillAsync(setting.OwnerId, dueDate);

                    //            if (!result.IsSuccess)
                    //            {
                    //                _logger.LogWarning("Không thể tạo hóa đơn cho chủ trọ {OwnerId}: {Message}",
                    //                    setting.OwnerId, result.Message);
                    //            }
                    //            else
                    //            {
                    //                _logger.LogInformation("Đã tạo hóa đơn thành công cho chủ trọ {OwnerId}",
                    //                    setting.OwnerId);
                    //            }
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            _logger.LogError(ex, "Lỗi khi tạo hóa đơn cho chủ trọ {OwnerId}", setting.OwnerId);
                    //        }
                    //    }
                    //}

                    //// Chờ đến lần chạy tiếp theo (24 giờ)
                    //await Task.Delay(TimeSpan.FromHours(24), stoppingToken);*/
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi trong quá trình xử lý tạo hóa đơn tự động");
                    // Chờ 1 giờ trước khi thử lại nếu có lỗi
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }
    }
}
