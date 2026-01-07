namespace OnlineShop.Services
{
    public interface IAIAssistantService
    {
        Task<string> GetProductAnswerAsync(int productId, string userQuestion);
        Task<string> GetGeneralAnswerAsync(string userQuestion);
    }
}
