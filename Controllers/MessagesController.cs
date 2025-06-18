using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using App.Filters;
using App.Models;

namespace App.Controllers;

[Authorize]
//[AutoValidateAntiforgeryToken]
public class MessagesController : Controller
{
    readonly UserManager<MyIdentityUser> userManager;
    readonly IWebHostEnvironment env;
    //readonly char ds = Path.DirectorySeparatorChar;
    readonly Messages_DbContext messageDb;
    readonly Notification_Process notification_Process;
    readonly Messages_Process messages_Process;
    readonly UploadLargeFile uploadLargeFile;
    readonly Account_Process accountProcess;
    readonly DisplayMedia_Info displayMedia_Info;

    public MessagesController(UserManager<MyIdentityUser> userManager, IWebHostEnvironment env,
    Messages_DbContext messageDb, Notification_Process notification_Process,
    Messages_Process messages_Process, UploadLargeFile uploadLargeFile, Account_Process accountProcess,
    DisplayMedia_Info displayMedia_Info)
    {
        this.userManager = userManager;
        this.env = env;
        this.messageDb = messageDb;
        this.notification_Process = notification_Process;
        this.messages_Process = messages_Process;
        this.uploadLargeFile = uploadLargeFile;
        this.accountProcess = accountProcess;
        this.displayMedia_Info = displayMedia_Info;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        MyIdentityUser? me = await userManager.FindByNameAsync(User.Identity!.Name!);
        if (me == null)
        {
            return NotFound();
        }

        List<Messages_ListRowModel> allMessages = await messages_Process.GetMessagesList(me/*, page*/);

        //***** ViewBag.LastPage ***** 
        ViewBag.LastPage =
        (int)Math.Ceiling(allMessages.Count / (double)messages_Process.NumberOfMessagesPerPage);

        List<Messages_ListRowModel> messages = allMessages
        .Skip((page - 1) * messages_Process.NumberOfMessagesPerPage)
        .Take(messages_Process.NumberOfMessagesPerPage)
        .ToList();

        //review notifs
        var allMessagesGuids = messages.Select(m => m.MessageGuid);
        for (int i = 0; i < me.Notifications.Count; i++)
        {
            string notifString = me.Notifications[i];
            if (notifString.Contains("msg_"))
            {
                string msgGuid = notifString.Replace("msg_", "");
                if (!allMessagesGuids.Contains(msgGuid))
                {
                    await notification_Process.RemoveNotif_FromUser(notifString, me);
                    i--;
                }
            }
        }

        ViewBag.CurrentPage = page;
        return View(messages);
    }

    [GenerateAntiforgeryTokenCookie]
    public async Task<IActionResult> NewMessage(string ToUserGuid = "")
    {
        List<MyIdentityUser> myOrderedUsers = [];
        var orderedUsers = accountProcess.OrderUsersByPost(await userManager.Users.ToListAsync());
        //*********************************
        MyIdentityUser? receiver = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == ToUserGuid);
        if (receiver is not null)
        {
            orderedUsers.Remove(orderedUsers.Single(u => u.UserGuid == receiver.UserGuid));
            myOrderedUsers.Add(receiver);
        }
        myOrderedUsers.AddRange(orderedUsers);

        /*List<MyIdentityUser> CEOs = await userManager.Users
            .Where(u => u.Post.Contains("مدیرکل") || u.Post.Contains("مدیر کل")).ToListAsync();
        allUsersSorted.AddRange(CEOs);

        List<MyIdentityUser> Managers = await userManager.Users
        .Where(u => u.Post.Contains("مدیر")).ToListAsync();
        allUsersSorted.AddRange(Managers);

        List<MyIdentityUser> Bosses = await userManager.Users
        .Where(u => u.Post.Contains("رئیس")).ToListAsync();
        allUsersSorted.AddRange(Bosses);

        List<MyIdentityUser> Supervisors = await userManager.Users
        .Where(u => u.Post.Contains("سرپرست")).ToListAsync();
        allUsersSorted.AddRange(Supervisors);

        List<MyIdentityUser> Employees = await userManager.Users
        .Where(u => (!u.Post.Contains("مدیرکل") || !u.Post.Contains("مدیر کل")) &&
            !u.Post.Contains("مدیر") &&
            !u.Post.Contains("رئیس") &&
            !u.Post.Contains("سرپرست"))
        .ToListAsync();
        allUsersSorted.AddRange(Employees);

        allUsersSorted = allUsersSorted.DistinctBy(u => u.UserGuid).ToList();*/

