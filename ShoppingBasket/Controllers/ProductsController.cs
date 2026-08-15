using Microsoft.AspNetCore.Mvc;
using ShoppingBasket.API.Interfaces;

namespace ShoppingBasket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductCatalogClient _productCatalog;

    public ProductsController(IProductCatalogClient catalog)
    {
        _productCatalog = catalog;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var product = await _productCatalog.GetProductAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpGet("top100")]
    public async Task<IActionResult> GetTop100Products()
    {
        var products = await _productCatalog.GetTop100ProductsAsync();
        return Ok(products);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
    {
        if (pageNumber < 1) return BadRequest("pageNumber must be >= 1");
        if (pageSize < 1) return BadRequest("pageSize must be >= 1");
        if (pageSize > 1000) return BadRequest("pageSize cannot exceed 1000");

        var (items, totalCount) = await _productCatalog.GetProductsAsync(pageNumber, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return Ok(new
        {
            items,
            pageNumber,
            pageSize,
            totalCount,
            totalPages
        });
    }
}
