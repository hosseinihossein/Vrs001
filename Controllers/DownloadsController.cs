using Microsoft.Net.Http.Headers;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using App.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;
using System.Text;
using System.Globalization;
using System.Text.Unicode;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Antiforgery;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;

namespace App.Controllers;

//[AutoValidateAntiforgeryToken]
//[BindNever]
public class DownloadsController : Controller
{
    readonly IWebHostEnvironment env;
    readonly Downloads_DbContext downloadsDb;
    readonly char ds = Path.DirectorySeparatorChar;
    readonly UploadLargeFile uploadLargeFile;
    readonly Downloads_Process downloadsProcess;
    readonly Downloads_Info downloads_Info;
    readonly DisplayMedia_Info displayMedia_Info;

    // Get the default form options so that we can use them to set the default 
    // limits for request body data.
    //private static readonly FormOptions _defaultFormOptions = new FormOptions();

    public DownloadsController(IWebHostEnvironment env, Downloads_DbContext downloadsDb,
    UploadLargeFile uploadLargeFile, Downloads_Process downloadsProcess, Downloads_Info downloads_Info,
    DisplayMedia_Info displayMedia_Info)
    {
        this.env = env;
        this.downloadsDb = downloadsDb;
        this.uploadLargeFile = uploadLargeFile;
        this.downloadsProcess = downloadsProcess;
        this.downloads_Info = downloads_Info;
        this.displayMedia_Info = displayMedia_Info;
    }

    public async Task<IActionResult> Index(string? category, int page = 1)
    {
        List<Downloads_FileDbModel> files;
        if (category == null)
        {
            files = await downloadsDb.Files.OrderByDescending(f => f.Id)
            .Skip((page - 1) * 12).Take(12).ToListAsync();
            ViewBag.ActiveCategory = "all";
            //***** sorts 12 files per page *****
            ViewBag.LastPage = (int)Math.Ceiling(await downloadsDb.Files.CountAsync() / 12.0);
        }
        else
        {
            files = await downloadsDb.Files.Where(f => f.Category == category)
            .OrderByDescending(f => f.Id)
            .Skip((page - 1) * 12).Take(12).ToListAsync();
            ViewBag.ActiveCategory = category;
            //***** sorts 12 files per page *****
            ViewBag.LastPage = (int)Math.Ceiling(await downloadsDb.Files.Where(f => f.Category == category).CountAsync() / 12.0);
        }
        ViewBag.Categories = downloads_Info.Categories;
        return View(files);
    }

