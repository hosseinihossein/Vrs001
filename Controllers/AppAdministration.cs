using System.Globalization;
using System.IO.Compression;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[Authorize]
public class AppAdministrationController : Controller
{
    readonly UserManager<MyIdentityUser> userManager;
    readonly IWebHostEnvironment env;
    readonly DirectoryInfo backupDirectoryInfo;
    readonly FileInfo backupFileInfo;
    public AppAdministrationController(UserManager<MyIdentityUser> userManager, IWebHostEnvironment env)
    {
        this.userManager = userManager;
        this.env = env;

        backupDirectoryInfo =
        Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "TempFiles", "Backup"));

        backupFileInfo = new FileInfo(Path.Combine(backupDirectoryInfo.FullName, "backup.zip"));
    }

    public async Task<IActionResult> Backup()
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (me.UserGuid != "admin")
        {
            object o = "Access Denied!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        object o1 = "<h2>لطفا توجه بفرمایید، بهتر است عملیات بکاپ خارج از ساعات اداری و در وقت بیکاری سرور انجام گیرد.</h2>";

        if (backupFileInfo.Exists)
        {
            DateTime creationTime = backupFileInfo.CreationTime;
            object o2 = $"<h2>یک فایل بکاپ متعلق به تاریخ {creationTime.ToString("yyyy/MM/dd", new CultureInfo("fa-IR"))} وجود دارد.</h2>";

            ViewBag.SuccessBtnName = "دانلود";
            ViewBag.SuccessBtnHref = "/AppAdministration/DownloadBackup";

            //ViewBag.InfoBtnName = "بروز رسانی";
            //ViewBag.InfoBtnHref = "/AppAdministration/RenewBackup";

            ViewBag.DangerBtnName = "حذف";
            ViewBag.DangerBtnHref = "/AppAdministration/DeleteBackup";

            return View("Result", o1 + "\n" + o2);
        }
        else
        {
            ViewBag.InfoBtnName = "بروز رسانی";
            ViewBag.InfoBtnHref = "/AppAdministration/RenewBackup";

            return View("Result", o1);
        }
    }

    public async Task<IActionResult> RenewBackup()
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (me.UserGuid != "admin")
        {
            object o = "Access Denied!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        ZipFile.CreateFromDirectory(Path.Combine(env.ContentRootPath, "Storage"), backupFileInfo.FullName);

        return RedirectToAction(nameof(Backup));
    }

    public async Task<IActionResult> DeleteBackup()
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (me.UserGuid != "admin")
        {
            object o = "Access Denied!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        if (backupFileInfo.Exists)
        {
            backupFileInfo.Delete();
        }

        return RedirectToAction(nameof(Backup));
    }

    public async Task<IActionResult> DownloadBackup()
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (me.UserGuid != "admin")
        {
            object o = "Access Denied!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        if (backupFileInfo.Exists)
        {
            return PhysicalFile(backupFileInfo.FullName, "Application/zip", "backup.zip",
            enableRangeProcessing: true);
        }

        return RedirectToAction(nameof(Backup));
    }
}