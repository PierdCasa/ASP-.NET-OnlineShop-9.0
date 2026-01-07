using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;
using System.Text.Json;

namespace OnlineShop.Data
{
    public class CategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class ProductDto
    {
        public string SeedId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int Status { get; set; } = 1;
        public string? ImagePath { get; set; }
    }

    public class SeedData
    {
        public List<CategoryDto> Categories { get; set; } = new();
        public List<ProductDto> Products { get; set; } = new();
    }

    public class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { "Admin", "Collaborator", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var adminUser = new ApplicationUser { UserName = "admin@onlineshop.com", Email = "admin@onlineshop.com", FirstName = "Admin", LastName = "Shop" };
            if (await userManager.FindByEmailAsync(adminUser.Email) == null)
            {
                await userManager.CreateAsync(adminUser, "Admin123!");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            var collaboratorUser = new ApplicationUser { UserName = "colaborator@onlineshop.com", Email = "colaborator@onlineshop.com", FirstName = "Collab", LastName = "Orator" };
            if (await userManager.FindByEmailAsync(collaboratorUser.Email) == null)
            {
                await userManager.CreateAsync(collaboratorUser, "Collab123!");
                await userManager.AddToRoleAsync(collaboratorUser, "Collaborator");
            }

            var normalUser = new ApplicationUser { UserName = "user@onlineshop.com", Email = "user@onlineshop.com", FirstName = "Ion", LastName = "Popescu" };
            if (await userManager.FindByEmailAsync(normalUser.Email) == null)
            {
                await userManager.CreateAsync(normalUser, "User123!");
                await userManager.AddToRoleAsync(normalUser, "User");
            }

            await SeedProductsAsync(context);
        }

        private static async Task SeedProductsAsync(ApplicationDbContext context)
        {
            Console.WriteLine("=== SeedProductsAsync started ===");
            
            var hasCategories = await context.Categories.AnyAsync();
            var hasProducts = await context.Products.AnyAsync();
            
            Console.WriteLine($"Existing categories: {hasCategories}, Existing products: {hasProducts}");

            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "seed_data.json");
            Console.WriteLine($"Looking for seed file at: {jsonPath}");
            
            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"Seed file not found at: {jsonPath}");
                return;
            }
            
            Console.WriteLine("Seed file found, reading...");

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var seedData = JsonSerializer.Deserialize<SeedData>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (seedData == null)
                return;

            var categoryMap = new Dictionary<string, int>();
            foreach (var catDto in seedData.Categories)
            {
                var existingCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == catDto.Name);
                if (existingCategory != null)
                {
                    Console.WriteLine($"Category '{catDto.Name}' already exists with ID {existingCategory.Id}");
                    categoryMap[catDto.Name] = existingCategory.Id;
                }
                else
                {
                    var category = new Category
                    {
                        Name = catDto.Name,
                        Description = catDto.Description
                    };
                    context.Categories.Add(category);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"Created category '{catDto.Name}' with ID {category.Id}");
                    categoryMap[catDto.Name] = category.Id;
                }
            }

            var deletedSeedIds = await context.DeletedSeeds.Select(d => d.SeedId).ToListAsync();
            Console.WriteLine($"Found {deletedSeedIds.Count} deleted seed IDs to skip");

            int addedCount = 0;
            int skippedDeletedCount = 0;
            int skippedCount = 0;
            foreach (var prodDto in seedData.Products)
            {
                if (string.IsNullOrEmpty(prodDto.SeedId))
                {
                    Console.WriteLine($"Skipping product '{prodDto.Title}' - no SeedId specified");
                    skippedCount++;
                    continue;
                }

                if (deletedSeedIds.Contains(prodDto.SeedId))
                {
                    Console.WriteLine($"Skipping product '{prodDto.Title}' with SeedId '{prodDto.SeedId}' - was intentionally deleted");
                    skippedDeletedCount++;
                    continue;
                }

                if (!categoryMap.TryGetValue(prodDto.CategoryName, out var categoryId))
                {
                    Console.WriteLine($"Category '{prodDto.CategoryName}' not found for product '{prodDto.Title}'");
                    continue;
                }

                var existingProduct = await context.Products.FirstOrDefaultAsync(p => p.SeedId == prodDto.SeedId);
                
                if (existingProduct != null)
                {
                    Console.WriteLine($"Product with SeedId '{prodDto.SeedId}' already exists ('{existingProduct.Title}') - preserving user edits");
                }
                else
                {
                    var product = new Product
                    {
                        SeedId = prodDto.SeedId,
                        Title = prodDto.Title,
                        Description = prodDto.Description,
                        Price = prodDto.Price,
                        Stock = prodDto.Stock,
                        CategoryId = categoryId,
                        Status = (ProductStatus)prodDto.Status,
                        ImagePath = prodDto.ImagePath,
                        CreatedAt = DateTime.Now
                    };
                    context.Products.Add(product);
                    Console.WriteLine($"Added new product '{prodDto.Title}' with SeedId '{prodDto.SeedId}'");
                    addedCount++;
                }
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"Seeding complete: {addedCount} added, {skippedCount} skipped (no SeedId), {skippedDeletedCount} skipped (intentionally deleted).");
        }
    }
}
