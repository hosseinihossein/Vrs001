using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Controllers;

public class RegulationsController : Controller
{
    readonly Regulations_DbContext regulationDb;
    readonly IWebHostEnvironment env;
    readonly char ds = Path.DirectorySeparatorChar;
    readonly Regulations_Process regulationProcess;
    readonly Regulation_Info regulation_Info;
    readonly DisplayMedia_Info displayMedia_Info;
    public RegulationsController(Regulations_DbContext regulationDb, IWebHostEnvironment env,
    Regulations_Process regulationProcess, Regulation_Info regulation_Info,
    DisplayMedia_Info displayMedia_Info)
    {
        this.regulationDb = regulationDb;
        this.env = env;
        this.regulationProcess = regulationProcess;
        this.regulation_Info = regulation_Info;
        this.displayMedia_Info = displayMedia_Info;
    }

    public async Task<IActionResult> Index(string? category)
    {
        List<Regulations_DbModel> regulations;
        if (category == null)
        {
            regulations = await regulationDb.Regulations.ToListAsync();
            ViewBag.SameCategory = false;
        }
        else
        {
            regulations = await regulationDb.Regulations.Where(r => r.Category == category).ToListAsync();
            ViewBag.SameCategory = true;
        }

        ViewBag.DisplayMedia_Info = displayMedia_Info;
        return View(regulations);
    }

    [Authorize(Roles = "Regulations_Admins")]
    public IActionResult NewRegulation()
    {
        ViewBag.Categories = regulation_Info.Categories;
        return View(new Regulations_FormModel());
    }

    [HttpPost]
    [Authorize(Roles = "Regulations_Admins")]
    public async Task<IActionResult> SubmitNewRegulation(Regulations_FormModel regulationFormModel)
    {
        if (ModelState.IsValid)
        {
            if (!regulation_Info.Categories.Contains(regulationFormModel.Category))
            {
                ModelState.AddModelError("category", "دسته بندی انتخاب شده اشتباه میباشد!");
                return View(nameof(NewRegulation));
            }
            string regulationGuid = Guid.NewGuid().ToString().Replace("-", "");
            Regulations_DbModel regulationDbModel = new()
            {
                Guid = regulationGuid,
                Category = regulationFormModel.Category,
                Title = regulationFormModel.Title,
                BriefDescription = regulationFormModel.Brief,
                AttachedFileName = regulationFormModel.RegulationFile?.FileName
            };
            await regulationDb.Regulations.AddAsync(regulationDbModel);
            await regulationDb.SaveChangesAsync();

            //***** save file *****
            if (regulationFormModel.RegulationFile is not null)
            {
                string regulationDirectory = Path.Combine(env.ContentRootPath, "Storage", "Regulations", regulationGuid);
                DirectoryInfo directoryInfo = Directory.CreateDirectory(regulationDirectory);
                using (FileStream fs = System.IO.File
                .Create(Path.Combine(directoryInfo.FullName, regulationFormModel.RegulationFile.FileName)))
                {
                    await regulationFormModel.RegulationFile.CopyToAsync(fs);
                }
            }

            //seed regulation
            await regulationProcess.UpdateRegulationSeed(regulationDbModel);

            return RedirectToAction("Index", new { category = regulationFormModel.Category });
        }
        return View(nameof(NewRegulation));
    }

