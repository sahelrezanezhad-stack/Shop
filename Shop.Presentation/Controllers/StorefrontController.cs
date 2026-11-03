using Shop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Products;
using Shop.Application.ShoppingCarts;

namespace Shop.Presentation.Controllers;

/// <summary>Read-only SALLY catalog presentation. Customer-facing copy stays in English even if legacy DB descriptions are Persian.</summary>
[ApiController]
[Route("api/storefront/products")]
public sealed class StorefrontController : ControllerBase
{
    private readonly IProductService _products;
    private readonly ISizeStockRepository _sizeStock;

    public StorefrontController(IProductService products, ISizeStockRepository sizeStock)
        => (_products, _sizeStock) = (products, sizeStock);

    private sealed record ProductPresentation(
        string Slug, string Name, string Type, string Color, string Description,
        string Material, string FrontImage, string BackImage, string[] Sizes);

    private static readonly ProductPresentation[] Collection =
    {
        new("label-jacket", "LABEL PATCH WORK JACKET", "WASHED WORKWEAR", "FADED CHARCOAL",
            "Washed work jacket with oversized pockets, scattered color patches, a classic collar and a button front.",
            "Heavy washed cotton canvas with reinforced stitching and decorative patches (visual estimate).", "label-jacket-front.png", "label-jacket-back.png", new[] {"S", "M", "L", "XL"}),
        new("suede-jacket", "CAMEL SUEDE JACKET", "SUEDE OUTERWEAR", "CAMEL",
            "Camel-toned jacket with a classic collar, button cuffs and a ribbed hem.",
            "Suede-look outer shell with a fabric lining; natural or synthetic suede is unconfirmed (visual estimate).", "suede-jacket.png", "suede-jacket-back.png", new[] {"S", "M", "L", "XL"}),
        new("black-puffer", "TEXTURED BLACK PUFFER", "PUFFER OUTERWEAR", "DEEP BLACK",
            "Voluminous black puffer jacket with a large hood, raised texture and a relaxed silhouette.",
            "Textured soft shell, padded insulation and a lightweight lining (visual estimate).", "black-puffer-front.png", "black-puffer-back.png", new[] {"S", "M", "L", "XL"}),
        new("wash-hoodie", "WASHED GREY HOODIE", "PRINTED HOODIE", "GREY / WHITE",
            "Washed grey hoodie with an irregular white graphic, drawstring hood and kangaroo pocket.",
            "Heavy cotton-blend sweatshirt fleece with a washed finish and printed graphic (visual estimate).", "wash-hoodie.png", "wash-hoodie-back.png", new[] {"S", "M", "L", "XL"}),
        new("floral-overshirt", "REVERSIBLE SHREDDED SEAM DROP NECK SHIRT", "PRINTED SHIRT", "BLACK MULTICOLOR",
            "Reversible printed shirt with a dropped shoulder, frayed edges, pointed collar, long sleeves and a button front.",
            "100% Italian printed cotton, as supplied in the product specification.", "floral-overshirt.png", "floral-overshirt-back.png", new[] {"XS", "S", "M", "L"}),
        new("camp-knit", "COUNTRYSIDE STORY KNIT", "ILLUSTRATED KNITWEAR", "ECRU",
            "Ecru illustrated knit featuring a countryside scene with trees, animals and a house; brown ribbed trim.",
            "Multicolor jacquard knit; exact fiber composition is unconfirmed (visual estimate).", "camp-knit.png", "camp-knit-back.png", new[] {"S", "M", "L", "XL"}),
        new("utility-vest", "GREY UTILITY VEST", "UTILITY VEST", "SLATE GREY",
            "Slate grey utility vest with a layered front, functional side pockets and structured seams.",
            "Durable technical fabric with a lightweight lining (visual estimate).", "vest-front.png", "vest-back.png", new[] {"S", "M", "L", "XL"}),
        new("stone-sweat", "GREY CREWNECK SWEATSHIRT", "CREWNECK SWEATSHIRT", "LIGHT GREY",
            "Light grey crewneck sweatshirt with a small zip pocket on the chest and a sleeve brand patch.",
            "Heavy cotton-blend sweatshirt fleece with a soft texture and ribbed trim (visual estimate).", "stone-sweat-front.png", "stone-sweat-back.png", new[] {"S", "M", "L", "XL"})
    };

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var stored = await _products.GetAllAsync();
        var availability = await _sizeStock.ListAsync();
        var byName = stored.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var result = Collection
            .Where(view => byName.ContainsKey(view.Name))
            .Select(view =>
            {
                var db = byName[view.Name];
                return new
                {
                    id = db.Id,
                    slug = view.Slug,
                    name = view.Name,
                    type = view.Type,
                    description = view.Description,
                    priceToman = db.Price,
                    stockQuantity = db.StockQuantity,
                    sizeStock = view.Sizes.ToDictionary(size => size,
                        size => availability.FirstOrDefault(x => x.ProductId == db.Id && x.Size == size)?.Quantity ?? 0),
                    color = view.Color,
                    material = view.Material,
                    sizes = view.Sizes,
                    frontImage = $"/images/collection/{view.FrontImage}",
                    backImage = $"/images/collection/{view.BackImage}",
                    sideImage = $"/images/hanging/{view.Slug}-side.png",
                    photographs = new {
                        front = $"/images/hanging/{view.Slug}-front.png",
                        side = $"/images/hanging/{view.Slug}-side.png",
                        back = $"/images/hanging/{view.Slug}-back.png"
                    }
                };
            }).ToList();
        return Ok(result);
    }
}
