using Microsoft.AspNetCore.Mvc;
using Shop.Application.Products;
using Shop.Application.Products.DTOs;

namespace Shop.Presentation.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsApiController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsApiController(ProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            var product = await _productService.AddAsync(dto);

            return StatusCode(201, new
            {
                message = "Product created successfully.",
                id = product.Id
            });
        }

        [HttpGet]
        public async Task<ActionResult<List<ProductDto>>> GetAll()
        {
            var products = await _productService.GetAllAsync();

            return Ok(products);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            if (id <= 0)
                return BadRequest();

            var product = await _productService.GetByIdAsync(id);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

    }
}
