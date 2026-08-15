using Microsoft.AspNetCore.Mvc;
using ShoppingBasket.API.Interfaces;
using ShoppingBasket.Contracts.DTOs;

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
    public async Task<IActionResult> GetShoppingBasketById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return BadRequest("Invalid id");

        var basket = await _shoppingBasketService.GetBasketAsync(id);

        if (basket == null) return NotFound();

        return Ok(basket);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShoppingBasket([FromBody] BasketDto basket)
    {
        if (basket == null) return BadRequest("Basket payload is required");

        var created = await _shoppingBasketService.CreateBasketAsync(basket);

        return CreatedAtAction(nameof(GetShoppingBasketById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteShoppingBasket(Guid id)
    {
        if (id == Guid.Empty) return BadRequest("Invalid id");

        var deleted = await _shoppingBasketService.DeleteBasketAsync(id);

        if (!deleted) return NotFound();

        return NoContent();
    }

    [HttpPost("{id:guid}/addProducts")]
    public async Task<IActionResult> AddItemsToBasket(Guid id, [FromBody] IEnumerable<AddItemRequest> items)
    {
        if (id == Guid.Empty) return BadRequest("Invalid id");
        if (items == null || !items.Any()) return BadRequest("Items payload is required");

        var updated = await _shoppingBasketService.AddItemsAsync(id, items);

        if (updated == null) return NotFound();

        return Ok(updated);
    }

    [HttpPut("{id:guid}/updateProducts")]
    public async Task<IActionResult> UpdateItemsInBasket(Guid id, [FromBody] IEnumerable<AddItemRequest> items)
    {
        if (id == Guid.Empty) return BadRequest("Invalid id");
        if (items == null || !items.Any()) return BadRequest("Items payload is required");

        var updated = await _shoppingBasketService.UpdateItemsAsync(id, items);

        if (updated == null) return NotFound();

        return Ok(updated);
    }
}
