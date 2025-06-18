using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace App.Models;

public class LoginModel
{
    public string? ReturnUrl { get; set; } = string.Empty;
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool? IsPersistent { get; set; } = false;
}

public class SignupModel
{
    [StringLength(40)]
    public string Username { get; set; } = string.Empty;

    /*[StringLength(50)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;*/

    [StringLength(40)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password))]
    public string RepeatPassword { get; set; } = string.Empty;

    [StringLength(40)]
    [Unicode]
    public string FullName { get; set; } = string.Empty;

    [StringLength(40)]
    [Unicode]
    public string Branch { get; set; } = string.Empty;
    //public SelectList BranchOptions { get; set; } = null!;

    [StringLength(40)]
    [Unicode]
    public string Post { get; set; } = string.Empty;
}

public class ChangePasswordForm
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string RepeatNewPassword { get; set; } = string.Empty;
}

public class Administration_UsersListModel(MyIdentityUser user, List<string> roles)
{
    public MyIdentityUser MyIdentityUser { get; set; } = user;
    public List<string> Roles { get; set; } = roles;
    public List<MyIdentityUser> SubUsers { get; set; } = [];
    public MyIdentityUser? MainUser { get; set; }
}

public class Administration_RolesListModel(string role, string description, int numberOfUsers)
{
    public string Role { get; set; } = role;
    public string Description { get; set; } = description;
    public int NumberOfMembers { get; set; } = numberOfUsers;
}

public class Administration_AddNewRoleModel
{
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class Administration_EditRoleModel
{
    public string RoleName { get; set; } = string.Empty;
    public List<MyIdentityUser> Members { get; set; } = new();
    public List<MyIdentityUser> NotMembers { get; set; } = new();
    public List<string> Notices { get; set; } = [];
}

public class Account_Profile
{
    public MyIdentityUser MyUser { get; set; } = null!;
    public int TotalScore { get; set; }
    public double AverageScore { get; set; }
}

public class Account_UserSeedModel
{
    public string UserName { get; set; } = string.Empty;
    public string UserGuid { get; set; } = string.Empty;
    public string PasswordLiteral { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Post { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool InActive { get; set; } = false;
    public string[] PerformanceField { get; set; } = [];
    public string[] Roles { get; set; } = [];
    public string[] SubUsersGuids { get; set; } = [];
    public string? MainUserGuid { get; set; }
}

public class Account_Process
{
    readonly IConfiguration configuration;
    readonly UserManager<MyIdentityUser> userManager;
    readonly IWebHostEnvironment env;
    readonly DirectoryInfo SeedDirectoryInfo;
    public Account_Process(IConfiguration configuration, UserManager<MyIdentityUser> userManager,
    IWebHostEnvironment env)
    {
        this.configuration = configuration;
        this.userManager = userManager;
        this.env = env;
        SeedDirectoryInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Storage", "Account", "DbSeed"));
    }
    public List<MyIdentityUser> OrderUsersByPost(List<MyIdentityUser> users)
    {
        var postPreferences = configuration.GetSection("PostPreferences").Get<Dictionary<int, string>>() ?? [];
        var orderKeys = postPreferences.Keys.Order().ToList();

        List<MyIdentityUser> orderedUsers = [];
        foreach (int order in orderKeys)
        {
            var bunch = users.Where(u => u.Post.Contains(postPreferences[order]));
            foreach (var user in bunch)
            {
                if (!orderedUsers.Any(u => u.UserGuid == user.UserGuid))
                {
                    orderedUsers.Add(user);
                }
            }
        }

        foreach (var user in users)
        {
            if (!orderedUsers.Any(u => u.UserGuid == user.UserGuid))
            {
                orderedUsers.Add(user);
            }
        }

        return orderedUsers;
    }

    //************** seed ***************
    public async Task UpdateUserSeed(MyIdentityUser user)
    {
        Account_UserSeedModel userSeedModel = new()
        {
            Branch = user.Branch,
            Description = user.Description,
            FullName = user.FullName,
            InActive = user.InActive,
            PasswordLiteral = user.PasswordLiteral,
            PerformanceField = user.PerformanceField.ToArray(),
            Post = user.Post,
            UserGuid = user.UserGuid,
            UserName = user.UserName!,
            Roles = [.. await userManager.GetRolesAsync(user)],
            SubUsersGuids = [.. user.SubUsersGuids],
            MainUserGuid = user.MainUserGuid
        };

        string json = JsonSerializer.Serialize(userSeedModel);
        string userSeedPath = Path.Combine(SeedDirectoryInfo.FullName, user.UserGuid);

        await File.WriteAllTextAsync(userSeedPath, json);
    }

    public void DeleteUserSeed(MyIdentityUser user)
    {
        string userSeedPath = Path.Combine(SeedDirectoryInfo.FullName, user.UserGuid);
        File.Delete(userSeedPath);
    }

    public async Task SeedDb()
    {
        foreach (var fileInfo in SeedDirectoryInfo.EnumerateFiles())
        {
            var myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == fileInfo.Name);
            if (myUser != null) continue;

            string json = await File.ReadAllTextAsync(fileInfo.FullName);
            Account_UserSeedModel? userSeedModel;
            try
            {
                userSeedModel = JsonSerializer.Deserialize<Account_UserSeedModel>(json);
            }
            catch
            {
                //log
                continue;
            }
            if (userSeedModel is not null)
            {
                myUser = new MyIdentityUser()
                {
                    Branch = userSeedModel.Branch,
                    Description = userSeedModel.Description,
                    FullName = userSeedModel.FullName,
                    InActive = userSeedModel.InActive,
                    PasswordLiteral = userSeedModel.PasswordLiteral,
                    PerformanceField = userSeedModel.PerformanceField.ToList(),
                    Post = userSeedModel.Post,
                    UserGuid = userSeedModel.UserGuid,
                    UserName = userSeedModel.UserName!,
                    SubUsersGuids = [.. userSeedModel.SubUsersGuids],
                    MainUserGuid = userSeedModel.MainUserGuid
                };
                IdentityResult result = await userManager.CreateAsync(myUser, myUser.PasswordLiteral);
                if (!result.Succeeded)
                {
                    //log
                    continue;
                }

                foreach (string roleName in userSeedModel.Roles)
                {
                    await userManager.AddToRoleAsync(myUser, roleName);
                }
            }
        }
    }
    //************************************

    public async Task<List<MyIdentityUser>> GetAllRelatedUsers(MyIdentityUser? user)
    {
        List<MyIdentityUser> allRelatedUsers = [];
        if (user == null)
        {
            return allRelatedUsers;
        }

        if (user.MainUserGuid != null)
        {
            var mainUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == user.MainUserGuid);
            if (mainUser != null)
            {
                allRelatedUsers.Add(mainUser);
                foreach (string subUserGuid in mainUser.SubUsersGuids)
                {
                    var subUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == subUserGuid);
                    if (subUser != null)
                    {
                        allRelatedUsers.Add(subUser);
                    }
                }
            }
        }
        else if (user.SubUsersGuids.Count > 0)
        {
            allRelatedUsers.Add(user);
            foreach (string subUserGuid in user.SubUsersGuids)
            {
                var subUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == subUserGuid);
                if (subUser != null)
                {
                    allRelatedUsers.Add(subUser);
                }
            }
        }

        return allRelatedUsers;
    }
}