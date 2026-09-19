using AshaNandanvan.Application.Options;
using AshaNandanvan.Application.Orders;
using AshaNandanvan.Application.Payments;
using AshaNandanvan.Application.Products;
using AshaNandanvan.Infrastructure.Data;
using AshaNandanvan.Infrastructure.Identity;
using AshaNandanvan.Infrastructure.Orders;
using AshaNandanvan.Infrastructure.Payments;
using AshaNandanvan.Infrastructure.Products;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AshaNandanvan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<StripePaymentProvider>();
        services.AddScoped<MockPaymentProvider>();
        services.AddScoped<IPaymentProvider>(sp =>
        {
            var payment = sp.GetRequiredService<IOptions<PaymentOptions>>().Value;
            var useStripe = payment.Provider.Equals("Stripe", StringComparison.OrdinalIgnoreCase)
                && payment.Stripe.IsConfigured;
            return useStripe
                ? sp.GetRequiredService<StripePaymentProvider>()
                : sp.GetRequiredService<MockPaymentProvider>();
        });

        return services;
    }
}
