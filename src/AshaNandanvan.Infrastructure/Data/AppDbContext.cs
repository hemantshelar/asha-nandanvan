using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSlot> ProductSlots => Set<ProductSlot>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<DogSittingSettings> DogSittingSettings => Set<DogSittingSettings>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Slug).IsUnique();
            entity.Property(p => p.Name).HasMaxLength(160).IsRequired();
            entity.Property(p => p.Slug).HasMaxLength(180).IsRequired();
            entity.Property(p => p.Unit).HasMaxLength(40).IsRequired();
            entity.Property(p => p.Price).HasColumnType("decimal(10,2)");
            entity.Property(p => p.ImagePath).HasMaxLength(260);
            entity.HasMany(p => p.Slots)
                .WithOne(s => s.Product)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProductSlot>(entity =>
        {
            entity.Property(s => s.Label).HasMaxLength(160);
            entity.HasIndex(s => new { s.ProductId, s.StartsAt });
        });

        builder.Entity<Cart>(entity =>
        {
            entity.HasIndex(c => c.UserId);
            entity.HasMany(c => c.Items)
                .WithOne(i => i.Cart)
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(i => new { i.CartId, i.ProductId })
                .IsUnique()
                .HasFilter("[ProductSlotId] IS NULL");
            entity.HasIndex(i => new { i.CartId, i.ProductId, i.ProductSlotId })
                .IsUnique()
                .HasFilter("[ProductSlotId] IS NOT NULL");
            entity.HasOne(i => i.ProductSlot)
                .WithMany()
                .HasForeignKey(i => i.ProductSlotId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(i => i.PetName).HasMaxLength(80);
        });

        builder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.Property(o => o.OrderNumber).HasMaxLength(40).IsRequired();
            entity.Property(o => o.CustomerName).HasMaxLength(160).IsRequired();
            entity.Property(o => o.CustomerEmail).HasMaxLength(256).IsRequired();
            entity.Property(o => o.PickupWindow).HasMaxLength(80).IsRequired();
            entity.Property(o => o.PaymentProvider).HasMaxLength(40).IsRequired();
            entity.Property(o => o.Total).HasColumnType("decimal(10,2)");
            entity.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.Property(i => i.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(i => i.Unit).HasMaxLength(40).IsRequired();
            entity.Property(i => i.UnitPrice).HasColumnType("decimal(10,2)");
            entity.Property(i => i.SlotLabel).HasMaxLength(160);
            entity.HasOne(i => i.ProductSlot)
                .WithMany()
                .HasForeignKey(i => i.ProductSlotId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(i => i.PetName).HasMaxLength(80);
        });

        builder.Entity<DogSittingSettings>(entity =>
        {
            entity.Property(s => s.Headline).HasMaxLength(160).IsRequired();
            entity.Property(s => s.Description).IsRequired();
            entity.Property(s => s.TermsAndConditions).IsRequired();
        });

        builder.Entity<MediaItem>(entity =>
        {
            entity.Property(m => m.OfferSlug).HasMaxLength(80).IsRequired();
            entity.Property(m => m.Title).HasMaxLength(160).IsRequired();
            entity.Property(m => m.SourceUrl).HasMaxLength(500).IsRequired();
            entity.Property(m => m.YouTubeVideoId).HasMaxLength(20).IsRequired();
            entity.HasIndex(m => new { m.IsPublished, m.OfferSlug, m.SortOrder });
            entity.HasIndex(m => new { m.OfferSlug, m.YouTubeVideoId }).IsUnique();
        });
    }
}
