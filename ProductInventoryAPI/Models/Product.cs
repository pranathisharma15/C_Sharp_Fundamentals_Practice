namespace ProductInventoryAPI.Models
{
    public class Product
    {
        public int Id { get; set; }

        // default initializers to avoid null issues when model binding
        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public double Price { get; set; }

        public int StockQuantity { get; set; }

        // convenience property to indicate availability
        public bool IsInStock => StockQuantity > 0;
    }
}
