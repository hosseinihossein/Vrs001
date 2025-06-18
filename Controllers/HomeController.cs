using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Models;
using Microsoft.AspNetCore.Authorization;

namespace App.Controllers;

[AutoValidateAntiforgeryToken]
public class HomeController(IWebHostEnvironment _env, UserManager<MyIdentityUser> userManager) : Controller
{
    readonly IWebHostEnvironment env = _env;
    readonly char ds = Path.DirectorySeparatorChar;
    readonly UserManager<MyIdentityUser> userManager = userManager;

    public async Task<IActionResult> IndexAsync()
    {
        List<string> notifications = [];
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity.Name!))!;
            notifications = me.Notifications;
        }
        return View(notifications);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SubmitMainImage(IFormFile imgFile)
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (me.UserGuid != "admin")
        {
            object o = "فقط ادمین مجوز تغییر عکس صفحه اصلی را دارد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }
        string userImagePath = $"{env.WebRootPath}{ds}Images{ds}HomeMainImage.jpg";
        using (FileStream fs = System.IO.File.Create(userImagePath))
        {
            await imgFile.CopyToAsync(fs);
        }
        return RedirectToAction(nameof(IndexAsync));
    }

}