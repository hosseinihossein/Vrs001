using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Models;

public class MyIdentityUser : IdentityUser<int>
{
    public string UserGuid { get; set; } = string.Empty;
    public string PasswordLiteral { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Post { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 0;
    public bool InActive { get; set; } = false;

    public string NotificationsCommaSeparated { get; set; } = string.Empty;
    [NotMapped]
    public List<string> Notifications
    {
        get => [.. NotificationsCommaSeparated.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        set => NotificationsCommaSeparated = "," + string.Join(",", value) + ",";
    }

    public string PerformanceFieldCommaSeparated { get; set; } = string.Empty;
    [NotMapped]
    public List<string> PerformanceField
    {
        get => [.. PerformanceFieldCommaSeparated.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        set => PerformanceFieldCommaSeparated = "," + string.Join(",", value) + ",";
    }

    public string SubUsersGuidsCommaSeparated { get; set; } = string.Empty;
    [NotMapped]
    public List<string> SubUsersGuids
    {
        get => [.. SubUsersGuidsCommaSeparated.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        set => SubUsersGuidsCommaSeparated = "," + string.Join(",", value) + ",";
    }
    public string? MainUserGuid { get; set; }
    //public List<MyIdentityUser> SubUsers { get; set; } = [];
    //public int? MainUserId { get; set; }
    //public MyIdentityUser? MainUser { get; set; }
}

public class MyIdentityRole : IdentityRole<int>
{
    public MyIdentityRole() : base() { }
    public MyIdentityRole(string roleName) : base(roleName) { }
    public string Description { get; set; } = string.Empty;
}

public class IdentityDb : IdentityDbContext<MyIdentityUser, MyIdentityRole, int>
{
    public IdentityDb(DbContextOptions<IdentityDb> options) : base(options) { }

    /*protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        //***** One-to-Many, mainUser to subUsers *****
        builder.Entity<MyIdentityUser>()
        .HasMany<MyIdentityUser>(u => u.SubUsers)
        .WithOne(u => u.MainUser)
        .HasForeignKey(u => u.MainUserId)
        .IsRequired(false);
    }*/
}

/*public class MyEmailTokenProvider : AuthenticatorTokenProvider<MyIdentityUser>
{
    public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<MyIdentityUser> userManager, MyIdentityUser user)
    {
        return base.CanGenerateTwoFactorTokenAsync(userManager, user);
    }
    public override async Task<string> GenerateAsync(string purpose, UserManager<MyIdentityUser> userManager, MyIdentityUser user)
    {
        //string token = string.Empty;
        string code = new Random().Next(1000, 99999).ToString();
        user.EmailValidationCode = code;
        user.EmailValidationDate = DateTime.Now;
        IdentityResult result = await userManager.UpdateAsync(user);
        string token = result.Succeeded ? code : string.Empty;
        return token;
    }
    public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<MyIdentityUser> userManager, MyIdentityUser user)
    {
        bool isValid = false;
        if (token == user.EmailValidationCode)
        {
            isValid = true;
            user.EmailValidationCode = null;
            user.EmailValidationDate = null;
            await userManager.UpdateAsync(user);
        }
        return isValid;
    }
}*/

