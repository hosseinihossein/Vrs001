using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Models;

namespace App.Components;

public class NavBarComponent(UserManager<MyIdentityUser> userManager, Account_Process accountProcess,
Downloads_Info downloads_Info, Regulation_Info regulation_Info) : ViewComponent
{

    public async Task<IViewComponentResult> InvokeAsync()
    {
        MyIdentityUser? user;

        if (User.Identity is null || !User.Identity.IsAuthenticated)
        {
            user = null;
        }
        else
        {
            user = await userManager.FindByNameAsync(User.Identity.Name!);
            ViewBag.AllRelatedUsers = (await accountProcess.GetAllRelatedUsers(user))
            .Where(u => u.InActive == false)
            .ToList();
        }

        ViewBag.DownloadCategories = downloads_Info.Categories;
        ViewBag.RegulationCategories = regulation_Info.Categories;
        return View(user);
    }
}