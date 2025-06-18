using System.Globalization;
using System.Net;
using System.Text;
using App.Filters;
using App.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace App.Controllers;

[AutoValidateAntiforgeryToken]
[Authorize]
public class AssessmentController : Controller
{
    readonly UserManager<MyIdentityUser> userManager;
    readonly Assessment_DbContext assessmentDb;
    readonly Assessment_Process assessmentProcess;
    readonly Assessment_Info assessmentInfo;
    readonly IWebHostEnvironment env;
    //readonly char ds = Path.DirectorySeparatorChar;
    readonly IConfiguration configuration;
    readonly UploadLargeFile uploadLargeFile;
    readonly PersianDateProcess persianDateProcess;
    readonly Notification_Process notification_Process;
    readonly Account_Process accountProcess;
    readonly Orders_DbContext ordersDb;
    readonly int NumberOfElementsPerPage = 4;
    readonly DisplayMedia_Info displayMedia_Info;

    public AssessmentController(UserManager<MyIdentityUser> userManager, Assessment_DbContext assessmentDb,
    Assessment_Process assessmentProcess, Assessment_Info assessmentInfo, IWebHostEnvironment env,
    IConfiguration configuration, UploadLargeFile uploadLargeFile, PersianDateProcess persianDateProcess,
    Notification_Process notification_Process, Account_Process accountProcess, Orders_DbContext ordersDb,
    DisplayMedia_Info displayMedia_Info)
    {
        this.userManager = userManager;
        this.assessmentDb = assessmentDb;
        this.assessmentProcess = assessmentProcess;
        this.assessmentInfo = assessmentInfo;
        this.env = env;
        this.configuration = configuration;
        this.uploadLargeFile = uploadLargeFile;
        this.persianDateProcess = persianDateProcess;
        this.notification_Process = notification_Process;
        this.accountProcess = accountProcess;
        this.ordersDb = ordersDb;
        this.displayMedia_Info = displayMedia_Info;
    }

    public async Task<IActionResult> Index(string? FromDate, string? ToDate, int page = 1)
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;

        if (!persianDateProcess.TryConvertToDateTime(FromDate, out DateTime? fromDateTime))
        {
            fromDateTime = DateTime.Today.Subtract(TimeSpan.FromDays(30));
        }

        persianDateProcess.TryConvertToDateTime(ToDate, out DateTime? toDateTime);
        if (toDateTime != null) toDateTime = toDateTime.Value.AddHours(23).AddMinutes(59);

        Assessment_PerformanceList performanceList = new()
        {
            FromDate = fromDateTime?.ToString("yyyy/MM/dd", new CultureInfo("fa-IR")),
            ToDate = ToDate,//toDateTime?.ToString("yyyy/MM/dd", new CultureInfo("fa-IR"))
            //Branch = user.Branch,
            MyUser = (await userManager.IsInRoleAsync(me, "Performance_Registrars") && me.UserGuid != "admin") ? me : null,
            //UserPost = user.Post
        };

        if (me.UserGuid == "admin")
        {
            var performanceDbModels = await assessmentDb.Performances
            .Include(p => p.ExpertConsiderations)
            .Where(p =>
                p.RegisterDate >= fromDateTime &&
                (toDateTime == null || p.RegisterDate <= toDateTime))
            .OrderByDescending(p => p.RegisterDate)
            .ToListAsync();

            foreach (var pdbm in performanceDbModels)
            {
                var performance = await assessmentProcess.GetPerformance(pdbm);
                if (performance is not null && !performanceList.Performances.Any(p => p.Guid == performance.Guid))
                {
                    performanceList.Performances.Add(performance);
                }
            }
        }
        else if (await userManager.IsInRoleAsync(me, "Can_Watch_Backgrounds"))
        {
            var performanceDbModels = await assessmentDb.Performances
            .Include(p => p.ExpertConsiderations)
            .Where(p => (p.Status == "vp" || p.Status == "expert" || p.Status == "complete" || p.Status == "reject") &&
                p.RegisterDate >= fromDateTime &&
                (toDateTime == null || p.RegisterDate <= toDateTime))
            .OrderByDescending(p => p.RegisterDate)
            .ToListAsync();

            foreach (var pdbm in performanceDbModels)
            {
                var performance = await assessmentProcess.GetPerformance(pdbm);
                if (performance is not null && !performanceList.Performances.Any(p => p.Guid == performance.Guid))
                {
                    performanceList.Performances.Add(performance);
                }
            }
        }
        else
        {
            if (await userManager.IsInRoleAsync(me, "Expert_Referees"))
            {
                var performanceDbModels = await assessmentDb.Performances
                .Include(p => p.ExpertConsiderations)
                .Where(p => p.Status == "expert" &&
                    me.PerformanceField.Contains(p.PerformanceField) &&
                    p.ExpertConsiderations.Any(ec =>
                        ec.ExpertRefereeGuid == me.UserGuid && ec.ConsiderationDate == null) &&
                    p.RegisterDate >= fromDateTime &&
                    (toDateTime == null || p.RegisterDate <= toDateTime))
                .OrderBy(p => p.RegisterDate)
                .ToListAsync();

                var performanceDbModels2 = await assessmentDb.Performances
                .Include(p => p.ExpertConsiderations)
                .Where(p => me.PerformanceField.Contains(p.PerformanceField) &&
                    p.ExpertConsiderations.Any(ec =>
                        ec.ExpertRefereeGuid == me.UserGuid && ec.ConsiderationDate != null) &&
                    p.RegisterDate >= fromDateTime &&
                    (toDateTime == null || p.RegisterDate <= toDateTime))
                .OrderByDescending(p => p.RegisterDate)
                .ToListAsync();

                foreach (var pdbm in performanceDbModels)
                {
                    var performance = await assessmentProcess.GetPerformance(pdbm);
                    if (performance is not null && !performanceList.Performances.Any(p => p.Guid == performance.Guid))
                    {
                        performanceList.Performances.Add(performance);
                    }
                }

                foreach (var pdbm in performanceDbModels2)
                {
                    var performance = await assessmentProcess.GetPerformance(pdbm);
                    if (performance is not null && !performanceList.Performances.Any(p => p.Guid == performance.Guid))
                    {
                        performanceList.Performances.Add(performance);
                    }
                }
            }
            if (await userManager.IsInRoleAsync(me, "VP_Referrers"))
            {
                var performanceDbModels = await assessmentDb.Performances
                .Include(p => p.ExpertConsiderations)
                .Where(p => p.Status == "vp" &&
                    me.PerformanceField.Contains(p.PerformanceField) &&
                    p.RegisterDate >= fromDateTime &&
                    (toDateTime == null || p.RegisterDate <= toDateTime))
                .OrderBy(p => p.RegisterDate)
                .ToListAsync();

                var performanceDbModels2 = (await assessmentDb.ExpertConsiderations
                .Include(ec => ec.Performance)
                .Where(ec => ec.VpReferrerGuid == me.UserGuid &&
                    ec.Performance.RegisterDate >= fromDateTime &&
                    (toDateTime == null || ec.Performance.RegisterDate <= toDateTime))
                .ToListAsync())
                .Select(ec => ec.Performance)
                .OrderByDescending(p => p.RegisterDate);

                foreach (var pdbm in performanceDbModels)
                {
                    var performance = await assessmentProcess.GetPerformance(pdbm);
                    if (performance is not null && !performanceList.Performances.Any(p => p.Guid == performance.Guid))
                    {
                        performanceList.Performances.Add(performance);
                    }
                }

                foreach (var pdbm in performanceDbModels2)
                {
                    var performance = await assessmentProcess.GetPerformance(pdbm);
                    if (performance is not null && !performanceList.Performances.Any(p => p.Guid == performance.Guid))
                    {
                        performanceList.Performances.Add(performance);
                    }
                }
            }
            if (await userManager.IsInRoleAsync(me, "Performance_Confirmers"))
            {
                //Branch ??= user.Branch;
                var performanceDbModels = await assessmentDb.Performances
                .Include(p => p.ExpertConsiderations)
                .Where(p => p.Status == "confirm" &&
                    p.Branch == me.Branch &&
                    p.RegisterDate >= fromDateTime &&
                    (toDateTime == null || p.RegisterDate <= toDateTime))
                .OrderByDescending(p => p.RegisterDate)
                .ToListAsync();

                foreach (var pdbm in performanceDbModels)
                {
                    var performance = await assessmentProcess.GetPerformance(pdbm);
                    if (performance is not null && !performanceList.Performances.Any(p => p.Guid == performance.Guid))
                    {
                        performanceList.Performances.Add(performance);
                    }
                }
            }
            if (await userManager.IsInRoleAsync(me, "Performance_Registrars"))
            {
                var performanceDbModels = await assessmentDb.Performances
                .Include(p => p.ExpertConsiderations)
                .Where(p => //p.Status == "edit" &&
                    p.RegistrarGuid == me.UserGuid &&
                    (p.RegisterDate == null || p.RegisterDate >= fromDateTime) &&
                    (p.RegisterDate == null || toDateTime == null || p.RegisterDate <= toDateTime))
                .OrderByDescending(p => p.RegisterDate)
                .ToListAsync();

                var preferredPerformanceDbModels = performanceDbModels
                .Where(p => p.Status == "edit" || p.Status == "reject");

                foreach (var pdbm in preferredPerformanceDbModels)
                {
                    var performance = await assessmentProcess.GetPerformance(pdbm);
                    if (performance is not null && !performanceList.Performances.Any(p => p.Guid == performance.Guid))
                    {
                        performanceList.Performances.Add(performance);
                    }
                }

                foreach (var pdbm in performanceDbModels.Except(preferredPerformanceDbModels))
                {
                    var performance = await assessmentProcess.GetPerformance(pdbm);
                    if (performance is not null && !performanceList.Performances.Any(p => p.Guid == performance.Guid))
                    {
                        performanceList.Performances.Add(performance);
                    }
                }
            }
            if (await userManager.IsInRoleAsync(me, "Branch_Chiefs"))
            {
                //Branch ??= user.Branch;
                var performanceDbModels = await assessmentDb.Performances
                .Include(p => p.ExpertConsiderations)
                .Where(p => //p.Status == "confirm" &&
                    p.Branch == me.Branch &&
                    (p.RegisterDate == null || p.RegisterDate >= fromDateTime) &&
                    (p.RegisterDate == null || toDateTime == null || p.RegisterDate <= toDateTime))
                .OrderByDescending(p => p.RegisterDate)
                .ToListAsync();

                foreach (var pdbm in performanceDbModels)
                {
                    var performance = await assessmentProcess.GetPerformance(pdbm);
                    if (performance is not null && !performanceList.Performances.Any(p => p.Guid == performance.Guid))
                    {
                        performanceList.Performances.Add(performance);
                    }
                }
            }
        }

