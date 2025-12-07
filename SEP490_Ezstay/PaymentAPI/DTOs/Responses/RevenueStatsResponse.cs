namespace PaymentAPI.DTOs.Responses;

public class RevenueStatsResponse
{
public decimal TotalRevenue { get; set; } // Tổng doanh thu
public int TotalTransactions { get; set; } // Tổng số giao dịch
public List<MonthlyRevenueStats> MonthlyStats { get; set; } = new(); // Thống kê theo tháng
}

public class MonthlyRevenueStats
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
}