
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Products;
using Shop.Application.Products.DTOs;
using Shop.Domain.Entities;

namespace Shop.Presentation.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllEntitiesAsync();
        return View(products);
    }
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid)
        {
            return View(product);
        }

        await _productService.AddAsync(new CreateProductDto
        {
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        });

        return RedirectToAction(nameof(Index));
    }










    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetEntityByIdAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Product product)
    {
        if (!ModelState.IsValid)
        {
            return View(product);
        }

        var existingProduct = await _productService.GetEntityByIdAsync(product.Id);
        if (existingProduct == null)
        {
            return NotFound();
        }

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.StockQuantity = product.StockQuantity;

        await _productService.UpdateAsync(existingProduct);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _productService.GetEntityByIdAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }
    [HttpPost]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _productService.GetEntityByIdAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        await _productService.DeleteAsync(product);

        return RedirectToAction(nameof(Index));
    }

    // API Endpoints

    [HttpPost("/api/products")]
    public async Task<IActionResult> CreateApi(CreateProductDto dto)
    {
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

