using System.Globalization;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public class OrdersController : Controller
{
    readonly Orders_DbContext ordersDb;
    readonly UserManager<MyIdentityUser> userManager;
    readonly PersianDateProcess persianDateProcess;
    readonly Assessment_DbContext assessmentDb;
    readonly Orders_Process ordersProcess;
    readonly IConfiguration configuration;
    readonly Notification_Process notification_Process;
    readonly int NumberOfElementsPerPage = 3;
    public OrdersController(Orders_DbContext ordersDb, UserManager<MyIdentityUser> userManager,
    PersianDateProcess persianDateProcess, Assessment_DbContext assessmentDb, Orders_Process ordersProcess,
    IConfiguration configuration, Notification_Process notification_Process)
    {
        this.ordersDb = ordersDb;
        this.userManager = userManager;
        this.persianDateProcess = persianDateProcess;
        this.assessmentDb = assessmentDb;
        this.ordersProcess = ordersProcess;
        this.configuration = configuration;
        this.notification_Process = notification_Process;
    }

    public async Task<IActionResult> Index(string? Branch = null,
    string? FromDate = null, string? ToDate = null, int page = 1)
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        MyIdentityUser? registrar = null;

        persianDateProcess.TryConvertToDateTime(FromDate, out DateTime? FromDateTime);
        FromDateTime ??= DateTime.Today - TimeSpan.FromDays(30);
        persianDateProcess.TryConvertToDateTime(ToDate, out DateTime? ToDateTime);
        if (ToDateTime != null) ToDateTime = ToDateTime.Value.AddHours(23).AddMinutes(59);

        if (me.UserGuid == "admin")
        {
            registrar = null;
        }
        else if (await userManager.IsInRoleAsync(me, "Order_Registrars"))
        {
            registrar = me;
        }
        else if (!await userManager.IsInRoleAsync(me, "Can_Watch_Orders"))
        {
            Branch = me.Branch;
        }

        string commaSeparatedBranch = "," + Branch + ",";
        Orders_IndexModel indexModel = new()
        {
            Branch = Branch,
            Registrar = registrar,
            FromDate = FromDateTime.Value.ToString("yyyy/MM/dd", new CultureInfo("fa-IR")),
            ToDate = ToDate,
            Orders = await ordersDb.Orders
                .Where(o => (registrar == null || o.RegistrarGuid == registrar.UserGuid) &&
                    (Branch == null || o.BranchesCommaSeparated.Contains(commaSeparatedBranch)) &&
                    o.RegisterDate >= FromDateTime.Value &&
                    (ToDateTime == null || o.RegisterDate <= ToDateTime))
                .OrderByDescending(o => o.RegisterDate)
                .ToListAsync(),
            //.Where(o => Branch == null || o.Branches.Contains(Branch))
            //.ToList(),
        };

        if (Branch != null)
        {
            indexModel.OrderGuidRespondDates = [];
            foreach (Orders_DbModel order in indexModel.Orders)
            {
                var performances = await assessmentDb.Performances
                .Where(p => p.RegisterDate != null && p.Branch == Branch && p.OrderGuid == order.Guid)
                .ToListAsync();

                if (performances.Count > 0)
                {
                    indexModel.OrderGuidRespondDates[order.Guid] = performances
                    .OrderByDescending(p => p.RegisterDate!.Value)
                    .Select(p => p.RegisterDate!.Value)
                    .First();
                }
            }
        }

        //review notifs
        var allOrderGuids = indexModel.Orders.Select(o => o.Guid);
        for (int i = 0; i < me.Notifications.Count; i++)
        {
            string notifString = me.Notifications[i];
            if (notifString.Contains("order_"))
            {
                string orderGuid = notifString.Replace("order_", "");
                if (!allOrderGuids.Contains(orderGuid))
                {
                    await notification_Process.RemoveNotif_FromUser(notifString, me);
                    i--;
                }
            }
        }

        ViewBag.LastPage = (int)Math.Ceiling(indexModel.Orders.Count / (double)NumberOfElementsPerPage);

        indexModel.Orders = indexModel.Orders
        .Skip((page - 1) * NumberOfElementsPerPage)
        .Take(NumberOfElementsPerPage)
        .ToList();

        return View("List", indexModel);
    }

    public async Task<IActionResult> Open(string orderGuid)
    {
        Orders_DbModel? orderDbModel = await ordersDb.Orders.FirstOrDefaultAsync(o => o.Guid == orderGuid);
        if (orderDbModel is null)
        {
            object o = $"درخواست پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o);
        }

        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        if (await userManager.IsInRoleAsync(me, "Can_Watch_Orders") ||
            orderDbModel.Branches.Contains(me.Branch) ||
            me.UserGuid == orderDbModel.RegistrarGuid)
        {

            Orders_OpenModel openModel = new()
            {
                Deadline = orderDbModel.Deadline,
                Description = orderDbModel.Description,
                Guid = orderDbModel.Guid,
                MeOpener = me,
                PerformanceField = orderDbModel.PerformanceField,
                PerformanceType = orderDbModel.PerformanceType,
                RegisterDate = orderDbModel.RegisterDate,
                Registrar = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == orderDbModel.RegistrarGuid),
                Subject = orderDbModel.Subject,
            };
            foreach (string branch in orderDbModel.Branches)
            {
                var performances = await assessmentDb.Performances
                .Include(p => p.ExpertConsiderations)
                .Where(p => p.Branch == branch &&
                    p.OrderGuid == orderDbModel.Guid)
                .ToListAsync();
                openModel.BranchPerformances.Add(branch, performances);
            }

            //remove notif
            await notification_Process.RemoveNotif_FromUser($"order_{orderDbModel.Guid}", me);

            return View(openModel);
        }

        object o1 = "شما مجاز به مشاهده این درخواست نمیباشید!";
        ViewBag.ResultState = "danger";
        return View("Result", o1);
    }

    [Authorize(Roles = "Order_Registrars")]
    public async Task<IActionResult> NewOrder()
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        Orders_FormModel formModel = new();
        ViewBag.MeFullName = me.FullName;
        return View("Form", formModel);
    }

    [Authorize(Roles = "Order_Registrars")]
    public async Task<IActionResult> SubmitNewOrder(Orders_FormModel formModel)
    {
        if (ModelState.IsValid)
        {
            MyIdentityUser meSubmitter = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
            persianDateProcess.TryConvertToDateTime(formModel.Deadline, out DateTime? deadlineDateTime);
            if (deadlineDateTime is null)
            {
                ModelState.AddModelError("تاریخ", "مهلت انجام درخواست نادرست میباشد!");
                return View("Form", formModel);
            }
            deadlineDateTime = deadlineDateTime.Value.AddHours(23).AddMinutes(59);

            if (formModel.Branches.Length == 0)
            {
                ModelState.AddModelError("اداره ها", "حداقل یک اداره باید انتخاب شود!");
                return View("Form", formModel);
            }

            Orders_DbModel orderDbModel = new()
            {
                Branches = formModel.Branches.ToList(),
                Deadline = deadlineDateTime.Value,
                Description = formModel.Description,
                //Guid
                PerformanceField = formModel.PerformanceField,
                PerformanceType = formModel.PerformanceType,
                RegisterDate = DateTime.Now,
                RegistrarGuid = meSubmitter.UserGuid,
                Subject = formModel.Subject,
            };

            await ordersDb.Orders.AddAsync(orderDbModel);
            await ordersDb.SaveChangesAsync();

            //seed
            await ordersProcess.UpdateOrderSeed(orderDbModel);

            //notification
            foreach (MyIdentityUser user in await userManager.Users.Where(u => orderDbModel.Branches.Contains(u.Branch)).ToListAsync())
            {
                await notification_Process.SetNewNotif_ToUser($"order_{orderDbModel.Guid}", user);
            }

            //delete old orders
            await ordersProcess.RemoveOlderOrders(30);//30 days after deadline

            return RedirectToAction(nameof(Index));
        }
        return View("Form", formModel);
    }

    [Authorize(Roles = "Can_Watch_Orders")]
    public async Task<IActionResult> ReportPerformances(string? FromDate = null, string? ToDate = null)
    {
        persianDateProcess.TryConvertToDateTime(FromDate, out DateTime? FromDateTime);
        FromDateTime ??= DateTime.Today - TimeSpan.FromDays(30);
        persianDateProcess.TryConvertToDateTime(ToDate, out DateTime? ToDateTime);
        if (ToDateTime != null) ToDateTime = ToDateTime.Value.AddHours(23).AddMinutes(59);

        Orders_ReportModel reportModel = new()
        {
            FromDate = FromDateTime.Value.ToString("yyyy/MM/dd", new CultureInfo("fa-IR")),
            ToDate = ToDate,
            Orders = await ordersDb.Orders
                .Where(o => o.RegisterDate >= FromDateTime &&
                    (ToDateTime == null || o.RegisterDate <= ToDateTime))
                .OrderByDescending(o => o.RegisterDate)
                .ToListAsync(),
        };

        foreach (string branch in configuration.GetSection("AllBranches").Get<List<string>>() ?? [])
        {
            reportModel.BranchOrderGuidRespondDates[branch] = [];
            foreach (Orders_DbModel order in reportModel.Orders)
            {
                var performances = await assessmentDb.Performances
                .Where(p => p.Branch == branch && p.OrderGuid == order.Guid)
                .ToListAsync();

                if (performances.Count > 0)
                {
                    reportModel.BranchOrderGuidRespondDates[branch][order.Guid] = performances
                    .OrderByDescending(p => p.RegisterDate)
                    .Select(p => p.RegisterDate)
                    .First();
                }
                else if (order.Branches.Contains(branch))
                {
                    reportModel.BranchOrderGuidRespondDates[branch][order.Guid] = null;
                }
            }
        }

        return View("ReportOfPerformances", reportModel);
    }

}