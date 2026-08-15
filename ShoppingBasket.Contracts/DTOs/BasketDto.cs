namespace ShoppingBasket.Contracts.DTOs;

public class BasketDto
{
    public Guid Id { get; set; } = Guid.Empty;
    public List<BasketItemDto> Items { get; set; } = new();
}