    [Authorize(Roles = "Downloads_Admins")]
    public async Task<IActionResult> DeleteFile(string fileGuid)
    {
        var file = await downloadsDb.Files.FirstOrDefaultAsync(f => f.Guid == fileGuid);
        if (file == null)
        {
            object o = "فایل پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        downloadsDb.Files.Remove(file);
        await downloadsDb.SaveChangesAsync();

        string fileDirectoryPath = Path.Combine(env.ContentRootPath, "Storage", "Downloads", file.Guid);
        //string fileImagePath = $"{env.WebRootPath}{ds}Images{ds}Files{ds}{file.Guid}";
        if (Directory.Exists(fileDirectoryPath))
        {
            Directory.Delete(fileDirectoryPath, true);
        }
        /*if (System.IO.File.Exists(fileImagePath))
        {
            System.IO.File.Delete(fileImagePath);
        }*/

        //seed file
        downloadsProcess.DeleteFileSeed(file);

        return RedirectToAction("Index", new { category = file.Category });
    }

    [Authorize(Roles = "Downloads_Admins")]
    [GenerateAntiforgeryTokenCookie]
    public IActionResult AddFile()
    {
        ViewBag.Categories = downloads_Info.Categories;
        return View(new Downloads_FormModel());
    }

    [Authorize(Roles = "Downloads_Admins")]
    [HttpPost]
    [DisableFormValueModelBinding]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> SubmitNewFile()
    {
        Downloads_FormModel formModel = new();
        List<string> uploadingFormFileNames = ["File", "FileImage"];
        var uploadLargeFile_Model = await uploadLargeFile.GetFormModelAndLargeFiles(HttpContext, formModel,
        uploadingFormFileNames, this);

        if (!uploadLargeFile_Model.FormFileNameFullName.ContainsKey("File") ||
        string.IsNullOrWhiteSpace(uploadLargeFile_Model.FormFileNameFullName["File"]))
        {
            ModelState.AddModelError("فایل", "فایل نمیتواند خالی باشد! لطفا فایل مورد نظر را انتخاب کنید.");
            //return View(formModel);
            return BadRequest(ModelState);
        }

        string fileGuid = uploadLargeFile_Model.UploadedFileGuid;
        FileInfo fileTempPathFileInfo =
        new FileInfo(uploadLargeFile_Model.FormFileNameFullName["File"]);
        string fileName = fileTempPathFileInfo.Name;

        if (!uploadLargeFile_Model.IsSuccessful)
        {
            if (fileTempPathFileInfo.Exists)
            {
                fileTempPathFileInfo.Directory?.Delete(true);
            }

            foreach (var kv in uploadLargeFile_Model.ErrorDescription)
            {
                ModelState.AddModelError(kv.Key, kv.Value);
            }
            return BadRequest(ModelState);
        }

        if (!downloads_Info.Categories.Contains(formModel.Category))
        {
            if (fileTempPathFileInfo.Exists)
            {
                fileTempPathFileInfo.Directory?.Delete(true);
            }

            ModelState.AddModelError("category", "نوع فایل اشتتباه میباشد!");
            return BadRequest(ModelState);
        }

        var fileDirInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Storage", "Downloads", fileGuid));

        if (fileTempPathFileInfo.Exists)
        {
            System.IO.File.Move(fileTempPathFileInfo.FullName, Path.Combine(fileDirInfo.FullName, fileName));
        }
        else
        {
            //object o = "فایل موقت پیدا نشد!";
            //ViewBag.ResultState = "danger";
            //return View("Result", o);
            ModelState.AddModelError("temp file", "فایل موقت پیدا نشد!");
            //return View(formModel);
            return BadRequest(ModelState);
        }

        if (uploadLargeFile_Model.FormFileNameFullName.ContainsKey("FileImage") &&
        !string.IsNullOrWhiteSpace(uploadLargeFile_Model.FormFileNameFullName["FileImage"]))
        {
            FileInfo fileImageTempPathFileInfo =
            new FileInfo(uploadLargeFile_Model.FormFileNameFullName["FileImage"]);
            string fileImageName = "FileImage.jpg";
            if (fileImageTempPathFileInfo.Exists)
            {
                System.IO.File.Move(fileImageTempPathFileInfo.FullName, Path.Combine(fileDirInfo.FullName, fileImageName));
            }
        }
        else
        {
            if (formModel.Category == "عکس")
            {
                var inputPath = Path.Combine(env.ContentRootPath, "Storage", "Downloads", fileGuid, fileName);
                using (var image = Image.Load(inputPath))
                {
                    image.Mutate(x => x.Resize(/*newWidth, newHeight*/200, 200));
                    var outputPath = Path.Combine(env.ContentRootPath, "Storage", "Downloads", fileGuid, "FileImage.jpg");
                    image.Save(outputPath); // Automatically detects format based on file extension
                }
            }
            //else if (formModel.Category == "ویدئو")
        }

        Downloads_FileDbModel fileDbModel = new()
        {
            Guid = fileGuid,
            Title = formModel.Title,
            Description = formModel.Description ?? string.Empty,
            Category = formModel.Category,
            FileName = fileName,
        };
        await downloadsDb.Files.AddAsync(fileDbModel);
        await downloadsDb.SaveChangesAsync();

        fileTempPathFileInfo.Directory?.Delete(true);

        //seed file
        await downloadsProcess.UpdateFileSeed(fileDbModel);

        return Ok($"/Downloads?category={formModel.Category}");
    }

    public async Task<IActionResult> FileImage(string fileGuid)
    {
        string fileImagePath = Path.Combine(env.ContentRootPath, "Storage", "Downloads", fileGuid, "FileImage.jpg");
        if (System.IO.File.Exists(fileImagePath))
        {
            return PhysicalFile(fileImagePath, "image/*");
        }
        else
        {
            var fileDbModel = await downloadsDb.Files.FirstOrDefaultAsync(f => f.Guid == fileGuid);
            if (fileDbModel is null)
            {
                return NotFound();
            }

            //if (fileDbModel.Category == "عکس"){}
            if (fileDbModel.Category == "ویدیو")
            {
                return PhysicalFile($"{env.WebRootPath}{ds}Images{ds}defaultVideo.jpg", "image/*");
            }
            else if (fileDbModel.Category == "نرم افزار")
            {
                return PhysicalFile($"{env.WebRootPath}{ds}Images{ds}defaultApp.jpg", "image/*");
            }
            else if (fileDbModel.Category == "مقاله")
            {
                return PhysicalFile($"{env.WebRootPath}{ds}Images{ds}defaultDocument.png", "image/*");
            }
            else if (fileDbModel.Category == "فرم")
            {
                return PhysicalFile($"{env.WebRootPath}{ds}Images{ds}defaultForm.png", "image/*");
            }
        }
        return PhysicalFile($"{env.WebRootPath}{ds}Images{ds}defaultImage.png", "image/*");
    }

    public async Task<IActionResult> Download(string fileGuid)
    {
        string fileDirectory = Path.Combine(env.ContentRootPath, "Storage", "Downloads", fileGuid);
        Downloads_FileDbModel? fileDbModel = await downloadsDb.Files.FirstOrDefaultAsync(r => r.Guid == fileGuid);
        if (fileDbModel == null)
        {
            ViewBag.ResultState = "danger";
            object o1 = "اطلاعات فایل پیدا نشد!";
            return View("Result", o1);
        }

        if (fileDbModel.FileName is not null && Directory.Exists(fileDirectory))
        {
            string filePath = Path.Combine(fileDirectory, fileDbModel.FileName);
            if (fileDbModel.FileName.ToLower().EndsWith(".pdf"))
            {
                Response.Headers.ContentDisposition = $"inline; filename=\"{fileDbModel.FileName}\"";
                return PhysicalFile(filePath, "application/pdf", enableRangeProcessing: true);
            }
            return PhysicalFile(filePath, "application/*", fileDbModel.FileName, enableRangeProcessing: true);
        }

        ViewBag.ResultState = "danger";
        object o = "فایل پیدا نشد!";
        return View("Result", o);
    }

    public async Task<IActionResult> DisplayMedia(string fileGuid)
    {
        string fileDirectory = Path.Combine(env.ContentRootPath, "Storage", "Downloads", fileGuid);
        Downloads_FileDbModel? fileDbModel = await downloadsDb.Files.FirstOrDefaultAsync(r => r.Guid == fileGuid);
        if (fileDbModel == null)
        {
            ViewBag.ResultState = "danger";
            object o1 = "اطلاعات فایل پیدا نشد!";
            return View("Result", o1);
        }

        if (fileDbModel.FileName is not null)
        {
            string filePath = Path.Combine(fileDirectory, fileDbModel.FileName);
            FileInfo fileInfo = new FileInfo(filePath);
            string category = "";
            if (displayMedia_Info.ImageFormats.Any(f => fileDbModel.FileName.EndsWith(f.ToLower())))
            {
                category = "img";
            }
            else if (displayMedia_Info.VideoFormats.Any(f => fileDbModel.FileName.EndsWith(f.ToLower())))
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
                    Description = fileDbModel.Description,
                    Title = fileDbModel.Title,
                    Src = $"./Download?fileGuid={fileDbModel.Guid}",
                    VideoPoster = $"./FileImage?fileGuid={fileDbModel.Guid}"
                };
                return View(displayMediaModel);
            }
        }

        ViewBag.ResultState = "danger";
        object o = "فایل پیدا نشد!";
        return View("Result", o);
    }

}