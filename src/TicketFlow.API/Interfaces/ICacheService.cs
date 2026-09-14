namespace TicketFlow.API.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key); //it is user for caching unique info from application
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null); 
        Task RemoveAsync(string key);
    }
}