        var ownUser = myOrderedUsers.FirstOrDefault(u => u.UserName == User.Identity!.Name);
        if (ownUser != null)
        {
            myOrderedUsers.Remove(ownUser);
        }
        //*********************************

        MyIdentityUser? senderUser = await userManager.FindByNameAsync(User.Identity?.Name!);
        if (senderUser is null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.ShowCheckmarks = true;
        ViewBag.CheckedUserGuids = ToUserGuid;
        return View(new Messages_NewMessageModel()
        {
            UsersList = myOrderedUsers,
            ReceiversGuid = ToUserGuid,
            SenderUser = senderUser
        });
    }

    [HttpPost]
    [DisableFormValueModelBinding]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> SubmitNewMessage()
    {
        Messages_FormModel formModel = new();
        List<string> uploadingFormFileNames = ["AttachedFile"];
        var uploadLargeFile_Model = await uploadLargeFile.GetFormModelAndLargeFiles(HttpContext, formModel,
        uploadingFormFileNames, this);

        string fileGuid = uploadLargeFile_Model.UploadedFileGuid;

        FileInfo? fileTempPathFileInfo = null;
        string? fileName = null;
        if (uploadLargeFile_Model.FormFileNameFullName.ContainsKey("AttachedFile") &&
        !string.IsNullOrWhiteSpace(uploadLargeFile_Model.FormFileNameFullName["AttachedFile"]))
        {
            fileTempPathFileInfo = new FileInfo(uploadLargeFile_Model.FormFileNameFullName["AttachedFile"]);
            fileName = fileTempPathFileInfo.Name;
        }

        if (!uploadLargeFile_Model.IsSuccessful)
        {
            //***** delete attached file *****
            if (fileTempPathFileInfo?.Exists ?? false)
            {
                fileTempPathFileInfo.Directory?.Delete(true);
            }

            foreach (var kv in uploadLargeFile_Model.ErrorDescription)
            {
                Console.WriteLine(kv.Key + "\n" + kv.Value);
                ModelState.AddModelError(kv.Key, kv.Value);
            }
            return BadRequest(ModelState);
        }

        string[] toUserGuids = formModel.ToUserGuids.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (toUserGuids.Length == 0)
        {
            //***** delete attached file *****
            if (fileTempPathFileInfo?.Exists ?? false)
            {
                fileTempPathFileInfo.Directory?.Delete(true);
            }
            //ViewBag.ResultState = "danger";
            //return RedirectToAction("Index", "Result", new { resultMessage = "پیام شما مخاطب ندارد!" });
            //object o = "پیام شما مخاطب ندارد!";
            ModelState.AddModelError("مخاطب", "پیام شما مخاطب ندارد!");
            return BadRequest(ModelState);
        }

        List<string> toAcceptedUserGuids = [];
        foreach (string userGuid in toUserGuids)
        {
            if (await userManager.Users.AnyAsync(u => u.UserGuid == userGuid))
            {
                toAcceptedUserGuids.Add(userGuid);
            }
        }

        MyIdentityUser user = (await userManager.FindByNameAsync(User.Identity!.Name!))!;

        Messages_DbModel messageDbModel = new()
        {
            MessageGuid = fileGuid,
            FromUserGuid = user.UserGuid,
            ToUsersGuids = toAcceptedUserGuids,
            Subject = formModel.Subject,
            MessageText = formModel.MessageText,
            AttachedFileName = fileName
        };
        await messageDb.Messages.AddAsync(messageDbModel);
        await messageDb.SaveChangesAsync();

        //***** save attached file *****
        if ((fileTempPathFileInfo?.Exists ?? false) && fileName != null)
        {
            string attachedFileDirectory = Path.Combine(env.ContentRootPath, "Storage", "Messages", fileGuid);
            DirectoryInfo attachedFileDirectoryInfo = Directory.CreateDirectory(attachedFileDirectory);
            string destinationFilePath = Path.Combine(attachedFileDirectoryInfo.FullName, fileName);

            System.IO.File.Move(fileTempPathFileInfo.FullName, destinationFilePath);
            fileTempPathFileInfo.Directory?.Delete(true);
        }

        //***** Notifications *****
        foreach (string userGuid in toAcceptedUserGuids)
        {
            await notification_Process.SetNewNotif_ToUser($"msg_{messageDbModel.MessageGuid}", userGuid);
        }

        //seed message
        await messages_Process.UpdateMessageSeed(messageDbModel);

        //delete old messages
        await messages_Process.RemoveOlderMessages(DateTime.Today - TimeSpan.FromDays(30));

        return Ok($"/Messages");

    }

