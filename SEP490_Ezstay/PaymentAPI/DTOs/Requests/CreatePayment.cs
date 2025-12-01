namespace PaymentAPI.DTOs.Requests;

public class CreatePayment
{
    public string Gateway { get; set; }
    
    public string AccountNumber { get; set; }
    
    public string Content { get; set; } 
    public decimal TransferAmount { get; set; }
    
    // Cần dùng: ID giao dịch (cần cho việc kiểm tra trùng lặp)
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public long IdNumber { get; set; }
    
    [System.Text.Json.Serialization.JsonIgnore]
    public string Id => IdNumber.ToString();
    
    [System.Text.Json.Serialization.JsonIgnore]
    public string TransactionId => Id;
}