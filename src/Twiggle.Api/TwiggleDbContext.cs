using Microsoft.EntityFrameworkCore;
namespace Twiggle.Api;
public sealed class TwiggleDbContext(DbContextOptions<TwiggleDbContext> options) : DbContext(options)
{
    public DbSet<InterviewSession> Sessions => Set<InterviewSession>();
    public DbSet<InterviewTurn> Turns => Set<InterviewTurn>();
    public DbSet<ExperienceClaim> Claims => Set<ExperienceClaim>();
    public DbSet<ExperienceEpisode> Episodes => Set<ExperienceEpisode>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<InterviewSession>().Property(x => x.Revision).IsConcurrencyToken();
        model.Entity<InterviewSession>().Property(x => x.Title).HasMaxLength(160);
        model.Entity<InterviewSession>().HasIndex(x => x.WorkspaceId);
        model.Entity<InterviewSession>().HasMany(x => x.Turns).WithOne(x => x.Session).HasForeignKey(x => x.SessionId);
        model.Entity<InterviewSession>().HasMany(x => x.Claims).WithOne(x => x.Session).HasForeignKey(x => x.SessionId);
        model.Entity<InterviewSession>().HasOne(x => x.Episode).WithOne(x => x.Session).HasForeignKey<ExperienceEpisode>(x => x.SessionId);
        model.Entity<ExperienceClaim>().HasOne(x => x.SourceTurn).WithMany().HasForeignKey(x => x.SourceTurnId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ExperienceClaim>().HasMany(x => x.Revisions).WithOne(x => x.Claim).HasForeignKey(x => x.ClaimId);
    }
}