    public async Task<IActionResult> Open(string messageGuid, int currentPage = 1)
    {
        MyIdentityUser? meOpener = await userManager.FindByNameAsync(User.Identity!.Name!);
        if (meOpener == null)
        {
            return RedirectToAction("Login", "Account");
        }

        Messages_DbModel? messageDbModel = await messageDb.Messages.FirstOrDefaultAsync(m => m.MessageGuid == messageGuid);
        if (messageDbModel == null)
        {
            ViewBag.ResultState = "danger";
            object o1 = "پیام پیدا نشد!";
            return View("Result", o1);
        }

        //List<string> toUserGuids = [.. messageDbModel.ToUsersGuidsCommaSeparated.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

        if (await userManager.IsInRoleAsync(meOpener, "Messages_Admins") ||
            messageDbModel.ToUsersGuids.Contains(meOpener.UserGuid) ||
            messageDbModel.FromUserGuid == meOpener.UserGuid)
        {
            if (!messageDbModel.SeenByClientsGuids.Contains(meOpener.UserGuid))
            {
                //messageDbModel.SeenByClientsGuids.Add(meOpener.UserGuid);
                messageDbModel.SeenByClientsGuids = [.. messageDbModel.SeenByClientsGuids, meOpener.UserGuid];
                await messageDb.SaveChangesAsync();
                //seed message
                await messages_Process.UpdateMessageSeed(messageDbModel);
            }

            MyIdentityUser? fromUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == messageDbModel.FromUserGuid);

            List<MyIdentityUser> toClients = new();
            foreach (string userGuid in messageDbModel.ToUsersGuids ?? [])
            {
                MyIdentityUser toUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid) ?? new();
                // toUserAndRolesModel = new(toUser, [.. (await userManager.GetRolesAsync(toUser))]);
                toClients.Add(toUser);
            }

