using JACO.CR.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace JACO.CR.Web.Data;

public sealed class CrDbContext(DbContextOptions<CrDbContext> options) : DbContext(options)
{
    public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();
    public DbSet<CRLookupValue> CRLookupValues => Set<CRLookupValue>();
    public DbSet<CRAttachment> CRAttachments => Set<CRAttachment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ChangeRequest>().ToTable("ChangeRequests");
        b.Entity<ChangeRequest>().Property(x => x.CRNumber)
            .HasComputedColumnSql("RIGHT(REPLICATE('0',10) + CONVERT(varchar(10), [CRId]), 10)", stored: true);
        b.Entity<ChangeRequest>().HasIndex(x => x.CRId).IsUnique();
        b.Entity<ChangeRequest>().HasIndex(x => x.SAPReferenceId);

        b.Entity<ChangeRequest>().Property(x => x.CreatedOnDate).HasColumnType("date");
        b.Entity<ChangeRequest>().Property(x => x.CreatedOnTime).HasColumnType("time(0)");
        b.Entity<ChangeRequest>().Property(x => x.LastUpdateDate).HasColumnType("date");
        b.Entity<ChangeRequest>().Property(x => x.LastUpdateOn).HasColumnType("time(0)");

        b.Entity<CRLookupValue>().ToTable("CRLookupValues");
        b.Entity<CRLookupValue>().HasIndex(x => new { x.LookupType, x.Value }).IsUnique();
        b.Entity<CRAttachment>().ToTable("CRAttachments");
        b.Entity<CRAttachment>().HasIndex(x => x.ChangeRequestId);
    }
}
