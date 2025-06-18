using System.ComponentModel;
using System.Globalization;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Components;

public class ScoreChartComponent(Assessment_Process assessmentProcess, PersianDateProcess persianDateProcess) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string? selectedBranch = null,
    string? selectedUserGuid = null, string? selectedPost = null, string? fromDate = null,
    string? toDate = null)
    {
        if (string.IsNullOrWhiteSpace(fromDate)) fromDate = null;
        if (string.IsNullOrWhiteSpace(toDate)) toDate = null;
        if (string.IsNullOrWhiteSpace(selectedBranch)) selectedBranch = null;
        if (string.IsNullOrWhiteSpace(selectedUserGuid)) selectedUserGuid = null;
        if (string.IsNullOrWhiteSpace(selectedPost)) selectedPost = null;

        persianDateProcess.TryConvertToDateTime(fromDate, out DateTime? fromDateTime);
        persianDateProcess.TryConvertToDateTime(toDate, out DateTime? toDateTime);
        if (toDateTime != null) toDateTime = toDateTime.Value.AddHours(23).AddMinutes(59);

        Assessment_ScoreChart scoreChart =
        await assessmentProcess.GetScoreChartList(selectedBranch, selectedUserGuid, selectedPost,
        fromDateTime, toDateTime);

        return View(scoreChart);
    }
}