namespace ShoppingBasket.Contracts.DTOs;

public class BasketDto
{
    public string Id { get; set; } = string.Empty;
    public List<BasketItemDto> Items { get; set; } = new();
}
