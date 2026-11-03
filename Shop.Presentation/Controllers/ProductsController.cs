
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Products;
using Shop.Application.Products.DTOs;
using Microsoft.AspNetCore.Authorization;
using Shop.Presentation.Security;

namespace Shop.Presentation.Controllers;

[Authorize(Policy = SallySecurity.AdminPolicy)]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var products = string.IsNullOrWhiteSpace(search)
            ? await _productService.GetAllAsync()
            : await _productService.SearchByNameAsync(search);

        ViewBag.Search = search;

        return View(products);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateProductDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try { await _productService.AddAsync(dto); }
        catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);return View(dto);}

        return RedirectToAction(nameof(Index));
    }










    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        var dto = new UpdateProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        };

        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            await _productService.UpdateAsync(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);return View(dto);}

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }
    [HttpPost]
    public IActionResult DeleteConfirmed(int id)
    {
        // Legacy Product -> OrderItem may cascade in the existing SQL schema.
        // Do not delete historical order data or silently alter that foreign key.
        return Conflict("Product deletion is disabled to preserve order history. Set size inventory to zero to stop sales.");
    }

    // API Endpoints


    [HttpPost("/api/products")]
    public async Task<IActionResult> CreateApi([FromBody] CreateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        ProductDto product;
        try { product = await _productService.AddAsync(dto); }
        catch(InvalidOperationException ex){return BadRequest(new { message=ex.Message });}

        return StatusCode(201, new
        {
            message = "Product created successfully.",
            id = product.Id
        });
    }


    [HttpGet("/api/products")]
    public async Task<ActionResult<List<ProductDto>>> GetAllApi()
    {
        var products = await _productService.GetAllAsync();

        return Ok(products);
    }

    [HttpGet("/api/products/{id:int}")]
    public async Task<ActionResult<ProductDto>> GetByIdApi(int id)
    {
        if (id <= 0)
            return BadRequest();

        var product = await _productService.GetByIdAsync(id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

}