        //review notifs
        var allPerformanceGuids = performanceList.Performances.Select(p => p.Guid);
        for (int i = 0; i < me.Notifications.Count; i++)
        {
            string notifString = me.Notifications[i];
            if (notifString.Contains("perf_"))
            {
                string perfGuid = notifString.Replace("perf_", "");
                if (!allPerformanceGuids.Contains(perfGuid))
                {
                    await notification_Process.RemoveNotif_FromUser(notifString, me);
                    i--;
                }
            }
        }

        ViewBag.Me = me;
        ViewBag.LastPage = (int)Math.Ceiling(performanceList.Performances.Count / (double)NumberOfElementsPerPage);

        performanceList.Performances = performanceList.Performances
        .Skip((page - 1) * NumberOfElementsPerPage)
        .Take(NumberOfElementsPerPage)
        .ToList();

        return View("PerformanceList", performanceList);
    }

    //for those who want access to see list of performances whithout being in any performance roles
    public async Task<IActionResult> ListOfPerformances(string? Branch = null, string? UserGuid = null,
    string? UserPost = null, string? FromDate = null, string? ToDate = null, string? orderGuid = null,
    int page = 1)
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;

        if (await userManager.IsInRoleAsync(me, "Can_Watch_Backgrounds") ||
            (await userManager.IsInRoleAsync(me, "Branch_Chiefs") && me.Branch == Branch) ||
            me.UserGuid == UserGuid)
        {
            persianDateProcess.TryConvertToDateTime(FromDate, out DateTime? fromDateTime);
            persianDateProcess.TryConvertToDateTime(ToDate, out DateTime? toDateTime);
            if (toDateTime != null) toDateTime = toDateTime.Value.AddHours(23).AddMinutes(59);

            Assessment_PerformanceList performanceList = new()
            {
                FromDate = FromDate,//fromDateTime.ToString("yyyy/MM/dd", new CultureInfo("fa-IR")),
                ToDate = ToDate,//toDateTime?.ToString("yyyy/MM/dd", new CultureInfo("fa-IR")),
                Branch = Branch,
                UserPost = UserPost,
                MyUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == UserGuid)
            };

            List<MyIdentityUser> allRelatedUsers = await accountProcess.GetAllRelatedUsers(performanceList.MyUser);
            if (allRelatedUsers.Count > 0)
            {
                foreach (MyIdentityUser user in allRelatedUsers)
                {
                    UserGuid = user.UserGuid;

                    var performanceDbModels = await assessmentDb.Performances
                        .Include(p => p.ExpertConsiderations)
                        .Where(p => (Branch == null || p.Branch == Branch) &&
                            (orderGuid == null || p.OrderGuid == orderGuid) &&
                            (UserGuid == null || p.RegistrarGuid == UserGuid || p.ChiefGuid == UserGuid) &&
                            (UserPost == null || p.RegistrarPost == UserPost || p.ChiefPost == UserPost) &&
                            (fromDateTime == null || (p.RegisterDate != null && p.RegisterDate >= fromDateTime)) &&
                            (toDateTime == null || (p.RegisterDate != null && p.RegisterDate <= toDateTime)))
                        .ToListAsync();

                    foreach (var pdbm in performanceDbModels)
                    {
                        var performance = await assessmentProcess.GetPerformance(pdbm);
                        if (performance is not null)
                        {
                            performanceList.Performances.Add(performance);
                        }
                    }
                }
            }
            else
            {
                var performanceDbModels = await assessmentDb.Performances
                        .Include(p => p.ExpertConsiderations)
                        .Where(p => (Branch == null || p.Branch == Branch) &&
                            (orderGuid == null || p.OrderGuid == orderGuid) &&
                            (UserGuid == null || p.RegistrarGuid == UserGuid || p.ChiefGuid == UserGuid) &&
                            (UserPost == null || p.RegistrarPost == UserPost || p.ChiefPost == UserPost) &&
                            (fromDateTime == null || (p.RegisterDate != null && p.RegisterDate >= fromDateTime)) &&
                            (toDateTime == null || (p.RegisterDate != null && p.RegisterDate <= toDateTime)))
                        .ToListAsync();

                foreach (var pdbm in performanceDbModels)
                {
                    var performance = await assessmentProcess.GetPerformance(pdbm);
                    if (performance is not null)
                    {
                        performanceList.Performances.Add(performance);
                    }
                }

                /*
                                if (Branch == null && UserGuid == null && UserPost == null)
                                {
                                    var performanceDbModels = await assessmentDb.Performances
                                    .Include(p => p.ExpertConsiderations)
                                    .Where(p => (fromDateTime == null || p.RegisterDate >= fromDateTime) &&
                                        (toDateTime == null || p.RegisterDate <= toDateTime))
                                    .ToListAsync();

                                    foreach (var pdbm in performanceDbModels)
                                    {
                                        var performance = await assessmentProcess.GetPerformance(pdbm);
                                        if (performance is not null)
                                        {
                                            performanceList.Performances.Add(performance);
                                        }
                                    }
                                }
                                else if (Branch is not null && UserGuid == null && UserPost == null)
                                {
                                    var performanceDbModels = await assessmentDb.Performances
                                    .Include(p => p.ExpertConsiderations)
                                    .Where(p => p.Branch == Branch &&
                                        (fromDateTime == null || p.RegisterDate >= fromDateTime) &&
                                        (toDateTime == null || p.RegisterDate <= toDateTime))
                                    .ToListAsync();

                                    foreach (var pdbm in performanceDbModels)
                                    {
                                        var performance = await assessmentProcess.GetPerformance(pdbm);
                                        if (performance is not null)
                                        {
                                            performanceList.Performances.Add(performance);
                                        }
                                    }
                                }
                                else if (Branch == null && UserGuid is not null && UserPost == null)
                                {
                                    var performanceDbModels = await assessmentDb.Performances
                                    .Include(p => p.ExpertConsiderations)
                                    .Where(p => (p.RegistrarGuid == UserGuid || p.ChiefGuid == UserGuid) &&
                                        (fromDateTime == null || p.RegisterDate >= fromDateTime) &&
                                        (toDateTime == null || p.RegisterDate <= toDateTime))
                                    .ToListAsync();

                                    foreach (var pdbm in performanceDbModels)
                                    {
                                        var performance = await assessmentProcess.GetPerformance(pdbm);
                                        if (performance is not null)
                                        {
                                            performanceList.Performances.Add(performance);
                                        }
                                    }
                                }
                                else if (Branch is not null && UserGuid is not null && UserPost == null)
                                {
                                    var performanceDbModels = await assessmentDb.Performances
                                    .Include(p => p.ExpertConsiderations)
                                    .Where(p => p.Branch == Branch &&
                                        (p.RegistrarGuid == UserGuid || p.ChiefGuid == UserGuid) &&
                                        (fromDateTime == null || p.RegisterDate >= fromDateTime) &&
                                        (toDateTime == null || p.RegisterDate <= toDateTime))
                                    .ToListAsync();

                                    foreach (var pdbm in performanceDbModels)
                                    {
                                        var performance = await assessmentProcess.GetPerformance(pdbm);
                                        if (performance is not null)
                                        {
                                            performanceList.Performances.Add(performance);
                                        }
                                    }
                                }
                                else if (Branch is not null && UserGuid == null && UserPost is not null)
                                {
                                    var performanceDbModels = await assessmentDb.Performances
                                    .Include(p => p.ExpertConsiderations)
                                    .Where(p => p.Branch == Branch &&
                                        //(p.RegistrarGuid == UserGuid || p.ChiefGuid == UserGuid) &&
                                        (p.RegistrarPost == UserPost || p.ChiefPost == UserPost) &&
                                        (fromDateTime == null || p.RegisterDate >= fromDateTime) &&
                                        (toDateTime == null || p.RegisterDate <= toDateTime))
                                    .ToListAsync();

                                    foreach (var pdbm in performanceDbModels)
                                    {
                                        var performance = await assessmentProcess.GetPerformance(pdbm);
                                        if (performance is not null)
                                        {
                                            performanceList.Performances.Add(performance);
                                        }
                                    }
                                }
                                else if (Branch == null && UserGuid is not null && UserPost is not null)
                                {
                                    var performanceDbModels = await assessmentDb.Performances
                                    .Include(p => p.ExpertConsiderations)
                                    .Where(p => //p.Branch == Branch &&
                                        (p.RegistrarGuid == UserGuid || p.ChiefGuid == UserGuid) &&
                                        (p.RegistrarPost == UserPost || p.ChiefPost == UserPost) &&
                                        (fromDateTime == null || p.RegisterDate >= fromDateTime) &&
                                        (toDateTime == null || p.RegisterDate <= toDateTime))
                                    .ToListAsync();

                                    foreach (var pdbm in performanceDbModels)
                                    {
                                        var performance = await assessmentProcess.GetPerformance(pdbm);
                                        if (performance is not null)
                                        {
                                            performanceList.Performances.Add(performance);
                                        }
                                    }
                                }
                                else if (Branch is not null && UserGuid is not null && UserPost is not null)
                                {
                                    var performanceDbModels = await assessmentDb.Performances
                                    .Include(p => p.ExpertConsiderations)
                                    .Where(p => p.Branch == Branch &&
                                        (p.RegistrarGuid == UserGuid || p.ChiefGuid == UserGuid) &&
                                        (p.RegistrarPost == UserPost || p.ChiefPost == UserPost) &&
                                        (fromDateTime == null || p.RegisterDate >= fromDateTime) &&
                                        (toDateTime == null || p.RegisterDate <= toDateTime))
                                    .ToListAsync();

                                    foreach (var pdbm in performanceDbModels)
                                    {
                                        var performance = await assessmentProcess.GetPerformance(pdbm);
                                        if (performance is not null)
                                        {
                                            performanceList.Performances.Add(performance);
                                        }
                                    }
                                }
                */
            }

            if (orderGuid != null)
            {
                ViewBag.Order = await ordersDb.Orders.FirstOrDefaultAsync(o => o.Guid == orderGuid);
            }

            ViewBag.Me = me;
            ViewBag.LastPage = (int)Math.Ceiling(performanceList.Performances.Count / (double)NumberOfElementsPerPage);

            performanceList.Performances = performanceList.Performances
            .Skip((page - 1) * NumberOfElementsPerPage)
            .Take(NumberOfElementsPerPage)
            .ToList();

            return View("PerformanceList", performanceList);
        }
        else
        {
            object o = "شما مجوز مشاهده سوابق عملکرد مدنظر را ندارید!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }
    }

    public async Task<IActionResult> ListOfArchivedPerformnaces(string? FromDate, string? ToDate,
    int page = 1)
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (me.UserGuid != "admin")
        {
            object o = "شما مجاز به مشاهده لیست آرشیو نیستید!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        persianDateProcess.TryConvertToDateTime(FromDate, out DateTime? FromDateTime);
        persianDateProcess.TryConvertToDateTime(ToDate, out DateTime? ToDateTime);
        if (ToDateTime != null) ToDateTime = ToDateTime.Value.AddHours(23).AddMinutes(59);

        List<Assessment_PerformanceDbModel> performanceDbModels = await assessmentProcess
        .ReadArchivedPerformancesList(FromDateTime, ToDateTime, NumberOfElementsPerPage,
        (page - 1) * NumberOfElementsPerPage);

        Assessment_PerformanceList performanceList = new()
        {
            FromDate = FromDate,
            ToDate = ToDate,
            Branch = null,
            UserPost = null,
            MyUser = null,
        };
        foreach (Assessment_PerformanceDbModel pdbm in performanceDbModels)
        {
            Assessment_Performance? performance = await assessmentProcess.GetPerformance(pdbm);
            if (performance != null)
            {
                performanceList.Performances.Add(performance);
            }
        }

        return View("ArchivedPerformanceList", performanceList);
    }

    public async Task<IActionResult> Performance(string guid)
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;

        Assessment_Performance? performance = await assessmentProcess.GetPerformance(guid);
        if (performance is null)
        {
            object o = "عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        if (me.UserGuid == "admin" ||
            me.UserGuid == performance.Registrar?.UserGuid ||
            (await userManager.IsInRoleAsync(me, "Performance_Confirmers") &&
                me.Branch == performance.Branch) ||
            (await userManager.IsInRoleAsync(me, "VP_Referrers") &&
                me.PerformanceField.Contains(performance.PerformanceField)) ||
            (await userManager.IsInRoleAsync(me, "Expert_Referees") &&
                me.PerformanceField.Contains(performance.PerformanceField) &&
                performance.ExpertConsiderations.Any(ec => ec.ExpertReferee?.UserGuid == me.UserGuid)) ||
            me.UserGuid == performance.Chief?.UserGuid ||
            await userManager.IsInRoleAsync(me, "Can_Watch_Backgrounds")
            )
        {
            //remove notification
            await notification_Process.RemoveNotif_FromUser($"perf_{performance.Guid}", me);

            if (await userManager.IsInRoleAsync(me, "VP_Referrers"))
            {
                ViewBag.Experts = (await userManager.GetUsersInRoleAsync("Expert_Referees"))
                    .Where(expert => expert.PerformanceField.Contains(performance.PerformanceField))
                    .ToList();
            }

            ViewBag.StatusDictionary = assessmentInfo.PerformanceStatus;
            if (performance.Order != null)
            {
                ViewBag.OrderRegistrarFullName = (await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == performance.Order.RegistrarGuid))?.FullName;
            }
            ViewBag.Me = me;

            ViewBag.AttachedMediaFile =
            performance.AttachedFileName != null &&
            (displayMedia_Info.ImageFormats.Any(performance.AttachedFileName.EndsWith) ||
            displayMedia_Info.VideoFormats.Any(performance.AttachedFileName.EndsWith));

            ViewBag.AttachedPdfFile =
            performance.AttachedFileName != null &&
            performance.AttachedFileName.ToLower().EndsWith(".pdf");

            if (configuration.GetSection("PerformanceTypes:مکاتبات").Get<List<string>>()?.Contains(performance.PerformanceType) ?? false)
                return View("Correspondence", performance);
            else
                return View("Performance", performance);
        }
        else
        {
            object o = "شما مجاز به مشاهده این عملکرد نمیباشید!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

    }

    public async Task<IActionResult> ArchivedPerformance(string guid)
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (me.UserGuid != "admin")
        {
            object o = "شما مجاز به مشاهده لیست آرشیو نیستید!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        var performanceDbModel = await assessmentProcess.ReadArchivedPerformance(guid);
        if (performanceDbModel is null)
        {
            object o = "آرشیو عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        Assessment_Performance? performance = await assessmentProcess.GetPerformance(performanceDbModel);
        if (performance is null)
        {
            object o = "عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        ViewBag.StatusDictionary = assessmentInfo.PerformanceStatus;
        if (performance.Order != null)
        {
            ViewBag.OrderRegistrarFullName = (await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == performance.Order.RegistrarGuid))?.FullName;
        }
        ViewBag.Me = me;

        ViewBag.AttachedMediaFile =
        performance.AttachedFileName != null &&
        (displayMedia_Info.ImageFormats.Any(performance.AttachedFileName.EndsWith) ||
        displayMedia_Info.VideoFormats.Any(performance.AttachedFileName.EndsWith));

        ViewBag.AttachedPdfFile =
        performance.AttachedFileName != null &&
        performance.AttachedFileName.ToLower().EndsWith(".pdf");

        if (configuration.GetSection("PerformanceTypes:مکاتبات").Get<List<string>>()?.Contains(performance.PerformanceType) ?? false)
            return View("ArchivedCorrespondence", performance);
        else
            return View("ArchivedPerformance", performance);

    }

    public async Task<IActionResult> DownloadAttachedFile(string performanceGuid)
    {
        Assessment_PerformanceDbModel? performanceDbModel =
            await assessmentDb.Performances
            .Include(p => p.ExpertConsiderations)
            .FirstOrDefaultAsync(p => p.Guid == performanceGuid);
        if (performanceDbModel is null)
        {
            object o = "عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        if (performanceDbModel.AttachedFileName is null)
        {
            object o = "فایل پیوست برای عملکرد وجود ندارد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        MyIdentityUser meDownloader = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (meDownloader.UserGuid == "admin" ||
            meDownloader.UserGuid == performanceDbModel.RegistrarGuid ||
            (await userManager.IsInRoleAsync(meDownloader, "Performance_Confirmers") &&
                meDownloader.Branch == performanceDbModel.Branch) ||
            (await userManager.IsInRoleAsync(meDownloader, "VP_Referrers") &&
                meDownloader.PerformanceField.Contains(performanceDbModel.PerformanceField)) ||
            (await userManager.IsInRoleAsync(meDownloader, "Expert_Referees") &&
                meDownloader.PerformanceField.Contains(performanceDbModel.PerformanceField) &&
                performanceDbModel.ExpertConsiderations.Any(ec => ec.ExpertRefereeGuid == meDownloader.UserGuid)))
        {
            string filePath = Path.Combine(env.ContentRootPath, "Storage", "Assessment", "Performance", performanceDbModel.Guid, performanceDbModel.AttachedFileName);
            if (System.IO.File.Exists(filePath))
            {
                if (performanceDbModel.AttachedFileName.ToLower().EndsWith(".pdf"))
                {
                    Response.Headers.ContentDisposition = $"inline; filename=\"{performanceDbModel.AttachedFileName}\"";
                    return PhysicalFile(filePath, "application/pdf", enableRangeProcessing: true);
                }
                return PhysicalFile(filePath, "application/*", performanceDbModel.AttachedFileName,
                    enableRangeProcessing: true);
            }
        }

        return NotFound();
    }

    public async Task<IActionResult> DisplayMedia(string performanceGuid)
    {
        Assessment_PerformanceDbModel? performanceDbModel =
            await assessmentDb.Performances
            .Include(p => p.ExpertConsiderations)
            .FirstOrDefaultAsync(p => p.Guid == performanceGuid);
        if (performanceDbModel is null)
        {
            object o = "عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        if (performanceDbModel.AttachedFileName is null)
        {
            object o = "فایل پیوست برای عملکرد وجود ندارد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        MyIdentityUser meDownloader = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (meDownloader.UserGuid == "admin" ||
            meDownloader.UserGuid == performanceDbModel.RegistrarGuid ||
            (await userManager.IsInRoleAsync(meDownloader, "Performance_Confirmers") &&
                meDownloader.Branch == performanceDbModel.Branch) ||
            (await userManager.IsInRoleAsync(meDownloader, "VP_Referrers") &&
                meDownloader.PerformanceField.Contains(performanceDbModel.PerformanceField)) ||
            (await userManager.IsInRoleAsync(meDownloader, "Expert_Referees") &&
                meDownloader.PerformanceField.Contains(performanceDbModel.PerformanceField) &&
                performanceDbModel.ExpertConsiderations.Any(ec => ec.ExpertRefereeGuid == meDownloader.UserGuid)))
        {
            string filePath = Path.Combine(env.ContentRootPath, "Storage", "Assessment", "Performance", performanceDbModel.Guid, performanceDbModel.AttachedFileName);
            if (System.IO.File.Exists(filePath))
            {
                string category = "";
                if (displayMedia_Info.ImageFormats.Any(f => performanceDbModel.AttachedFileName.EndsWith(f.ToLower())))
                {
                    category = "img";
                }
                else if (displayMedia_Info.VideoFormats.Any(f => performanceDbModel.AttachedFileName.EndsWith(f.ToLower())))
                {
                    category = "video";
                }
                else
                {
                    ViewBag.ResultState = "danger";
                    object o2 = "فرمت فایل شناسایی نشد!";
                    return View("Result", o2);
                }

                DisplayMediaModel displayMediaModel = new()
                {
                    Category = category,
                    Description = string.Empty,
                    Title = performanceDbModel.Subject ?? string.Empty,
                    Src = $"./DownloadAttachedFile?performanceGuid={performanceDbModel.Guid}",
                    VideoPoster = $""
                };
                return View(displayMediaModel);

            }
            ViewBag.ResultState = "danger";
            object o = "فایل پیدا نشد!";
            return View("Result", o);
        }

        object o1 = "شما مجوز مشاهده این فایل را ندارید!";
        ViewBag.ResultState = "danger";
        return View("Result", o1);
    }

    //***************** Expert_Referees *****************

    [Authorize(Roles = "Expert_Referees")]
    [HttpPost]
    public async Task<IActionResult> SubmitExpertConsideration(string PerformanceGuid, string ExpertConsiderationGuid,
    string ExpertChoice, int? Score, string? ExpertDescription)
    {
        Assessment_PerformanceDbModel? performanceDbModel =
            await assessmentDb.Performances
            .Include(p => p.ExpertConsiderations)
            .FirstOrDefaultAsync(p => p.Guid == PerformanceGuid);
        if (performanceDbModel is null)
        {
            object o = "عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        Assessment_ExpertConsiderationDbModel? ec =
            performanceDbModel.ExpertConsiderations
            .FirstOrDefault(ec => ec.Guid == ExpertConsiderationGuid);
        if (ec is null)
        {
            object o = "کارشناسی عملکرد مربوطه پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        MyIdentityUser meExpertReferee = (await userManager.FindByNameAsync(User.Identity!.Name!))!;

        if (!await userManager.IsInRoleAsync(meExpertReferee, "Expert_Referees") ||
            !meExpertReferee.PerformanceField.Contains(performanceDbModel.PerformanceField) ||
            meExpertReferee.UserGuid != ec.ExpertRefereeGuid)
        {
            object o = $"شما مجاز به کارشناسی این عملکرد نمیباشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        if (string.IsNullOrWhiteSpace(ExpertChoice))
        {
            return RedirectToAction(nameof(Performance), new { guid = PerformanceGuid });
        }


        if (ExpertChoice == "reject")
        {
            ec.Performance.Status = "reject";
            ec.IsRejected = true;
            ec.ExpertDescription = ExpertDescription;
            ec.ConsiderationDate = DateTime.Now;
            await assessmentDb.SaveChangesAsync();
        }
        else if (ExpertChoice == "accept")
        {
            if (Score != null && Score.HasValue && Score >= 1 && Score <= 10)
            {
                ec.IsRejected = false;
                ec.Performance.Status = "complete";
                ec.Score = Score.Value;
                ec.ExpertDescription = ExpertDescription;
                ec.ConsiderationDate = DateTime.Now;
                await assessmentDb.SaveChangesAsync();
                await assessmentProcess.SubmitScoreForStatics(performanceDbModel, Score.Value);
            }
            else
            {
                object o = $"امتیاز معتبر نمیباشد!";
                ViewBag.ResultState = "danger";
                return View("Result", o);
            }
        }

        //ec.ConsiderationDate = DateTime.Now;
        //await assessmentDb.SaveChangesAsync();

        //seed expert consideration
        await assessmentProcess.ExpertConsideration_UpdateSeed(ec);

        //update performance seed
        await assessmentProcess.Performance_UpdateSeed(performanceDbModel);

        return RedirectToAction(nameof(Index));
    }

    //***************** VP_Referrers *****************
    [Authorize(Roles = "VP_Referrers")]
    public async Task<IActionResult> SubmitExpertReferee(string PerformanceGuid, string ExpertUserName)
    {
        Assessment_PerformanceDbModel? performanceDbModel =
            await assessmentDb.Performances.FirstOrDefaultAsync(p => p.Guid == PerformanceGuid);
        if (performanceDbModel is null)
        {
            object o = "عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        MyIdentityUser meVpReferrer = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (!meVpReferrer.PerformanceField.Contains(performanceDbModel.PerformanceField))
        {
            object o = $"شما مجاز به ارجاع این عملکرد نمیباشید! فقط معاونت زمینه مربوطه مجاز به ارجاع میباشد.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }
        if (performanceDbModel.Status != "vp")
        {
            object o = "عملکرد در وضعیت بررسی معاون نمیباشد! فقط در وضعیت بررسی معاون مجاز به ارجاع میباشید.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        MyIdentityUser? expertReferee = await userManager.FindByNameAsync(ExpertUserName);
        if (expertReferee == null)
        {
            object o = $"کارشناس با نام کاربری \'{ExpertUserName}\' پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }
        if (!await userManager.IsInRoleAsync(expertReferee, "Expert_Referees") ||
            !expertReferee.PerformanceField.Contains(performanceDbModel.PerformanceField))
        {
            object o = $"کارشناس با نام کاربری \'{ExpertUserName}\' مجاز به کارشناسی در زمینه مربوطه نمیباشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        Assessment_ExpertConsiderationDbModel ec = new()
        {
            Guid = Guid.NewGuid().ToString().Replace("-", ""),

            Performance = performanceDbModel,

            VpReferrerGuid = meVpReferrer.UserGuid,
            ExpertRefereeGuid = expertReferee.UserGuid,

            ReferenceDate = DateTime.Today.ToString("yyyy/MM/dd", new CultureInfo("fa-IR")),
            ConsiderationDate = null,

            IsRejected = false,
            ExpertDescription = null,
            Score = 0,
        };
        await assessmentDb.ExpertConsiderations.AddAsync(ec);
        performanceDbModel.Status = "expert";
        await assessmentDb.SaveChangesAsync();

        // seed expert consideration
        await assessmentProcess.ExpertConsideration_UpdateSeed(ec);

        //update performance seed
        await assessmentProcess.Performance_UpdateSeed(performanceDbModel);

        //set notification
        await notification_Process.SetNewNotif_ToUser($"perf_{performanceDbModel.Guid}", expertReferee);

        return RedirectToAction(nameof(Index));
    }


    //***************** Performance_Confirmers *****************
    [Authorize(Roles = "Performance_Confirmers")]
    public async Task<IActionResult> ConfirmPerformnace(string performanceGuid)
    {
        Assessment_PerformanceDbModel? performanceDbModel =
            await assessmentDb.Performances.FirstOrDefaultAsync(p => p.Guid == performanceGuid);
        if (performanceDbModel is null)
        {
            object o = "عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        MyIdentityUser meConfirmer = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (performanceDbModel.Branch != meConfirmer.Branch)
        {
            object o = $"شما مجاز به تایید این عملکرد نمیباشید! فقط تایید کننده شعبه مجاز به تایید میباشد.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }
        if (performanceDbModel.Status != "confirm")
        {
            object o = "عملکرد در وضعیت تایید نمیباشد! فقط در وضعیت تایید مجاز به تایید میباشید.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        performanceDbModel.ConfirmerGuid = meConfirmer.UserGuid;
        performanceDbModel.ConfirmDate = DateTime.Today.ToString("yyyy/MM/dd", new CultureInfo("fa-IR"));
        performanceDbModel.Status = "vp";
        await assessmentDb.SaveChangesAsync();

        //seed performance
        await assessmentProcess.Performance_UpdateSeed(performanceDbModel);

        //set notification
        var vpReferrers = (await userManager.GetUsersInRoleAsync("VP_Referrers"))
        .Where(u => u.PerformanceField.Contains(performanceDbModel.PerformanceField));
        foreach (var vpReferrer in vpReferrers)
        {
            await notification_Process.SetNewNotif_ToUser($"perf_{performanceDbModel.Guid}", vpReferrer);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Performance_Confirmers")]
    public async Task<IActionResult> NotConfirmPerformnace(string performanceGuid)
    {
        Assessment_PerformanceDbModel? performanceDbModel =
            await assessmentDb.Performances.FirstOrDefaultAsync(p => p.Guid == performanceGuid);
        if (performanceDbModel is null)
        {
            object o = "عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        MyIdentityUser meConfirmer = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (performanceDbModel.Branch != meConfirmer.Branch)
        {
            object o = $"شما مجاز به تایید این عملکرد نمیباشید! فقط تایید کننده شعبه مجاز به تایید میباشد.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }
        if (performanceDbModel.Status != "confirm")
        {
            object o = "عملکرد در وضعیت تایید نمیباشد! فقط در وضعیت تایید مجاز به تایید میباشید.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        performanceDbModel.Status = "edit";
        await assessmentDb.SaveChangesAsync();

        //seed performance
        await assessmentProcess.Performance_UpdateSeed(performanceDbModel);

        return RedirectToAction(nameof(Index));
    }


    //***************** Performance_Registrars *****************
    [Authorize(Roles = "Performance_Registrars")]
    public IActionResult NewPerformance()
    {
        Assessment_NewPerformance newPerformanceModel = new()
        {
            performanceFields = configuration.GetSection("PerformanceFields").Get<List<string>>() ?? [],
            performanceTypes = configuration.GetSection("PerformanceTypes").Get<Dictionary<string, List<string>>>() ?? []
        };
        return View(newPerformanceModel);
    }

    [Authorize(Roles = "Performance_Registrars")]
    public async Task<IActionResult> SubmitNewPerformance(string type, string field, string? orderGuid = null)
    {
        List<string> performanceTypes = [.. configuration.GetSection("PerformanceTypes").Get<Dictionary<string, List<string>>>()?.Values.SelectMany(inner => inner) ?? []];
        if (!performanceTypes.Contains(type))
        {
            ModelState.AddModelError("performance type", "type is incorrect!");
            return BadRequest(ModelState);
        }
        if (!(configuration.GetSection("PerformanceFields").Get<List<string>>()?.Contains(field) ?? false))
        {
            ModelState.AddModelError("performance field", "field is incorrect!");
            return BadRequest(ModelState);
        }

        MyIdentityUser meSubmitter = (await userManager.FindByNameAsync(User.Identity!.Name!))!;

        if (string.IsNullOrWhiteSpace(meSubmitter.Branch) ||
            string.IsNullOrWhiteSpace(meSubmitter.Post))
        {
            object o = "اداره یا سمت اشتباه میباشد! لطفا به ادمین کاربران اطلاع دهید.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        Orders_DbModel? order = null;
        if (orderGuid is not null)
        {
            order = await ordersDb.Orders.FirstOrDefaultAsync(o => o.Guid == orderGuid);
        }

        var chiefs = (await userManager.GetUsersInRoleAsync("Branch_Chiefs"))
            .Where(u => u.Branch == meSubmitter.Branch && u.UserGuid != "admin")!;

        MyIdentityUser? chief = null;
        if (chiefs.Count() == 1)
        {
            chief = chiefs.First();
        }
        else if (chiefs.Count() == 0)
        {
            object o = "رئیس اداره مشخص نمیباشد! لطفا به ادمین مرکزی اطلاع دهید.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
            //return NotFound(o);
        }
        else if (chiefs.Count() > 1)
        {
            object o = "رئیس اداره بیش از یک نفر درنظر گرفته شده است! لطفا به ادمین مرکزی اطلاع دهید.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
            //return NotFound(o);
        }

        Assessment_PerformanceDbModel performanceDbModel = new()
        {
            OrderGuid = order?.Guid,
            PerformanceType = type,
            PerformanceField = field,
            Branch = meSubmitter.Branch,
            RegistrarGuid = meSubmitter.UserGuid,
            RegistrarPost = meSubmitter.Post,
            ChiefGuid = chief?.UserGuid ?? string.Empty,
            ChiefPost = chief?.Post ?? string.Empty
        };

        await assessmentDb.Performances.AddAsync(performanceDbModel);
        await assessmentDb.SaveChangesAsync();

        //seed performance
        await assessmentProcess.Performance_UpdateSeed(performanceDbModel);

        //archive old performances
        await assessmentProcess.ArchiveOlderPerformances(DateTime.Today - TimeSpan.FromDays(30));

        return RedirectToAction(nameof(EditPerformance), new { performanceGuid = performanceDbModel.Guid });
    }

    [Authorize(Roles = "Performance_Registrars")]
    [GenerateAntiforgeryTokenCookie]
    public async Task<IActionResult> EditPerformance(string performanceGuid)
    {
        Assessment_PerformanceDbModel? performanceDbModel = await assessmentDb.Performances
        .Include(p => p.ExpertConsiderations)
        .FirstOrDefaultAsync(p => p.Guid == performanceGuid);

        if (performanceDbModel is null)
        {
            object o = "عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        MyIdentityUser meEditor = (await userManager.FindByNameAsync(User.Identity!.Name!))!;

        if (meEditor.UserGuid == "admin")
        {
            performanceDbModel.Status = "vp";
            var scoredEc = performanceDbModel.ExpertConsiderations.FirstOrDefault(ec => ec.Score > 0);
            if (scoredEc != null)
            {
                //delete ec seed
                assessmentProcess.ExpertConsideration_DeleteSeed(scoredEc);

                await assessmentProcess.DeleteScoreForStatics(performanceDbModel, scoredEc.Score);
                assessmentDb.ExpertConsiderations.Remove(scoredEc);
            }

            //update performance seed
            await assessmentProcess.Performance_UpdateSeed(performanceDbModel);

            await assessmentDb.SaveChangesAsync();

            //set notification
            var vpReferrers = (await userManager.GetUsersInRoleAsync("VP_Referrers"))
            .Where(u => u.PerformanceField.Contains(performanceDbModel.PerformanceField));
            foreach (var vpReferrer in vpReferrers)
            {
                await notification_Process.SetNewNotif_ToUser($"perf_{performanceDbModel.Guid}", vpReferrer);
            }


            object o = "عملکرد در وضعیت بررسی معاون قرار گرفت.";
            ViewBag.ResultState = "info";
            return View("Result", o);
        }

        if (performanceDbModel.RegistrarGuid != meEditor.UserGuid)
        {
            object o = "شما مجاز به ویرایش این عملکرد نمیباشید! فقط ثبت کننده عملکرد مجاز به ویرایش میباشد.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }
        if (performanceDbModel.Status != "edit" && performanceDbModel.Status != "reject")
        {
            object o = "عملکرد در وضعیت ویرایش نمیباشد! فقط در وضعیت ویرایش مجاز به ویرایش میباشید.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        if (performanceDbModel.Status != "edit")
        {
            performanceDbModel.Status = "edit";
            await assessmentDb.SaveChangesAsync();

            //update performance seed
            await assessmentProcess.Performance_UpdateSeed(performanceDbModel);
        }

        if (configuration.GetSection("PerformanceTypes:مکاتبات").Get<List<string>>()
            ?.Contains(performanceDbModel.PerformanceType) ?? false)
        {
            var correspondenceForm = await assessmentProcess.GetCorrespondenceForm(performanceDbModel);
            return View("EditCorrespondence", correspondenceForm);
        }
        else
        {
            var performanceForm = await assessmentProcess.GetPerformanceForm(performanceDbModel);
            return View("EditPerformance", performanceForm);
        }
    }

    [Authorize(Roles = "Performance_Registrars")]
    [HttpPost]
    [DisableFormValueModelBinding]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> SubmitPerformanceForm()
    {
        Assessment_PerformanceForm formModel = new();
        List<string> uploadingFormFileNames = ["File"];
        var uploadLargeFile_Model = await uploadLargeFile.GetFormModelAndLargeFiles(HttpContext, formModel,
        uploadingFormFileNames, this);

        //string fileGuid = uploadLargeFile_Model.UploadedFileGuid;

        FileInfo? fileTempPathFileInfo = null;
        string? fileName = null;
        if (uploadLargeFile_Model.FormFileNameFullName.ContainsKey("File") &&
        !string.IsNullOrWhiteSpace(uploadLargeFile_Model.FormFileNameFullName["File"]))
        {
            fileTempPathFileInfo = new FileInfo(uploadLargeFile_Model.FormFileNameFullName["File"]);
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
                ModelState.AddModelError(kv.Key, kv.Value);
            }
            return BadRequest(ModelState);
        }

        Assessment_PerformanceDbModel? performanceDbModel =
        await assessmentDb.Performances.FirstOrDefaultAsync(p => p.Guid == formModel.Guid);

        if (performanceDbModel is null)
        {
            //***** delete attached file *****
            if (fileTempPathFileInfo?.Exists ?? false)
            {
                fileTempPathFileInfo.Directory?.Delete(true);
            }

            //object o = "عملکرد پیدا نشد! شماره سریال عملکرد اشتباه میباشد";
            //ViewBag.ResultState = "danger";
            //return View("Result", o);
            ModelState.AddModelError("performanceGuid", "عملکرد پیدا نشد! شماره سریال عملکرد اشتباه میباشد");
            return BadRequest(ModelState);
        }

        MyIdentityUser meSubmitter = (await userManager.FindByNameAsync(User.Identity!.Name!))!;

        if (meSubmitter.Branch != performanceDbModel.Branch ||
            meSubmitter.UserGuid != performanceDbModel.RegistrarGuid ||
            performanceDbModel.Status != "edit")
        {
            //***** delete attached file *****
            if (fileTempPathFileInfo?.Exists ?? false)
            {
                fileTempPathFileInfo.Directory?.Delete(true);
            }

            //object o = $"شما مجاز به ویرایش این عملکرد نمی باشید!";
            //ViewBag.ResultState = "danger";
            //return View("Result", o);
            ModelState.AddModelError("Access Denied", "شما مجاز به ویرایش این عملکرد نمی باشید!");
            return BadRequest(ModelState);
        }

        performanceDbModel.PerformDate = formModel.PerformDate;
        performanceDbModel.Subject = formModel.Subject;
        performanceDbModel.Attendees = formModel.Attendees;
        performanceDbModel.EventPlace = formModel.EventPlace;
        performanceDbModel.StartTime = formModel.StartTime;
        performanceDbModel.EndTime = formModel.EndTime;
        performanceDbModel.Description = formModel.Description;
        performanceDbModel.SecurityOpinion = formModel.SecurityOpinion;
        if (fileName is not null)
        {
            performanceDbModel.AttachedFileName = fileName;
        }

        await assessmentDb.SaveChangesAsync();

        if ((fileTempPathFileInfo?.Exists ?? false) && fileName is not null)
        {
            string fileDirPath = Path.Combine(env.ContentRootPath, "Storage", "Assessment", "Performance", formModel.Guid);
            if (Directory.Exists(fileDirPath))
            {
                Directory.Delete(fileDirPath, true);
            }
            var fileDirInfo = Directory.CreateDirectory(fileDirPath);
            System.IO.File.Move(fileTempPathFileInfo.FullName, Path.Combine(fileDirInfo.FullName, fileName));
            fileTempPathFileInfo.Directory?.Delete(true);
        }

        //seed performance
        await assessmentProcess.Performance_UpdateSeed(performanceDbModel);

        return Ok($"/Assessment/Performance?guid={formModel.Guid}");
    }

    [Authorize(Roles = "Performance_Registrars")]
    public async Task<IActionResult> DeleteAttachedFile(string performanceGuid)
    {
        Assessment_PerformanceDbModel? performanceDbModel =
            await assessmentDb.Performances.FirstOrDefaultAsync(p => p.Guid == performanceGuid);
        if (performanceDbModel is null)
        {
            object o = "عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        if (performanceDbModel.AttachedFileName == null)
        {
            object o = "فایل پیوست برای این عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        MyIdentityUser meDoingDelete = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (performanceDbModel.RegistrarGuid != meDoingDelete.UserGuid)
        {
            object o = "شما مجاز به حذف فایل پیوست این عملکرد نمیباشید! فقط ثبت کننده عملکرد مجاز به حذف میباشد.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }
        if (performanceDbModel.Status != "edit")
        {
            object o = "عملکرد در وضعیت ویرایش نمیباشد! فقط در وضعیت ویرایش مجاز به حذف فایل پیوست میباشید.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        string fileDirPath = Path.Combine(env.ContentRootPath, "Storage", "Assessment", "Performance", performanceDbModel.Guid);
        if (Directory.Exists(fileDirPath))
        {
            Directory.Delete(fileDirPath, true);
        }

        performanceDbModel.AttachedFileName = null;
        await assessmentDb.SaveChangesAsync();

        //seed performance
        await assessmentProcess.Performance_UpdateSeed(performanceDbModel);

        return RedirectToAction(nameof(EditPerformance), new { performanceGuid });
    }

    [Authorize(Roles = "Performance_Registrars")]
    [HttpPost]
    public async Task<IActionResult> SubmitCorrespondenceForm(Assessment_CorrespondenceForm formModel)
    {
        if (ModelState.IsValid)
        {
            Assessment_PerformanceDbModel? performanceDbModel =
            await assessmentDb.Performances.FirstOrDefaultAsync(p => p.Guid == formModel.Guid);
            if (performanceDbModel is null)
            {
                object o = "عملکرد پیدا نشد!";
                ViewBag.ResultState = "danger";
                return View("Result", o);
            }

            MyIdentityUser meSubmitter = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
            if (performanceDbModel.RegistrarGuid != meSubmitter.UserGuid)
            {
                object o = "شما مجاز به ویرایش این عملکرد نمیباشید! فقط ثبت کننده عملکرد مجاز به ویرایش میباشد.";
                ViewBag.ResultState = "danger";
                return View("Result", o);
            }
            if (performanceDbModel.Status != "edit")
            {
                object o = "عملکرد در وضعیت ویرایش نمیباشد! فقط در وضعیت ویرایش مجاز به ویرایش میباشید.";
                ViewBag.ResultState = "danger";
                return View("Result", o);
            }

            performanceDbModel.Description = formModel.Description;
            performanceDbModel.LetterSubject = formModel.LetterSubject;
            performanceDbModel.LetterDate = formModel.LetterDate;
            performanceDbModel.LetterNumber = formModel.LetterNumber;
            performanceDbModel.Receiver = formModel.Receiver;
            performanceDbModel.ReportDate = formModel.ReportDate;
            performanceDbModel.ReportNumber = formModel.ReportNumber;

            await assessmentDb.SaveChangesAsync();

            //seed performance
            await assessmentProcess.Performance_UpdateSeed(performanceDbModel);

            return RedirectToAction(nameof(Performance), new { guid = formModel.Guid });
        }
        return View(nameof(EditPerformance), formModel.Guid);
    }

    [Authorize(Roles = "Performance_Registrars")]
    public async Task<IActionResult> FinalSubmitPerformance(string performanceGuid)
    {
        Assessment_PerformanceDbModel? performanceDbModel =
            await assessmentDb.Performances.FirstOrDefaultAsync(p => p.Guid == performanceGuid);
        if (performanceDbModel is null)
        {
            object o = "عملکرد پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        MyIdentityUser meSubmitter = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (performanceDbModel.RegistrarGuid != meSubmitter.UserGuid)
        {
            object o = "شما مجاز به ثبت نهایی این عملکرد نمیباشید! فقط ثبت کننده عملکرد مجاز به ثبت نهایی میباشد.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }
        if (performanceDbModel.Status != "edit")
        {
            object o = "عملکرد در وضعیت ویرایش نمیباشد! فقط در وضعیت ویرایش مجاز به ثبت نهایی میباشید.";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        performanceDbModel.Status = "confirm";
        if (performanceDbModel.ExpertConsiderations.Count == 0)
        {
            performanceDbModel.RegisterDate = DateTime.Now;
        }
        await assessmentDb.SaveChangesAsync();

        //seed performance
        await assessmentProcess.Performance_UpdateSeed(performanceDbModel);

        //set notification
        var confirmers = (await userManager.GetUsersInRoleAsync("Performance_Confirmers"))
        .Where(u => u.Branch == meSubmitter.Branch);
        foreach (var confirmer in confirmers)
        {
            await notification_Process.SetNewNotif_ToUser($"perf_{performanceDbModel.Guid}", confirmer);
        }

        return RedirectToAction(nameof(Performance), new { guid = performanceGuid });
    }

    [Authorize(Roles = "Performance_Registrars")]
    public async Task<IActionResult> DeletePerformance(string performanceGuid)
    {
        Assessment_PerformanceDbModel? performanceDbModel =
            await assessmentDb.Performances
            .Include(p => p.ExpertConsiderations)
            .FirstOrDefaultAsync(p => p.Guid == performanceGuid);

        MyIdentityUser meDoingDelete = (await userManager.FindByNameAsync(User.Identity!.Name!))!;

        if (performanceDbModel?.Status == "edit" && performanceDbModel?.RegistrarGuid == meDoingDelete.UserGuid)
        {
            if (performanceDbModel.ExpertConsiderations.Count > 0)
            {
                performanceDbModel.Status = "deleted";
            }
            else
            {
                assessmentDb.Performances.Remove(performanceDbModel);

                //seed performance
                assessmentProcess.Performance_DeleteSeed(performanceDbModel);
                //here, performance doesn't have any expert consideration

                string fileDirPath = Path.Combine(env.ContentRootPath, "Storage", "Assessment", "Performance", performanceDbModel.Guid);
                if (Directory.Exists(fileDirPath))
                {
                    Directory.Delete(fileDirPath, true);
                }
            }
            await assessmentDb.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }


    public async Task<IActionResult> ScoreList(string? Branch, string? UserGuid, string? UserPost,
    string? FromDate, string? ToDate)
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;

        /*if ((Branch == null && UserGuid == null && UserPost == null) ||
            await userManager.IsInRoleAsync(me, "Can_Watch_Backgrounds") ||
            (await userManager.IsInRoleAsync(me, "Branch_Chiefs") && Branch == me.Branch))
        {*/
        Assessment_ScoreList scoreListModel = new()
        {
            Branch = Branch,
            MyUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == UserGuid),
            UserPost = UserPost,
            FromDate = FromDate,
            ToDate = ToDate
        };

        return View(scoreListModel);
        /*}
        else
        {
            object o = "شما مجوز مشاهده سوابق عملکرد کارمندان را ندارید!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }*/
    }


}