using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductInventoryAPI.Data;
using ProductInventoryAPI.Models;
using System.Security.Claims;
using System.Linq;

namespace ProductInventoryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Task 4: All endpoints require a valid JWT by default
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/products
        // Optional query params:
        //  - category: filter by category (case-insensitive)
        //  - sort: "price_asc" | "price_desc"
        //  - inStock: true/false
        [Authorize(Roles = "Admin,Manager,Viewer")] // Task 7
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] string? category, [FromQuery] string? sort, [FromQuery] bool? inStock)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = category.Trim().ToLower();
                query = query.Where(p => p.Category != null && p.Category.ToLower() == cat);
            }

            if (inStock.HasValue)
            {
                query = inStock.Value
                    ? query.Where(p => p.StockQuantity > 0)
                    : query.Where(p => p.StockQuantity <= 0);
            }

            if (!string.IsNullOrWhiteSpace(sort))
            {
                switch (sort.Trim().ToLower())
                {
                    case "price_asc":
                        query = query.OrderBy(p => p.Price);
                        break;
                    case "price_desc":
                        query = query.OrderByDescending(p => p.Price);
                        break;
                }
            }

            var list = await query.ToListAsync();

            // Bonus: include caller info
            var calledBy = User.Identity?.Name;
            var callerRole = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new { data = list, calledBy, callerRole });
        }

        // GET: api/products/{id}
        [Authorize(Roles = "Admin,Manager,Viewer")] // Task 7
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound("Product not found");

            var calledBy = User.Identity?.Name;
            var callerRole = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new { data = product, calledBy, callerRole }); // Bonus
        }

        // POST: api/products
        [Authorize(Roles = "Admin,Manager")] // Task 6
        [HttpPost]
        public async Task<ActionResult<Product>> AddProduct([FromBody] Product product)
        {
            if (product.Price <= 0)
                return BadRequest("Price must be greater than 0");

            if (product.StockQuantity < 0)
                return BadRequest("StockQuantity cannot be negative");

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        // PUT: api/products/{id}
        [Authorize(Roles = "Admin,Manager")] // Task 6
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
        {
            if (id != product.Id)
                return BadRequest("ID mismatch");

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Products.AnyAsync(e => e.Id == id))
                    return NotFound("Product not found");

                throw;
            }

            return NoContent();
        }

        // DELETE: api/products/{id}
        [Authorize(Roles = "Admin")] // Task 5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound("Product not found");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}