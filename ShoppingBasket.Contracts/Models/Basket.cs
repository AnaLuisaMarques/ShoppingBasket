namespace ShoppingBasket.Contracts.Models;

public class Basket
{
    public string Id { get; set; } = string.Empty;
    public List<BasketItem> Items { get; set; } = new();
}