            List<MyIdentityUser> seenByClients = new();
            string seenByClientsInString = "";
            foreach (string userGuid in messageDbModel.SeenByClientsGuids)
            {
                MyIdentityUser seenByClient = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid) ?? new();
                //Shared_UserAndRolesModel seenByClientUserAndRolesModel = new(seenByClient, [.. (await userManager.GetRolesAsync(seenByClient))]);
                seenByClients.Add(seenByClient);
                seenByClientsInString += seenByClient.UserGuid + " ";
            }


            Messages_OpenModel Messages_OpenModel = new()
            {
                MessageGuid = messageDbModel.MessageGuid,
                FromUser = fromUser,
                Subject = messageDbModel.Subject,
                MessageText = messageDbModel.MessageText,
                AttachedFileName = messageDbModel.AttachedFileName,
                SeenByClients = seenByClients,
                Date = messageDbModel.Date,
                ToUsers = toClients,
            };

            //***** Notification *****
            await notification_Process.RemoveNotif_FromUser($"msg_{messageDbModel.MessageGuid}", meOpener);

            ViewBag.AttachedMediaFile =
            Messages_OpenModel.AttachedFileName != null &&
            (displayMedia_Info.ImageFormats.Any(Messages_OpenModel.AttachedFileName.EndsWith) ||
            displayMedia_Info.VideoFormats.Any(Messages_OpenModel.AttachedFileName.EndsWith));

            ViewBag.AttachedPdfFile =
            Messages_OpenModel.AttachedFileName != null &&
            Messages_OpenModel.AttachedFileName.ToLower().EndsWith(".pdf");

            ViewBag.ShowCheckmarks = true;
            ViewBag.CheckedUserGuids = seenByClientsInString;
            return View(Messages_OpenModel);
        }

        ViewBag.ResultStatus = "danger";
        object o = "شما مجوز مشاهده این پیام را ندارید!";
        return View("Result", o);
    }

    public async Task<IActionResult> DownloadAttachedFile(string messageGuid)
    {
        //string adminAllDirectory = $"{env.ContentRootPath}{ds}Messages";
        Messages_DbModel? messageDbModel = await messageDb.Messages.FirstOrDefaultAsync(m => m.MessageGuid == messageGuid);
        if (messageDbModel == null)
        {
            ViewBag.ResultState = "danger";
            object o = "پیام پیدا نشد!";
            return View("Result", o);
        }

        MyIdentityUser? user = await userManager.FindByNameAsync(User.Identity!.Name!);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (await userManager.IsInRoleAsync(user, "Messages_Admins") ||
            messageDbModel.ToUsersGuids.Contains(user.UserGuid) ||
            messageDbModel.FromUserGuid == user.UserGuid)
        {
            string attachedFileDirectory = Path.Combine(env.ContentRootPath, "Storage", "Messages", messageGuid);
            DirectoryInfo attachedFileDirectoryInfo = new DirectoryInfo(attachedFileDirectory);
            if (messageDbModel.AttachedFileName is not null && attachedFileDirectoryInfo.Exists)
            {
                string filePath = Path.Combine(attachedFileDirectoryInfo.FullName, messageDbModel.AttachedFileName);
                if (messageDbModel.AttachedFileName.ToLower().EndsWith(".pdf"))
                {
                    Response.Headers.ContentDisposition = $"inline; filename=\"{messageDbModel.AttachedFileName}\"";
                    return PhysicalFile(filePath, "application/pdf", enableRangeProcessing: true);
                }
                return PhysicalFile(filePath, "application/*", messageDbModel.AttachedFileName,
                    enableRangeProcessing: true);
            }
            else
            {
                ViewBag.ResultState = "danger";
                object o = "فایل پیدا نشد!";
                return View("Result", o);
            }
        }
        else
        {
            ViewBag.ResultStatus = "danger";
            object o = "شما مجوز مشاهده این پیام را ندارید!";
            return View("Result", o);
        }
    }

    [Authorize(Roles = "Messages_Admins")]
    public async Task<IActionResult> DeleteMessage(string messageGuid)
    {
        /*if (User.Identity?.Name != "admin")
        {
            ViewBag.ResultState = "danger";
            object o = "فقط ادمین میتواند پیامها را حذف کند!";
            return View("Result", o);
        }*/

        string attachedFileDirectory = Path.Combine(env.ContentRootPath, "Storage", "Messages", messageGuid);
        DirectoryInfo attachedFileDirectoryInfo = new DirectoryInfo(attachedFileDirectory);

        Messages_DbModel? messageDbModel = await messageDb.Messages.FirstOrDefaultAsync(m => m.MessageGuid == messageGuid);
        if (messageDbModel == null)
        {
            if (attachedFileDirectoryInfo.Exists)
            {
                attachedFileDirectoryInfo.Delete(true);
            }
            ViewBag.ResultState = "danger";
            object o = "پیام پیدا نشد و فایل مربوطه حذف شد!";
            return View("Result", o);
        }

        //seed message
        messages_Process.DeleteMessageSeed(messageDbModel);

        //***** Notification *****
        foreach (var receiverGuid in messageDbModel.ToUsersGuids)
        {
            await notification_Process.RemoveNotif_FromUser($"msg_{messageDbModel.MessageGuid}", receiverGuid);
        }

        messageDb.Messages.Remove(messageDbModel);
        await messageDb.SaveChangesAsync();
        if (attachedFileDirectoryInfo.Exists)
        {
            attachedFileDirectoryInfo.Delete(true);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> DisplayMedia(string messageGuid)
    {
        MyIdentityUser meOpener = (await userManager.FindByNameAsync(User.Identity!.Name!))!;

        Messages_DbModel? messageDbModel = await messageDb.Messages.FirstOrDefaultAsync(m => m.MessageGuid == messageGuid);
        if (messageDbModel == null)
        {
            ViewBag.ResultState = "danger";
            object o1 = "اطلاعات فایل پیدا نشد!";
            return View("Result", o1);
        }

        if (await userManager.IsInRoleAsync(meOpener, "Messages_Admins") ||
            messageDbModel.ToUsersGuids.Contains(meOpener.UserGuid) ||
            messageDbModel.FromUserGuid == meOpener.UserGuid)
        {
            string fileDirectory = Path.Combine(env.ContentRootPath, "Storage", "Messages", messageGuid);

            if (messageDbModel.AttachedFileName is not null)
            {
                string filePath = Path.Combine(fileDirectory, messageDbModel.AttachedFileName);
                FileInfo fileInfo = new FileInfo(filePath);
                string category = "";
                if (displayMedia_Info.ImageFormats.Any(f => messageDbModel.AttachedFileName.EndsWith(f.ToLower())))
                {
                    category = "img";
                }
                else if (displayMedia_Info.VideoFormats.Any(f => messageDbModel.AttachedFileName.EndsWith(f.ToLower())))
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
                        Description = string.Empty,
                        Title = messageDbModel.Subject,
                        Src = $"./DownloadAttachedFile?messageGuid={messageDbModel.MessageGuid}",
                        VideoPoster = $""
                    };
                    return View(displayMediaModel);
                }
            }

            ViewBag.ResultState = "danger";
            object o = "فایل پیدا نشد!";
            return View("Result", o);
        }

        ViewBag.ResultStatus = "danger";
        object o2 = "شما مجوز مشاهده این پیام را ندارید!";
        return View("Result", o2);
    }

}