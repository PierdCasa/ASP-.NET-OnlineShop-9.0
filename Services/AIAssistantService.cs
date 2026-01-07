using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;

namespace OnlineShop.Services
{
    public class AIAssistantService : IAIAssistantService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<AIAssistantService> _logger;
        private readonly ApplicationDbContext _context;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string ModelName = "gemini-2.5-flash-lite";

        public AIAssistantService(IConfiguration configuration, ILogger<AIAssistantService> logger, ApplicationDbContext context)
        {
            _httpClient = new HttpClient();
            _context = context;
            _apiKey = configuration["GoogleAI:ApiKey"]
                ?? throw new ArgumentNullException("GoogleAI:ApiKey nu este configurat în appsettings.json");

            _logger = logger;
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> GetProductAnswerAsync(int productId, string question)
        {
            var produs = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.FAQs)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (produs == null)
                return "Produsul nu a fost găsit.";

            var productContext = $"Produs: {produs.Title}\nCategorie: {produs.Category?.Name}\nPret: {produs.Price} RON\nStoc: {produs.Stock}\nDescriere: {produs.Description}";
            if (produs.Reviews != null && produs.Reviews.Any())
            {
                var avgRating = produs.Reviews.Where(r => r.Rating.HasValue).Average(r => r.Rating!.Value);
                var reviewCount = produs.Reviews.Count;
                productContext += $"\n\nReview-uri: {reviewCount} recenzii, rating mediu: {avgRating:F1}/5 stele";
                foreach (var review in produs.Reviews.Take(3))
                {
                    if (review.Rating.HasValue)
                        productContext += $"\n- {review.Rating} stele: {review.Text ?? "fără comentariu"}";
                }
            }
            else
            {
                productContext += "\n\nReview-uri: Niciun review încă.";
            }

            if (produs.FAQs != null && produs.FAQs.Any())
            {
                productContext += "\n\nFAQ:";
                foreach (var faq in produs.FAQs)
                    productContext += $"\nQ: {faq.Question}\nA: {faq.Answer}";
            }

            var answer = await GetAIResponseAsync(productContext, question);
            await SaveQuestionAsync(productId, question, answer);

            return answer;
        }

        public async Task<string> GetGeneralAnswerAsync(string question)
        {
            var produse = await _context.Products.Where(p => p.Status == ProductStatus.Aprobat).Take(5).ToListAsync();
            var productContext = "Produse disponibile:\n" + string.Join("\n", produse.Select(p => $"- {p.Title}: {p.Price} RON"));
            return await GetAIResponseAsync(productContext, question);
        }
        private async Task<string> GetAIResponseAsync(string context, string question)
        {
            try
            {
                var prompt = $@"Ești un asistent pentru un magazin online. Răspunde scurt și util în limba română.

Context despre produs:
{context}

Întrebarea clientului: {question}

Răspunde concis și la obiect:";
                var requestBody = new GoogleAiRequest
                {
                    Contents = new List<GoogleAiContent>
                    {
                        new GoogleAiContent
                        {
                            Parts = new List<GoogleAiPart>
                            {
                                new GoogleAiPart { Text = prompt }
                            }
                        }
                    },
                    GenerationConfig = new GoogleAiGenerationConfig
                    {
                        Temperature = 0.1,
                        MaxOutputTokens = 100
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var requestUrl = $"{BaseUrl}{ModelName}:generateContent?key={_apiKey}";

                _logger.LogInformation("Trimitem cererea de analiză către Google AI API");
                var response = await _httpClient.PostAsync(requestUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Eroare Google AI API: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return "Eroare la comunicarea cu AI.";
                }
                var googleResponse = JsonSerializer.Deserialize<GoogleAiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                var assistantMessage = googleResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrEmpty(assistantMessage))
                {
                    return "Nu am primit răspuns de la AI.";
                }

                _logger.LogInformation("Răspuns Google AI: {Response}", assistantMessage);
                var cleanedResponse = CleanJsonResponse(assistantMessage);

                return cleanedResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la comunicarea cu AI");
                return "Nu am informații despre asta.";
            }
        }
        private string CleanJsonResponse(string response)
        {
            var cleaned = response.Trim();
            if (cleaned.StartsWith("```json"))
            {
                cleaned = cleaned.Substring(7);
            }
            else if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Substring(3);
            }

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }

            return cleaned.Trim();
        }

        private async Task SaveQuestionAsync(int productId, string question, string answer)
        {
            try
            {
                var normalizedQuestion = question.Trim().ToLower();
                if (normalizedQuestion.Length < 10)
                    return;
                var excludedPhrases = new[] { "sigur", "ok", "da", "nu", "bine", "mersi", "mulțumesc", "buna", "salut", "hello", "test" };
                if (excludedPhrases.Any(p => normalizedQuestion == p || normalizedQuestion.StartsWith(p + " ") || normalizedQuestion.EndsWith(" " + p)))
                    return;
                
                var existingFaq = await _context.FAQs
                    .FirstOrDefaultAsync(f => f.ProductId == productId &&
                        f.Question.ToLower().Contains(normalizedQuestion.Substring(0, Math.Min(20, normalizedQuestion.Length))));

                if (existingFaq != null)
                {
                    existingFaq.TimesAsked++;
                    await _context.SaveChangesAsync();
                }
                else if (!string.IsNullOrEmpty(answer) && !answer.Contains("Eroare") && !answer.Contains("nu am"))
                {
                    _context.FAQs.Add(new FAQ
                    {
                        ProductId = productId,
                        Question = question.Trim(),
                        Answer = answer,
                        TimesAsked = 1,
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
            }
        }
    }
    public class GoogleAiRequest
    {
        [JsonPropertyName("contents")]
        public List<GoogleAiContent> Contents { get; set; } = new();

        [JsonPropertyName("generationConfig")]
        public GoogleAiGenerationConfig? GenerationConfig { get; set; }
    }
    public class GoogleAiContent
    {
        [JsonPropertyName("parts")]
        public List<GoogleAiPart> Parts { get; set; } = new();
    }
    public class GoogleAiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
    public class GoogleAiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 1024;
    }
    public class GoogleAiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GoogleAiCandidate>? Candidates { get; set; }
    }
    public class GoogleAiCandidate
    {
        [JsonPropertyName("content")]
        public GoogleAiContent? Content { get; set; }
    }
}
