using JACO.CR.Web.Data;
using JACO.CR.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace JACO.CR.Web.Services;

public sealed class CRLookupService(CrDbContext db)
{
    public async Task<IReadOnlyList<CRLookupValue>> GetAsync(string type) =>
        await db.CRLookupValues
            .Where(x => x.LookupType == type && x.Active)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayText)
            .ToListAsync();

    public Task<bool> IsAllowedAsync(string type, string value) =>
        db.CRLookupValues.AnyAsync(x =>
            x.LookupType == type &&
            x.Value == value &&
            x.Active);
}