    public async Task<IActionResult> Download(string regulationGuid)
    {
        string regulationDirectory = Path.Combine(env.ContentRootPath, "Storage", "Regulations", regulationGuid);
        Regulations_DbModel? regulationDbModel = await regulationDb.Regulations.FirstOrDefaultAsync(r => r.Guid == regulationGuid);
        if (regulationDbModel == null)
        {
            ViewBag.ResultState = "danger";
            object o1 = "دستورالعمل پیدا نشد!";
            return View("Result", o1);
        }

        if (regulationDbModel.AttachedFileName is not null && Directory.Exists(regulationDirectory))
        {
            string filePath = Path.Combine(regulationDirectory, regulationDbModel.AttachedFileName);
            if (regulationDbModel.AttachedFileName.ToLower().EndsWith(".pdf"))
            {
                Response.Headers.ContentDisposition = $"inline; filename=\"{regulationDbModel.AttachedFileName}\"";
                return PhysicalFile(filePath, "application/pdf", enableRangeProcessing: true);
            }
            return PhysicalFile(filePath, "application/*", regulationDbModel.AttachedFileName,
                enableRangeProcessing: true);
        }

        ViewBag.ResultState = "danger";
        object o = "فایل پیدا نشد!";
        return View("Result", o);
    }

    [Authorize(Roles = "Regulations_Admins")]
    public async Task<IActionResult> Delete(string regulationGuid)
    {
        /*if (User.Identity?.Name != "admin")
        {
            ViewBag.ResultState = "danger";
            return View("Result", "فقط ادمین میتواند دستورالعمل ها را حذف کند!");
            //return RedirectToAction("Index", "Result", new { resultMessage = "فقط ادمین میتواند دستورالعمل ها را حذف کند!" });
        }*/

        string regulationDirectory = Path.Combine(env.ContentRootPath, "Storage", "Regulations", regulationGuid);

        Regulations_DbModel? regulationDbModel = await regulationDb.Regulations.FirstOrDefaultAsync(r => r.Guid == regulationGuid);
        if (regulationDbModel == null)
        {
            if (Directory.Exists(regulationDirectory))
            {
                Directory.Delete(regulationDirectory, true);
            }
            ViewBag.ResultState = "danger";
            return View("Result", "دستورالعمل پیدا نشد!");
        }

        regulationDb.Regulations.Remove(regulationDbModel);
        await regulationDb.SaveChangesAsync();

        if (Directory.Exists(regulationDirectory))
        {
            Directory.Delete(regulationDirectory, true);
        }

        //regulation seed
        regulationProcess.DeleteRegulationSeed(regulationDbModel);

        return RedirectToAction(nameof(Index), new { category = regulationDbModel.Category });
    }

    public async Task<IActionResult> DisplayMedia(string regulationGuid)
    {
        Regulations_DbModel? regulationDbModel = await regulationDb.Regulations.FirstOrDefaultAsync(r => r.Guid == regulationGuid);
        if (regulationDbModel == null)
        {
            ViewBag.ResultState = "danger";
            object o1 = "اطلاعات فایل پیدا نشد!";
            return View("Result", o1);
        }

        string fileDirectory = Path.Combine(env.ContentRootPath, "Storage", "Regulations", regulationGuid);

        if (regulationDbModel.AttachedFileName is not null)
        {
            string filePath = Path.Combine(fileDirectory, regulationDbModel.AttachedFileName);
            FileInfo fileInfo = new FileInfo(filePath);
            string category = "";
            if (displayMedia_Info.ImageFormats.Any(f => regulationDbModel.AttachedFileName.EndsWith(f.ToLower())))
            {
                category = "img";
            }
            else if (displayMedia_Info.VideoFormats.Any(f => regulationDbModel.AttachedFileName.EndsWith(f.ToLower())))
            {
                category = "video";
            }
            else
            {
                ViewBag.ResultState = "danger";
                object o1 = "فرمت فایل شناسایی نشد!";
                return View("Result", o1);
            }
            if (fileInfo.Exists)
            {
                DisplayMediaModel displayMediaModel = new()
                {
                    Category = category,
                    Description = regulationDbModel.BriefDescription,
                    Title = regulationDbModel.Title,
                    Src = $"./Download?regulationGuid={regulationDbModel.Guid}",
                    VideoPoster = $""
                };
                return View(displayMediaModel);
            }
        }

        ViewBag.ResultState = "danger";
        object o = "فایل پیدا نشد!";
        return View("Result", o);

    }

}