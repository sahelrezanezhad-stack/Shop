
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Products;
using Shop.Application.Products.DTOs;
using Shop.Domain.Entities;

namespace Shop.Presentation.Controllers;

public class ProductsController : Controller
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
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
}

