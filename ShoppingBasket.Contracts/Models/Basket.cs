namespace ShoppingBasket.Contracts.Models;

public class Basket
{
    public Guid Id { get; set; } = Guid.Empty;
    public List<BasketItem> Items { get; set; } = new();
}
