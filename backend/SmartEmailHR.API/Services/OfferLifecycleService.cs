using Microsoft.EntityFrameworkCore;
using SmartEmailHR.API.Configuration;
using SmartEmailHR.API.Data;

namespace SmartEmailHR.API.Services;

public sealed class OfferLifecycleService : IOfferLifecycleService
{
    private readonly AppDbContext _dbContext;

    public OfferLifecycleService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> UpdateExpiredOffersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.Date;
        var offresActives = await _dbContext.Offres
            .Where(o => o.Statut == OffreStatuts.Active && o.DateExpiration.Date < now)
            .ToListAsync(cancellationToken);

        foreach (var offre in offresActives)
        {
            offre.Statut = OffreStatuts.Expiree;
        }

        if (offresActives.Count == 0)
        {
            return 0;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return offresActives.Count;
    }
}

