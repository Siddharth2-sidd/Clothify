using Clothify_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace Clothify_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }

        // CHECKOUT
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return BadRequest("Cart is empty");

            decimal total = 0;

            var order = new Order
            {
                UserId = userId,
                Status = "Pending",
                Items = new List<OrderItem>()
            };

            foreach (var item in cart.Items)
            {
                if (item.Product.Stock < item.Quantity)
                    return BadRequest($"Not enough stock for {item.Product.Name}");

                total += item.Product.Price * item.Quantity;

                item.Product.Stock -= item.Quantity;

                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price
                });
            }

            order.TotalAmount = total;

            _context.Orders.Add(order);

            // Clear cart after checkout
            _context.CartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync();

            return Ok(order);
        }

        // ORDER HISTORY
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var userId = GetUserId();

            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .ToListAsync();

            return Ok(orders);
        }
    }
}
