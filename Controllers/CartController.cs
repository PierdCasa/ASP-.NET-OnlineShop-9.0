using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public CartController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        public async Task<IActionResult> Index()
        {
            var uid = _users.GetUserId(User);
            var items = await _db.CartItems.Include(c => c.Product).Where(c => c.UserId == uid).ToListAsync();
            ViewBag.Total = items.Sum(c => c.Subtotal);
            return View(items);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var uid = _users.GetUserId(User);
            var prod = await _db.Products.FindAsync(productId);

            if (prod == null || prod.Status != ProductStatus.Aprobat)
            {
                TempData["Error"] = "Produs negasit.";
                return RedirectToAction("Index", "Products");
            }
            if (prod.Stock < quantity)
            {
                TempData["Error"] = "Stoc insuficient.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == uid && c.ProductId == productId);
            if (existing != null)
            {
                if (existing.Quantity + quantity > prod.Stock)
                {
                    TempData["Error"] = "Depaseste stocul.";
                    return RedirectToAction("Details", "Products", new { id = productId });
                }
                existing.Quantity += quantity;
            }
            else
            {
                _db.CartItems.Add(new CartItem { UserId = uid!, ProductId = productId, Quantity = quantity });
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = "Adaugat in cos!";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var uid = _users.GetUserId(User);
            var item = await _db.CartItems.Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == uid);
            if (item == null) return NotFound();

            if (quantity <= 0)
                _db.CartItems.Remove(item);
            else if (quantity > item.Product!.Stock)
            {
                TempData["Error"] = "Stoc insuficient.";
                return RedirectToAction("Index");
            }
            else
                item.Quantity = quantity;

            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var uid = _users.GetUserId(User);
            var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == uid);
            if (item != null)
            {
                _db.CartItems.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Sters din cos.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string? shippingAddress)
        {
            var uid = _users.GetUserId(User);
            var items = await _db.CartItems.Include(c => c.Product).Where(c => c.UserId == uid).ToListAsync();

            if (!items.Any())
            {
                TempData["Error"] = "Cosul e gol.";
                return RedirectToAction("Index");
            }

            foreach (var item in items)
            {
                if (item.Product!.Stock < item.Quantity)
                {
                    TempData["Error"] = $"Stoc insuficient: {item.Product.Title}";
                    return RedirectToAction("Index");
                }
            }

            var order = new Order { UserId = uid!, ShippingAddress = shippingAddress };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var item in items)
            {
                _db.OrderItems.Add(new OrderItem { OrderId = order.Id, ProductId = item.ProductId, Quantity = item.Quantity, UnitPrice = item.Product!.Price });
                item.Product.Stock -= item.Quantity;
            }

            order.TotalAmount = items.Sum(c => c.Quantity * c.Product!.Price);
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Comanda plasata!";
            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
        }

        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var uid = _users.GetUserId(User);
            var order = await _db.Orders.Include(o => o.OrderItems!).ThenInclude(oi => oi.Product).FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == uid);
            return order == null ? NotFound() : View(order);
        }
    }
}
