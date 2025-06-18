using System.ComponentModel;
using System.Globalization;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Components;

public class ScoreTableComponent(Assessment_Process assessmentProcess, Assessment_Info assessmentInfo,
    PersianDateProcess persianDateProcess, UserManager<MyIdentityUser> userManager) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string? branch = null,
        string? userGuid = null, string? userPost = null, string? fromDate = null,
        string? toDate = null, bool showSearch = true, bool isTopRecentBranches = false)
    {
        if (string.IsNullOrWhiteSpace(fromDate)) fromDate = null;
        if (string.IsNullOrWhiteSpace(toDate)) toDate = null;
        if (string.IsNullOrWhiteSpace(branch)) branch = null;
        if (string.IsNullOrWhiteSpace(userGuid)) userGuid = null;
        if (string.IsNullOrWhiteSpace(userPost)) userPost = null;

        List<Assessment_ScoreTable> scoreTables;
        if (isTopRecentBranches)
        {
            if (assessmentInfo.RecentTopBranchesScoreTable.Count() == 0)
            {
                await assessmentProcess.CalculateTopRecentStatics();
                Console.WriteLine($"************************* CalculateTopRecentStatics() ***********************");
            }
            scoreTables = assessmentInfo.RecentTopBranchesScoreTable.ToList();
            ViewBag.IsBranch = false;
            ViewBag.ShowSearchBox = false;
        }
        else
        {
            persianDateProcess.TryConvertToDateTime(fromDate, out DateTime? fromDateTime);
            persianDateProcess.TryConvertToDateTime(toDate, out DateTime? toDateTime);
            if (toDateTime != null) toDateTime = toDateTime.Value.AddHours(23).AddMinutes(59);

            scoreTables = await assessmentProcess
                .GetScoreTableList(branch, userGuid, userPost, fromDateTime, toDateTime);

            ViewBag.IsBranch = branch != null || userGuid != null;
            ViewBag.ShowSearchBox = showSearch;
            ViewBag.MeBranch = (await userManager.FindByNameAsync(User.Identity?.Name ?? ""))?.Branch;
        }

        return View(scoreTables.OrderByDescending(st => st.TotalScore).ToList());
    }
}