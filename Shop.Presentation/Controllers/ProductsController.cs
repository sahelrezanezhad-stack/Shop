
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Products;
using Shop.Application.Products.DTOs;

namespace Shop.Presentation.Controllers;

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

        await _productService.AddAsync(dto);

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
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _productService.DeleteAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    // API Endpoints


    [HttpPost("/api/products")]
    public async Task<IActionResult> CreateApi(CreateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var product = await _productService.AddAsync(dto);

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

