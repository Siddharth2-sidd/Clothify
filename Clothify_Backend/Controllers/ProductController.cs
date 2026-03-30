using Clothify_Backend.Models;
using Clothify_Backend.DTO.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace Clothify_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;

        public ProductController(AppDbContext context, IWebHostEnvironment env, IMemoryCache cache)
        {
            _context = context;
            _env = env;
            _cache = cache;

        }

        // CREATE PRODUCT (Admin/Seller)
        [Authorize(Roles = "Admin,Seller")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateProductDTO dto, IFormFile image)
        {
            string imagePath = null;

            if (image != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "images");
                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(folder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);

                imagePath = "/images/" + fileName;
            }

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                CategoryId = dto.CategoryId,
                ImageUrl = imagePath
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }

        // (Pagination + Filtering)
        [HttpGet]
    public async Task<IActionResult> GetAll(
    int page = 1,
    int pageSize = 10,
    string search = "",
    int? categoryId = null)
        {
            // ✅ Unique cache key based on filters
            var cacheKey = $"products_{page}_{pageSize}_{search}_{categoryId}";

            if (!_cache.TryGetValue(cacheKey, out object cachedResult))
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(p => p.Name.Contains(search));

                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId);

                var total = await query.CountAsync();

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    Data = products
                };

                // ✅ Store in cache
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

                return Ok(result);
            }

            // ✅ Return cached data
            return Ok(cachedResult);
        }

        //GET BY ID
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            return Ok(product);
        }

        // UPDATE
        [Authorize(Roles = "Admin,Seller")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateProductDTO dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;

            await _context.SaveChangesAsync();

            return Ok(product);
        }

        //DELETE
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok("Deleted successfully");
        }
    }
}
