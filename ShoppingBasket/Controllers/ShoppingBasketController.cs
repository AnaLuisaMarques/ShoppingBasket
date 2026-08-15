using Microsoft.AspNetCore.Mvc;
using ShoppingBasket.API.Interfaces;

namespace ShoppingBasket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShoppingBasketController : ControllerBase
{
    private readonly IShoppingBasketService _service;

    public ShoppingBasketController(IShoppingBasketService service)
    {
        _service = service;
    }
        
}
