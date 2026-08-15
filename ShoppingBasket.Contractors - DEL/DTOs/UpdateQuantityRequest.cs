namespace ShoppingBasket.Contracts.DTOs;

public class UpdateQuantityRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
