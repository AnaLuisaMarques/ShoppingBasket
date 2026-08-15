namespace ShoppingBasket.Contracts.DTOs;

public class AddItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
