namespace Shared.DTOs.Payments.Responses;

public class BankAccountResponse
{
    public Guid Id { get; set; } 
    public Guid UserId { get; set; }
    public BankGatewayResponse BankGateway { get; set; }
    public string AccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
    public string ImageQR { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}