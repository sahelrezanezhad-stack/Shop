namespace Shop.Application.ShoppingCarts;

/// <summary>The eight approved SALLY items. Images live only in wwwroot; no simulated stock.</summary>
public static class SallyCatalog
{
    public sealed record Item(string Slug, string Name, string FrontImage, string[] Sizes);
    public static readonly Item[] Items =
    {
        new("label-jacket", "LABEL PATCH WORK JACKET", "label-jacket-front.png", new[]{"S","M","L","XL"}),
        new("suede-jacket", "CAMEL SUEDE JACKET", "suede-jacket.png", new[]{"S","M","L","XL"}),
        new("black-puffer", "TEXTURED BLACK PUFFER", "black-puffer-front.png", new[]{"S","M","L","XL"}),
        new("wash-hoodie", "WASHED GREY HOODIE", "wash-hoodie.png", new[]{"S","M","L","XL"}),
        new("floral-overshirt", "REVERSIBLE SHREDDED SEAM DROP NECK SHIRT", "floral-overshirt.png", new[]{"XS","S","M","L"}),
        new("camp-knit", "COUNTRYSIDE STORY KNIT", "camp-knit.png", new[]{"S","M","L","XL"}),
        new("utility-vest", "GREY UTILITY VEST", "vest-front.png", new[]{"S","M","L","XL"}),
        new("stone-sweat", "GREY CREWNECK SWEATSHIRT", "stone-sweat-front.png", new[]{"S","M","L","XL"})
    };
    public static Item? Find(string productName) => Items.FirstOrDefault(p =>
        string.Equals(p.Name, productName, StringComparison.OrdinalIgnoreCase));
}
