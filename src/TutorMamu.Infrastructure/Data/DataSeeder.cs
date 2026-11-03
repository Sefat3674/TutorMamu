using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorMamu.Domain.Entities;


namespace TutorMamu.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { Roles.Admin, Roles.Teacher, Roles.Student })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role));
        }

        var adminEmail = configuration["SeedAdmin:Email"];
        var adminPassword = configuration["SeedAdmin:Password"];
        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword)
            && await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FullName = "Platform Admin", EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        if (!await db.Topics.AnyAsync())
        {
            var math = new Topic { Name = "Mathematics", NameBn = "গণিত", Slug = "mathematics" };
            var calculus = new Topic { Name = "Calculus", NameBn = "ক্যালকুলাস", Slug = "calculus", Parent = math };
            var integration = new Topic { Name = "Integration", NameBn = "সমাকলন", Slug = "integration", Parent = calculus };
            var physics = new Topic { Name = "Physics", NameBn = "পদার্থবিজ্ঞান", Slug = "physics" };
            var english = new Topic { Name = "English", NameBn = "ইংরেজি", Slug = "english" };
            db.Topics.AddRange(math, calculus, integration, physics, english);
            await db.SaveChangesAsync();
        }
    }
}
