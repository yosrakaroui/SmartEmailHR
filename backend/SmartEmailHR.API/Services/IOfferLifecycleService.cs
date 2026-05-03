namespace SmartEmailHR.API.Services;

public interface IOfferLifecycleService
{
    Task<int> UpdateExpiredOffersAsync(CancellationToken cancellationToken = default);
}

