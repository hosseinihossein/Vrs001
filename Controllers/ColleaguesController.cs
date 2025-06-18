using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Controllers;

public class ColleaguesController : Controller
{
    readonly UserManager<MyIdentityUser> userManager;
    readonly Account_Process accountProcess;
    public ColleaguesController(UserManager<MyIdentityUser> userManager, Account_Process accountProcess)
    {
        this.userManager = userManager;
        this.accountProcess = accountProcess;
    }

    public async Task<IActionResult> Index(string? branch, int page = 1)
    {
        List<MyIdentityUser> allSelectedUsers;
        if (branch == null)
        {
            allSelectedUsers = await userManager.Users.ToListAsync();
        }
        else
        {
            allSelectedUsers = await userManager.Users
                .Where(u => u.Branch == branch)
                .ToListAsync();
        }

        if (allSelectedUsers.Any(u => u.UserGuid == "admin"))
        {
            allSelectedUsers.Remove(allSelectedUsers.Single(u => u.UserGuid == "admin"));
        }

        var orderedUsers = accountProcess.OrderUsersByPost(allSelectedUsers);

        //***** sorts 10 colleagues per page *****
        ViewBag.LastPage = (int)Math.Ceiling(orderedUsers.Count / 10.0);
        var usersList = orderedUsers.Skip((page - 1) * 10).Take(10).ToList();

        return View(usersList);
    }
}