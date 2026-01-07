using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;
using OnlineShop.Services;

namespace OnlineShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIAssistantService _aiService;

        public ProductsController(ApplicationDbContext context, IAIAssistantService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId, string? sortOrder, int page = 1)
        {
            const int pageSize = 9;

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Where(p => p.Status == ProductStatus.Aprobat);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(search) || p.Description.ToLower().Contains(search));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "rating_asc" => query.OrderBy(p => p.Rating ?? 0),
                "rating_desc" => query.OrderByDescending(p => p.Rating ?? 0),
                "name_asc" => query.OrderBy(p => p.Title),
                "name_desc" => query.OrderByDescending(p => p.Title),
                _ => query.OrderByDescending(p => p.Id)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(products);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews!).ThenInclude(r => r.User)
                .Include(p => p.FAQs)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == ProductStatus.Aprobat);

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> AskAI(int productId, string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return Json(new { success = false, answer = "Te rugăm să introduci o întrebare." });

            try
            {
                var answer = await _aiService.GetProductAnswerAsync(productId, question);
                return Json(new { success = true, answer });
            }
            catch
            {
                return Json(new { success = false, answer = "A apărut o eroare. Te rugăm să încerci din nou." });
            }
        }
    }
}
