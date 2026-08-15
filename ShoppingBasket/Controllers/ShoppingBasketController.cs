using Microsoft.AspNetCore.Mvc;
using ShoppingBasket.API.Interfaces;

namespace ShoppingBasket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShoppingBasketController : ControllerBase
{
    private readonly IShoppingBasketService _shoppingBasketService;

    public ShoppingBasketController(IShoppingBasketService service)
    {
        _shoppingBasketService = service;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return BadRequest("Invalid id");

        var basket = await _shoppingBasketService.GetBasketAsync(id);

        if (basket == null) return NotFound();

        return Ok(basket);
    }
}
