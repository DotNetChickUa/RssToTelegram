using Microsoft.EntityFrameworkCore;

sealed class TelegramSessionDbContext(DbContextOptions<TelegramSessionDbContext> options) : DbContext(options)
{
    internal DbSet<TelegramSessionEntity> Sessions => Set<TelegramSessionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelegramSessionEntity>(entity =>
        {
            entity.ToTable("telegram_sessions");
            entity.HasKey(x => x.Token);
            entity.Property(x => x.Token).HasMaxLength(64);
            entity.Property(x => x.AppId).IsRequired();
            entity.Property(x => x.AppHash).IsRequired();
            entity.Property(x => x.Phone).IsRequired();
            entity.Property(x => x.OtpCode);
            entity.Property(x => x.Password);
            entity.Property(x => x.RssConfigsJson);
            entity.Property(x => x.LastPublishedAtUtc);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
        });
    }
}