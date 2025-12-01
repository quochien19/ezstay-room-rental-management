namespace PaymentAPI.DTOs.Responses;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid BillId { get; set; }
    public string TransactionId { get; set; }
    public decimal TransferAmount { get; set; }
    public string Content { get; set; }
    public string Description { get; set; }
    public string AccountNumber { get; set; }
    public string Gateway { get; set; } 
    public string TransferType { get; set; }
    public DateTime TransactionDate { get; set; }
}
