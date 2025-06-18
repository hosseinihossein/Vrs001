using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Models;

public class Assessment_DbContext : DbContext
{
    public DbSet<Assessment_PerformanceDbModel> Performances { get; set; } = null!;
    public DbSet<Assessment_ExpertConsiderationDbModel> ExpertConsiderations { get; set; } = null!;
    public DbSet<Assessment_UserBranchPostStaticsDbModel> UserBranchPostStatics { get; set; } = null!;
    public DbSet<Assessment_EachMonthStaticsDbModel> EachMonthStatics { get; set; } = null!;
    public DbSet<Assessment_OldStaticsDbModel> OldStatics { get; set; } = null!;

    public Assessment_DbContext(DbContextOptions<Assessment_DbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //***** One-to-Many, performance to expert considerations *****
        modelBuilder.Entity<Assessment_PerformanceDbModel>()
        .HasMany<Assessment_ExpertConsiderationDbModel>(p => p.ExpertConsiderations)
        .WithOne(ec => ec.Performance)
        .HasForeignKey(ec => ec.PerformanceId)
        .IsRequired()
        .OnDelete(DeleteBehavior.Cascade);

        //***** One-to-Many, UserBranch to EachMonthStatics *****
        modelBuilder.Entity<Assessment_UserBranchPostStaticsDbModel>()
        .HasMany<Assessment_EachMonthStaticsDbModel>(ubp => ubp.EachMonthStatics)
        .WithOne(ms => ms.UserBranchPost)
        .HasForeignKey(ms => ms.UserBranchPostId)
        .IsRequired()
        .OnDelete(DeleteBehavior.Cascade);

        //***** One-to-One, UserBranch to OldStatics *****
        modelBuilder.Entity<Assessment_UserBranchPostStaticsDbModel>()
        .HasOne(ubp => ubp.OldStatics)
        .WithOne(os => os.UserBranchPost)
        .HasForeignKey<Assessment_OldStaticsDbModel>(os => os.UserBranchPostId)
        .IsRequired();
    }
}

public class Assessment_PerformanceDbModel
{
    public int Id { get; set; }
    public string Guid { get; set; } = System.Guid.NewGuid().ToString().Replace("-", "");
    public string? OrderGuid { get; set; } = null;

    public string PerformanceType { get; set; } = string.Empty;
    public string PerformanceField { get; set; } = string.Empty;

    //********* Dates *********
    public DateTime? RegisterDate { get; set; } = null;
    public string? PerformDate { get; set; } = null;
    public string? ConfirmDate { get; set; }

    public string Branch { get; set; } = string.Empty;
    public string RegistrarGuid { get; set; } = string.Empty;
    public string RegistrarPost { get; set; } = string.Empty;
    public string? ConfirmerGuid { get; set; }
    public string ChiefGuid { get; set; } = string.Empty;
    public string ChiefPost { get; set; } = string.Empty;

    public string Status { get; set; } = "edit";
    public string? Subject { get; set; } = null;
    public string? Attendees { get; set; } = null;
    public string? EventPlace { get; set; } = null;
    public string? StartTime { get; set; } = null;
    public string? EndTime { get; set; } = null;
    public string? Description { get; set; }
    public string? SecurityOpinion { get; set; } = null;
    public string? AttachedFileName { get; set; } = null;
    public List<Assessment_ExpertConsiderationDbModel> ExpertConsiderations { get; set; } = [];

    //********* Correspondence ***********
    public string? LetterSubject { get; set; } = null;
    public string? LetterDate { get; set; } = null;
    public string? LetterNumber { get; set; } = null;
    public string? Receiver { get; set; } = null;
    public string? ReportDate { get; set; } = null;
    public string? ReportNumber { get; set; } = null;

    //********** Archive **********
    public bool IsArchived { get; set; } = false;
}

public class Assessment_ExpertConsiderationDbModel
{
    public int Id { get; set; }
    public string Guid { get; set; } = null!;
    public int PerformanceId { get; set; }
    public Assessment_PerformanceDbModel Performance { get; set; } = null!;

    public string VpReferrerGuid { get; set; } = string.Empty;
    public string ExpertRefereeGuid { get; set; } = string.Empty;

    public string ReferenceDate { get; set; } = string.Empty;
    public DateTime? ConsiderationDate { get; set; } = null;

    public bool IsRejected { get; set; } = false;
    public string? ExpertDescription { get; set; } = null;
    public int Score { get; set; } = 0;

    //********** Archive **********
    //public bool IsArchived { get; set; } = false;
}

public class Assessment_UserBranchPostStaticsDbModel
{
    public int Id { get; set; }
    //public string Guid { get; set; } = System.Guid.NewGuid().ToString().Replace("-", "");
    public string UserGuid { get; set; } = string.Empty;
    public string UserPost { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public List<Assessment_EachMonthStaticsDbModel> EachMonthStatics { get; set; } = [];
    public Assessment_OldStaticsDbModel OldStatics { get; set; } = new();
}

public class Assessment_EachMonthStaticsDbModel
{
    public int Id { get; set; }

    public int UserBranchPostId { get; set; }
    public Assessment_UserBranchPostStaticsDbModel UserBranchPost { get; set; } = null!;

    //RegisterDateTime is when this EachMonthStatics is created by submitting the first performance for it
    public DateTime RegisterDateTime { get; set; } = DateTime.Now;
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public int TotalPerformances { get; set; }
}

public class Assessment_OldStaticsDbModel
{
    public int Id { get; set; }

    public int UserBranchPostId { get; set; }
    public Assessment_UserBranchPostStaticsDbModel UserBranchPost { get; set; } = null!;

    public int TotalScore { get; set; }
    public int TotalPerformances { get; set; }
    public int TotalMonths { get; set; }
}

//*****************************************************
public class Assessment_PerformanceList
{
    public string? Branch { get; set; } = null;
    public MyIdentityUser? MyUser { get; set; } = null;
    public string? UserPost { get; set; }
    public string? FromDate { get; set; } = null;
    public string? ToDate { get; set; } = null;
    public List<Assessment_Performance> Performances { get; set; } = [];
}

public class Assessment_ScoreList
{
    public string? Branch { get; set; } = null;
    public MyIdentityUser? MyUser { get; set; } = null;
    public string? UserPost { get; set; } = null;
    public string? FromDate { get; set; } = null;
    public string? ToDate { get; set; } = null;
}

public class Assessment_ScoreTable
{
    public MyIdentityUser? MyUser { get; set; }
    //public MyIdentityUser? BranchChief { get; set; }
    public string MyUserPost { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public int TotalPerformances { get; set; }
    public int TotalScore { get; set; }
    public int TotalMonths { get; set; }
}

public class Assessment_Performance
{
    public string Guid { get; set; } = string.Empty;
    public Orders_DbModel? Order { get; set; } = null;

    public string PerformanceType { get; set; } = string.Empty;
    public string PerformanceField { get; set; } = string.Empty;

    //********* Dates *********
    public string? RegisterDate { get; set; } = null;
    public string? PerformDate { get; set; }
    public string? ConfirmDate { get; set; }

    public string Branch { get; set; } = string.Empty;
    public MyIdentityUser? Registrar { get; set; }
    public string RegistrarPost { get; set; } = string.Empty;
    public MyIdentityUser? Confirmer { get; set; }
    public MyIdentityUser? Chief { get; set; }
    public string ChiefPost { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Attendees { get; set; }
    public string? EventPlace { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Description { get; set; }
    public string? SecurityOpinion { get; set; }
    public string? AttachedFileName { get; set; }
    public List<Assessment_ExpertConsideration> ExpertConsiderations { get; set; } = [];

    //********* Correspondence ***********
    public string? LetterSubject { get; set; }
    public string? LetterDate { get; set; }
    public string? LetterNumber { get; set; }
    public string? Receiver { get; set; }
    public string? ReportDate { get; set; }
    public string? ReportNumber { get; set; }
}

public class Assessment_ExpertConsideration
{
    public string Guid { get; set; } = string.Empty;

    public MyIdentityUser? VpReferrer { get; set; }
    public MyIdentityUser? ExpertReferee { get; set; }

    public string ReferenceDate { get; set; } = string.Empty;
    public string? ConsiderationDate { get; set; }

    public bool IsRejected { get; set; } = false;
    public string? ExpertDescription { get; set; }
    public int Score { get; set; } = 0;
}

public class Assessment_ScoreChart
{
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
    public string? Branch { get; set; }
    public MyIdentityUser? MyUser { get; set; }
    public string? UserPost { get; set; }
    public Dictionary<string, int> DateScore { get; set; } = [];
}

public class Assessment_NewPerformance
{
    public List<string> performanceFields { get; set; } = [];
    public Dictionary<string, List<string>> performanceTypes { get; set; } = [];
}


public class Assessment_PerformanceForm
{
    public string Guid { get; set; } = string.Empty;

    public string? PerformanceType { get; set; } = null;
    public string? PerformanceField { get; set; } = null;

    //********* Dates *********
    public string? RegisterDate { get; set; } = null;
    public string? PerformDate { get; set; } = string.Empty;

    public string? Branch { get; set; } = null;
    public string? RegistrarFullName { get; set; } = null;
    public string? RegistrarPost { get; set; }
    public string? ChiefFullName { get; set; } = null;
    public string? ChiefPost { get; set; }
    public string? Subject { get; set; }
    public string? Attendees { get; set; }
    public string? EventPlace { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Description { get; set; }
    public string? SecurityOpinion { get; set; }
    public string? AttachedFileName { get; set; } = null;
    //public IFormFile? AttachedFile { get; set; } = null;
}

public class Assessment_CorrespondenceForm
{
    public string Guid { get; set; } = string.Empty;

    public string? PerformanceType { get; set; } = null;
    public string? PerformanceField { get; set; } = null;

    //********* Dates *********
    public string? RegisterDate { get; set; } = null;

    public string? Branch { get; set; } = null;
    public string? RegistrarFullName { get; set; } = null;
    public string? RegistrarPost { get; set; }
    public string? ChiefFullName { get; set; } = null;
    public string? ChiefPost { get; set; }
    public string? Description { get; set; }

    //********* Correspondence ***********
    public string? LetterSubject { get; set; }
    public string? LetterDate { get; set; }
    public string? LetterNumber { get; set; }
    public string? Receiver { get; set; }
    public string? ReportDate { get; set; }
    public string? ReportNumber { get; set; }
}



public class Assessment_PerformanceSeedModel
{
    public string Guid { get; set; } = System.Guid.NewGuid().ToString().Replace("-", "");
    public string? OrderGuid { get; set; } = null;

    public string PerformanceType { get; set; } = string.Empty;
    public string PerformanceField { get; set; } = string.Empty;

    //********* Dates *********
    public DateTime? RegisterDate { get; set; } = null;
    public string? PerformDate { get; set; } = null;
    public string? ConfirmDate { get; set; }

    public string Branch { get; set; } = string.Empty;
    public string RegistrarGuid { get; set; } = string.Empty;
    public string RegistrarPost { get; set; } = string.Empty;
    public string? ConfirmerGuid { get; set; }
    public string ChiefGuid { get; set; } = string.Empty;
    public string ChiefPost { get; set; } = string.Empty;

    public string Status { get; set; } = "edit";
    public string? Subject { get; set; } = null;
    public string? Attendees { get; set; } = null;
    public string? EventPlace { get; set; } = null;
    public string? StartTime { get; set; } = null;
    public string? EndTime { get; set; } = null;
    public string? Description { get; set; }
    public string? SecurityOpinion { get; set; } = null;
    public string? AttachedFileName { get; set; } = null;
    public string[] ExpertConsiderationsGuids { get; set; } = [];

    //********* Correspondence ***********
    public string? LetterSubject { get; set; } = null;
    public string? LetterDate { get; set; } = null;
    public string? LetterNumber { get; set; } = null;
    public string? Receiver { get; set; } = null;
    public string? ReportDate { get; set; } = null;
    public string? ReportNumber { get; set; } = null;

    //********** Archive **********
    public bool IsArchived { get; set; } = false;
}

public class Assessment_ExpertConsiderationSeedModel
{
    public string Guid { get; set; } = null!;
    public string PerformanceGuid { get; set; } = string.Empty;

    public string VpReferrerGuid { get; set; } = string.Empty;
    public string ExpertRefereeGuid { get; set; } = string.Empty;

    public string ReferenceDate { get; set; } = string.Empty;
    public DateTime? ConsiderationDate { get; set; } = null;

    public bool IsRejected { get; set; } = false;
    public string? RejectDescription { get; set; } = null;
    public int Score { get; set; } = 0;

    //********** Archive **********
    //public bool IsArchived { get; set; } = false;
}
/*
public class Assessment_UserBarnchPostStaticsSeedModel
{
    public string Guid { get; set; } = string.Empty;
    public string UserGuid { get; set; } = string.Empty;
    public string UserPost { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
}

public class Assessment_EachMonthStaticsSeedModel
{
    public string Guid { get; set; } = string.Empty;
    public string UserBranchPostGuid { get; set; } = string.Empty;

    public DateTime RegistrarDateTime { get; set; } = DateTime.Today;
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public int TotalPerformances { get; set; }
}
*/

//singleton service
public class Assessment_Info
{
    public readonly Dictionary<string, string> PerformanceStatus = new(){
        {"edit", "در حال ویرایش"},
        {"confirm", "در انتظار تایید"},
        {"vp", "بررسی معاون"},
        {"expert", "بررسی کارشناس"},
        {"complete", "تکمیل"},
        {"reject", "مرجوع"},
        {"deleted","حذف توسط ثبت کننده"}
    };

    public ConcurrentQueue<Assessment_ScoreTable> RecentTopBranchesScoreTable = [];

    public DateTime? LastTimeDoneArchive = null;
    public DateTime? LastTimeDoneReconsideringOldStatics = null;

}

//scoped service
public class Assessment_Process
{
    readonly Assessment_DbContext assessment_Db;
    readonly UserManager<MyIdentityUser> userManager;
    //readonly Account_Info accountInfo;
    readonly Assessment_Info assessment_Info;
    readonly IWebHostEnvironment env;
    readonly DirectoryInfo Performance_SeedDirectoryInfo;
    readonly DirectoryInfo ExpertConsideration_SeedDirectoryInfo;
    //readonly DirectoryInfo UserBranchPostStatics_SeedDirectoryInfo;
    //readonly DirectoryInfo EachMonthStatics_SeedDirectoryInfo;
    readonly Account_Process accountProcess;
    readonly Orders_DbContext ordersDb;
    readonly Orders_Process ordersProcess;
    const int ConsideredOldDays = 365;//Days in One Year
    readonly IConfiguration configuration;
    readonly List<string> ChiefPosts = [];


    public Assessment_Process(Assessment_DbContext _assessment_Db, UserManager<MyIdentityUser> _userManager,
    /*Account_Info _accountInfo,*/ Assessment_Info _assessment_Info, IWebHostEnvironment env,
    Account_Process accountProcess, Orders_DbContext ordersDb, Orders_Process ordersProcess, IConfiguration configuration)
    {
        assessment_Db = _assessment_Db;
        userManager = _userManager;
        //accountInfo = _accountInfo;
        assessment_Info = _assessment_Info;
        this.env = env;
        this.accountProcess = accountProcess;
        this.ordersDb = ordersDb;
        this.ordersProcess = ordersProcess;
        this.configuration = configuration;

        ChiefPosts = configuration.GetSection("PerformanceChiefPostInclude").Get<List<string>>() ?? [];
        Performance_SeedDirectoryInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Storage", "Assessment", "Performance", "DbSeed"));
        ExpertConsideration_SeedDirectoryInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Storage", "Assessment", "ExpertConsideration", "DbSeed"));
        //UserBranchPostStatics_SeedDirectoryInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Storage", "Assessment", "UserBranchPostStatics", "DbSeed"));
        //EachMonthStatics_SeedDirectoryInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Storage", "Assessment", "EachMonthStatics", "DbSeed"));
    }

    public async Task<Assessment_Performance?> GetPerformance(string performanceGuid)
    {
        Assessment_PerformanceDbModel? dbModel =
        await assessment_Db.Performances
        .Include(p => p.ExpertConsiderations)
        .FirstOrDefaultAsync(p => p.Guid == performanceGuid);

        if (dbModel is null) return null;

        Assessment_Performance performance = new()
        {
            Guid = dbModel.Guid,
            Order = await ordersDb.Orders.FirstOrDefaultAsync(o => o.Guid == dbModel.OrderGuid),
            PerformanceType = dbModel.PerformanceType,
            PerformanceField = dbModel.PerformanceField,

            //********* Dates *********
            RegisterDate = dbModel.RegisterDate?.ToString("yyyy/MM/dd", new CultureInfo("fa-IR")),
            PerformDate = dbModel.PerformDate,
            ConfirmDate = dbModel.ConfirmDate,

            Branch = dbModel.Branch,
            Registrar = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == dbModel.RegistrarGuid),
            RegistrarPost = dbModel.RegistrarPost,
            Confirmer = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == dbModel.ConfirmerGuid),
            Chief = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == dbModel.ChiefGuid),
            ChiefPost = dbModel.ChiefPost,
            Status = dbModel.Status,
            Subject = dbModel.Subject,
            Attendees = dbModel.Attendees,
            EventPlace = dbModel.EventPlace,
            StartTime = dbModel.StartTime,
            EndTime = dbModel.EndTime,
            Description = dbModel.Description,
            SecurityOpinion = dbModel.SecurityOpinion,
            AttachedFileName = dbModel.AttachedFileName,

            //********* Correspondence ***********
            LetterSubject = dbModel.LetterSubject,
            LetterDate = dbModel.LetterDate,
            LetterNumber = dbModel.LetterNumber,
            Receiver = dbModel.Receiver,
            ReportDate = dbModel.ReportDate,
            ReportNumber = dbModel.ReportNumber
        };

        foreach (var ecdb in dbModel.ExpertConsiderations)
        {
            performance.ExpertConsiderations.Add(await GetExpertConsideration(ecdb));
        }

        return performance;
    }

    public async Task<Assessment_Performance?> GetPerformance(Assessment_PerformanceDbModel performanceDbModel)
    {
        /*Assessment_PerformanceDbModel? performanceDbModel =
        await assessment_Db.Performances
        .Include(p => p.ExpertConsiderations)
        .FirstOrDefaultAsync(p => p.Guid == performanceGuid);*/

        if (performanceDbModel is null) return null;

        Assessment_Performance performance = new()
        {
            Guid = performanceDbModel.Guid,
            Order = await ordersDb.Orders.FirstOrDefaultAsync(o => o.Guid == performanceDbModel.OrderGuid),
            PerformanceType = performanceDbModel.PerformanceType,
            PerformanceField = performanceDbModel.PerformanceField,

            //********* Dates *********
            RegisterDate = performanceDbModel.RegisterDate?.ToString("yyyy/MM/dd", new CultureInfo("fa-IR")),
            PerformDate = performanceDbModel.PerformDate,
            ConfirmDate = performanceDbModel.ConfirmDate,

            Branch = performanceDbModel.Branch,
            Registrar = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == performanceDbModel.RegistrarGuid),
            RegistrarPost = performanceDbModel.RegistrarPost,
            Confirmer = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == performanceDbModel.ConfirmerGuid),
            Chief = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == performanceDbModel.ChiefGuid),
            ChiefPost = performanceDbModel.ChiefPost,
            Status = performanceDbModel.Status,
            Subject = performanceDbModel.Subject,
            Attendees = performanceDbModel.Attendees,
            EventPlace = performanceDbModel.EventPlace,
            StartTime = performanceDbModel.StartTime,
            EndTime = performanceDbModel.EndTime,
            Description = performanceDbModel.Description,
            SecurityOpinion = performanceDbModel.SecurityOpinion,
            AttachedFileName = performanceDbModel.AttachedFileName,

            //********* Correspondence ***********
            LetterSubject = performanceDbModel.LetterSubject,
            LetterDate = performanceDbModel.LetterDate,
            LetterNumber = performanceDbModel.LetterNumber,
            Receiver = performanceDbModel.Receiver,
            ReportDate = performanceDbModel.ReportDate,
            ReportNumber = performanceDbModel.ReportNumber
        };

        /*if (performanceDbModel.OrderGuid != null && performance.Order == null)
        {
            performance.Order = await ordersProcess.GetArchivedOrder(performanceDbModel.OrderGuid);
        }*/

        foreach (var ecdb in performanceDbModel.ExpertConsiderations)
        {
            performance.ExpertConsiderations.Add(await GetExpertConsideration(ecdb));
        }

        return performance;
    }

    private async Task<Assessment_ExpertConsideration> GetExpertConsideration(Assessment_ExpertConsiderationDbModel dbModel)
    {
        Assessment_ExpertConsideration ec = new()
        {
            Guid = dbModel.Guid,

            VpReferrer = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == dbModel.VpReferrerGuid),
            ExpertReferee = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == dbModel.ExpertRefereeGuid),

            ReferenceDate = dbModel.ReferenceDate,
            ConsiderationDate = dbModel.ConsiderationDate?.ToString("yyyy/MM/dd", new CultureInfo("fa-IR")),

            IsRejected = dbModel.IsRejected,
            ExpertDescription = dbModel.ExpertDescription,
            Score = dbModel.Score
        };

        return ec;
    }

    public async Task<Assessment_PerformanceForm?> GetPerformanceForm(Assessment_PerformanceDbModel performanceDbModel)
    {
        /*Assessment_PerformanceDbModel? performanceDbModel =
        await assessment_Db.Performances
        .FirstOrDefaultAsync(p => p.Guid == performanceGuid);*/

        if (performanceDbModel is null) return null;

        var chief = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == performanceDbModel.ChiefGuid);
        var registrar = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == performanceDbModel.RegistrarGuid);
        Assessment_PerformanceForm performanceForm = new()
        {
            Guid = performanceDbModel.Guid,
            PerformanceType = performanceDbModel.PerformanceType,
            PerformanceField = performanceDbModel.PerformanceField,

            //********* Dates *********
            RegisterDate = performanceDbModel.RegisterDate?.ToString("yyyy/MM/dd", new CultureInfo("fa-IR")),
            PerformDate = performanceDbModel.PerformDate ?? string.Empty,

            ChiefFullName = chief?.FullName,
            ChiefPost = chief?.Post,
            RegistrarPost = registrar?.Post,
            Branch = performanceDbModel.Branch,
            RegistrarFullName = registrar?.FullName,
            Subject = performanceDbModel.Subject ?? string.Empty,
            Attendees = performanceDbModel.Attendees ?? string.Empty,
            EventPlace = performanceDbModel.EventPlace ?? string.Empty,
            StartTime = performanceDbModel.StartTime ?? string.Empty,
            EndTime = performanceDbModel.EndTime ?? string.Empty,
            Description = performanceDbModel.Description,
            SecurityOpinion = performanceDbModel.SecurityOpinion ?? string.Empty,
            AttachedFileName = performanceDbModel.AttachedFileName,
        };

        return performanceForm;
    }

    public async Task<Assessment_CorrespondenceForm?> GetCorrespondenceForm(Assessment_PerformanceDbModel performanceDbModel)
    {
        /*Assessment_PerformanceDbModel? performanceDbModel =
        await assessment_Db.Performances
        .FirstOrDefaultAsync(p => p.Guid == performanceGuid);*/

        if (performanceDbModel is null) return null;

        var chief = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == performanceDbModel.ChiefGuid);
        var registrar = await userManager.Users.FirstOrDefaultAsync(user => user.UserGuid == performanceDbModel.RegistrarGuid);
        Assessment_CorrespondenceForm correspondenceForm = new()
        {
            Guid = performanceDbModel.Guid,
            PerformanceType = performanceDbModel.PerformanceType,
            PerformanceField = performanceDbModel.PerformanceField,

            //********* Dates *********
            RegisterDate = performanceDbModel.RegisterDate?.ToString("yyyy/MM/dd", new CultureInfo("fa-IR")),

            ChiefFullName = chief?.FullName,
            ChiefPost = chief?.Post,
            RegistrarPost = registrar?.Post,
            Branch = performanceDbModel.Branch,
            RegistrarFullName = registrar?.FullName,
            Description = performanceDbModel.Description,

            //********* Correspondence ***********
            LetterSubject = performanceDbModel.LetterSubject ?? string.Empty,
            LetterDate = performanceDbModel.LetterDate ?? string.Empty,
            LetterNumber = performanceDbModel.LetterNumber ?? string.Empty,
            Receiver = performanceDbModel.Receiver ?? string.Empty,
            ReportDate = performanceDbModel.ReportDate ?? string.Empty,
            ReportNumber = performanceDbModel.ReportNumber ?? string.Empty
        };

        return correspondenceForm;
    }

    //**************** Score Table ****************
    public async Task<List<Assessment_ScoreTable>> GetScoreTableList(string? selectedBranch = null,
    string? selectedUserGuid = null, string? selectedPost = null, DateTime? fromDateTime = null,
    DateTime? toDateTime = null)
    {
        List<Assessment_ScoreTable> scoreTables = [];

        MyIdentityUser? selectedUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
        List<MyIdentityUser> allRelatedUsers = await accountProcess.GetAllRelatedUsers(selectedUser);

        if (allRelatedUsers.Count > 0)
        {
            foreach (MyIdentityUser user in allRelatedUsers)
            {
                selectedUserGuid = user.UserGuid;

                if (await assessment_Db.UserBranchPostStatics.AnyAsync(ub =>
                (selectedBranch == null || ub.Branch == selectedBranch) &&
                (selectedUserGuid == null || ub.UserGuid == selectedUserGuid) &&
                (selectedPost == null || ub.UserPost == selectedPost)))
                {
                    IEnumerable<IGrouping<string, Assessment_UserBranchPostStaticsDbModel>>? UbGrouped;
                    if (selectedBranch == null && selectedUserGuid == null && selectedPost == null)
                    {
                        UbGrouped = (await assessment_Db.UserBranchPostStatics
                            .Include(ub => ub.EachMonthStatics)
                            .Include(ubp => ubp.OldStatics)
                            .Where(ubp =>
                                (!string.IsNullOrWhiteSpace(ChiefPosts[0]) && ubp.UserPost.Contains(ChiefPosts[0])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[1]) && ubp.UserPost.Contains(ChiefPosts[1])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[2]) && ubp.UserPost.Contains(ChiefPosts[2])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[3]) && ubp.UserPost.Contains(ChiefPosts[3])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[4]) && ubp.UserPost.Contains(ChiefPosts[4])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[5]) && ubp.UserPost.Contains(ChiefPosts[5])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[6]) && ubp.UserPost.Contains(ChiefPosts[6])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[7]) && ubp.UserPost.Contains(ChiefPosts[7])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[8]) && ubp.UserPost.Contains(ChiefPosts[8])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[9]) && ubp.UserPost.Contains(ChiefPosts[9])))
                            .ToListAsync())
                            .GroupBy(ub => ub.Branch);
                    }
                    else
                    {
                        UbGrouped = (await assessment_Db.UserBranchPostStatics
                            .Include(ub => ub.EachMonthStatics)
                            .Include(ubp => ubp.OldStatics)
                            .Where(ub => (selectedBranch == null || ub.Branch == selectedBranch) &&
                                (selectedUserGuid == null || selectedUserGuid == ub.UserGuid) &&
                                (selectedPost == null || selectedPost == ub.UserPost))
                            .ToListAsync())
                            .GroupBy(ubp => (selectedBranch == null ? ubp.Branch : string.Empty) +
                                (selectedUserGuid == null ? ubp.UserGuid : string.Empty) +
                                (selectedPost == null ? ubp.UserPost : string.Empty));
                    }

                    foreach (var g in UbGrouped)
                    {
                        var allSelectedMonths = g
                        .SelectMany(ub => ub.EachMonthStatics)
                        .Where(ms => (fromDateTime == null || fromDateTime.Value <= ms.RegisterDateTime) &&
                            (toDateTime == null || toDateTime.Value >= ms.RegisterDateTime));

                        string eachBranch = g.First().Branch;
                        string eachPost = g.First().UserPost;
                        MyIdentityUser? eachUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == g.First().UserGuid);

                        Assessment_ScoreTable scoreTable = new()
                        {
                            Branch = selectedBranch ?? eachBranch,
                            TotalPerformances = allSelectedMonths
                                    .Sum(ems => ems.TotalPerformances),
                            TotalScore = allSelectedMonths
                                    .Sum(ems => ems.TotalScore),
                            TotalMonths = allSelectedMonths.Count(),
                            MyUser = selectedUser ?? eachUser,
                            MyUserPost = selectedPost ?? eachPost
                        };

                        if (fromDateTime == null && toDateTime == null)
                        {
                            scoreTable.TotalScore += g.Sum(ubp => ubp.OldStatics.TotalScore);
                            scoreTable.TotalPerformances += g.Sum(ubp => ubp.OldStatics.TotalPerformances);
                            scoreTable.TotalMonths += g.Sum(ubp => ubp.OldStatics.TotalMonths);
                        }

                        if (scoreTable.TotalPerformances > 0)
                        {
                            scoreTables.Add(scoreTable);
                        }
                    }

                }
                /*
                    if (selectedBranch == null && selectedUserGuid == null && selectedPost == null)
                    {
                        if (await assessment_Db.UserBranchPostStatics.AnyAsync())
                        {
                            var UbGroupedByBranch = (await assessment_Db.UserBranchPostStatics
                                    .Include(ub => ub.EachMonthStatics)
                                    .Include(ubp => ubp.OldStatics)
                                    .Where(ubp => ChiefPosts.Any(cp => ubp.UserPost.Contains(cp)))
                                    .ToListAsync())
                                    .GroupBy(ub => ub.Branch);

                            foreach (var g in UbGroupedByBranch)
                            {
                                var allSelectedMonths = g
                                    .SelectMany(ub => ub.EachMonthStatics)
                                    .Where(ms => (fromDateTime == null || fromDateTime >= ms.RegisterDateTime) &&
                                        (toDateTime == null || toDateTime <= ms.RegisterDateTime));

                                var branch = g.Key;

                                Assessment_ScoreTable scoreTable = new()
                                {
                                    Branch = branch,
                                    TotalPerformances = allSelectedMonths
                                        .Sum(ms => ms.TotalPerformances),
                                    TotalScore = allSelectedMonths
                                        .Sum(ms => ms.TotalScore),
                                    TotalMonths = allSelectedMonths.Count()
                                };

                                if (fromDateTime == null && toDateTime == null)
                                {
                                    scoreTable.TotalScore += g.Sum(ubp => ubp.OldStatics.TotalScore);
                                    scoreTable.TotalPerformances += g.Sum(ubp => ubp.OldStatics.TotalPerformances);
                                    scoreTable.TotalMonths += g.Sum(ubp => ubp.OldStatics.TotalMonths);
                                }

                                if (scoreTable.TotalPerformances > 0)
                                {
                                    scoreTables.Add(scoreTable);
                                }
                            }
                        }
                    }
                    else if (selectedBranch is not null && selectedUserGuid == null && selectedPost == null)
                    {
                        if (await assessment_Db.UserBranchPostStatics.AnyAsync(ub => ub.Branch == selectedBranch))
                        {
                            var UbGroupedByUserGuidPost = (await assessment_Db.UserBranchPostStatics
                                .Include(ub => ub.EachMonthStatics)
                                .Where(ub => ub.Branch == selectedBranch)
                                .ToListAsync())
                                .GroupBy(ub => ub.UserGuid + ub.UserPost);

                            foreach (var g in UbGroupedByUserGuidPost)
                            {
                                var allSelectedMonths = g
                                    .SelectMany(ub => ub.EachMonthStatics)
                                    .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                        ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue));

                                MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == g.First().UserGuid);
                                string myUserPost = g.First().UserPost;

                                Assessment_ScoreTable scoreTable = new()
                                {
                                    Branch = selectedBranch,
                                    TotalPerformances = allSelectedMonths
                                        .Sum(ms => ms.TotalPerformances),
                                    TotalScore = allSelectedMonths
                                        .Sum(ms => ms.TotalScore),
                                    TotalMonths = allSelectedMonths.Count(),
                                    MyUser = myUser,
                                    MyUserPost = myUserPost
                                };
                                if (scoreTable.TotalPerformances > 0)
                                {
                                    scoreTables.Add(scoreTable);
                                }
                            }

                        }
                    }
                    else if (selectedBranch == null && selectedUserGuid is not null && selectedPost == null)
                    {
                        if (await assessment_Db.UserBranchPostStatics.AnyAsync(ub => ub.UserGuid == selectedUserGuid))
                        {
                            var UbGroupedByBranchPost = (await assessment_Db.UserBranchPostStatics
                                .Include(ub => ub.EachMonthStatics)
                                .Where(ub => ub.UserGuid == selectedUserGuid)
                                .ToListAsync())
                                .GroupBy(ub => ub.Branch + ub.UserPost);

                            MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
                            foreach (var g in UbGroupedByBranchPost)
                            {
                                var allSelectedMonths = g
                                    .SelectMany(ub => ub.EachMonthStatics)
                                    .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                        ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue));

                                //MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
                                string myUserPost = g.First().UserPost;
                                string branch = g.First().Branch;

                                Assessment_ScoreTable scoreTable = new()
                                {
                                    Branch = branch,
                                    TotalPerformances = allSelectedMonths
                                        .Sum(ms => ms.TotalPerformances),
                                    TotalScore = allSelectedMonths
                                        .Sum(ms => ms.TotalScore),
                                    TotalMonths = allSelectedMonths.Count(),
                                    MyUser = myUser,
                                    MyUserPost = myUserPost
                                };
                                if (scoreTable.TotalPerformances > 0)
                                {
                                    scoreTables.Add(scoreTable);
                                }
                            }

                        }
                    }
                    else if (selectedBranch is not null && selectedUserGuid is not null && selectedPost == null)
                    {
                        if (await assessment_Db.UserBranchPostStatics
                            .AnyAsync(ub => ub.UserGuid == selectedUserGuid && ub.Branch == selectedBranch))
                        {
                            var UbGroupedByPost = (await assessment_Db.UserBranchPostStatics
                                .Include(ub => ub.EachMonthStatics)
                                .Where(ub => ub.UserGuid == selectedUserGuid && ub.Branch == selectedBranch)
                                .ToListAsync())
                                .GroupBy(ub => ub.UserPost);

                            MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
                            foreach (var g in UbGroupedByPost)
                            {
                                var allSelectedMonths = g
                                    .SelectMany(ub => ub.EachMonthStatics)
                                    .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                        ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue));

                                //MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
                                string myUserPost = g.First().UserPost;
                                //string branch = g.First().Branch;

                                Assessment_ScoreTable scoreTable = new()
                                {
                                    Branch = selectedBranch,
                                    TotalPerformances = allSelectedMonths
                                        .Sum(ms => ms.TotalPerformances),
                                    TotalScore = allSelectedMonths
                                        .Sum(ms => ms.TotalScore),
                                    TotalMonths = allSelectedMonths.Count(),
                                    MyUser = myUser,
                                    MyUserPost = myUserPost
                                };
                                if (scoreTable.TotalPerformances > 0)
                                {
                                    scoreTables.Add(scoreTable);
                                }
                            }

                        }
                    }
                    else if (selectedBranch is not null && selectedUserGuid == null && selectedPost is not null)
                    {
                        if (await assessment_Db.UserBranchPostStatics
                            .AnyAsync(ub => ub.UserPost == selectedPost && ub.Branch == selectedBranch))
                        {
                            var UbGroupedByUser = (await assessment_Db.UserBranchPostStatics
                                .Include(ub => ub.EachMonthStatics)
                                .Where(ub => ub.UserPost == selectedPost && ub.Branch == selectedBranch)
                                .ToListAsync())
                                .GroupBy(ub => ub.UserGuid);

                            foreach (var g in UbGroupedByUser)
                            {
                                var allSelectedMonths = g
                                    .SelectMany(ub => ub.EachMonthStatics)
                                    .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                        ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue));

                                MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == g.First().UserGuid);
                                //string myUserPost = g.First().UserPost;
                                //string branch = g.First().Branch;

                                Assessment_ScoreTable scoreTable = new()
                                {
                                    Branch = selectedBranch,
                                    TotalPerformances = allSelectedMonths
                                        .Sum(ms => ms.TotalPerformances),
                                    TotalScore = allSelectedMonths
                                        .Sum(ms => ms.TotalScore),
                                    TotalMonths = allSelectedMonths.Count(),
                                    MyUser = myUser,
                                    MyUserPost = selectedPost
                                };
                                if (scoreTable.TotalPerformances > 0)
                                {
                                    scoreTables.Add(scoreTable);
                                }
                            }

                        }
                    }
                    else if (selectedBranch == null && selectedUserGuid is not null && selectedPost is not null)
                                {
                                    if (await assessment_Db.UserBranchPostStatics
                                        .AnyAsync(ub => ub.UserPost == selectedPost && ub.UserGuid == selectedUserGuid))
                                    {
                                        var UbGroupedByBranch = (await assessment_Db.UserBranchPostStatics
                                            .Include(ub => ub.EachMonthStatics)
                                            .Where(ub => ub.UserPost == selectedPost && ub.UserGuid == selectedUserGuid)
                                            .ToListAsync())
                                            .GroupBy(ub => ub.Branch);

                                        MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);

                                        foreach (var g in UbGroupedByBranch)
                                        {
                                            var allSelectedMonths = g
                                                .SelectMany(ub => ub.EachMonthStatics)
                                                .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                                    ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue));

                                            //MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
                                            //string myUserPost = g.First().UserPost;
                                            string branch = g.First().Branch;

                                            Assessment_ScoreTable scoreTable = new()
                                            {
                                                Branch = branch,
                                                TotalPerformances = allSelectedMonths
                                                    .Sum(ms => ms.TotalPerformances),
                                                TotalScore = allSelectedMonths
                                                    .Sum(ms => ms.TotalScore),
                                                TotalMonths = allSelectedMonths.Count(),
                                                MyUser = myUser,
                                                MyUserPost = selectedPost
                                            };
                                            if (scoreTable.TotalPerformances > 0)
                                            {
                                                scoreTables.Add(scoreTable);
                                            }
                                        }

                                    }
                                }
                */
            }
        }
        else
        {
            if (await assessment_Db.UserBranchPostStatics.AnyAsync(ub =>
                (selectedBranch == null || ub.Branch == selectedBranch) &&
                (selectedUserGuid == null || ub.UserGuid == selectedUserGuid) &&
                (selectedPost == null || ub.UserPost == selectedPost)))
            {
                IEnumerable<IGrouping<string, Assessment_UserBranchPostStaticsDbModel>>? UbGrouped;
                if (selectedBranch == null && selectedUserGuid == null && selectedPost == null)
                {
                    UbGrouped = (await assessment_Db.UserBranchPostStatics
                        .Include(ub => ub.EachMonthStatics)
                        .Include(ubp => ubp.OldStatics)
                        .Where(ubp =>
                            (!string.IsNullOrWhiteSpace(ChiefPosts[0]) && ubp.UserPost.Contains(ChiefPosts[0])) ||
                            (!string.IsNullOrWhiteSpace(ChiefPosts[1]) && ubp.UserPost.Contains(ChiefPosts[1])) ||
                            (!string.IsNullOrWhiteSpace(ChiefPosts[2]) && ubp.UserPost.Contains(ChiefPosts[2])) ||
                            (!string.IsNullOrWhiteSpace(ChiefPosts[3]) && ubp.UserPost.Contains(ChiefPosts[3])) ||
                            (!string.IsNullOrWhiteSpace(ChiefPosts[4]) && ubp.UserPost.Contains(ChiefPosts[4])) ||
                            (!string.IsNullOrWhiteSpace(ChiefPosts[5]) && ubp.UserPost.Contains(ChiefPosts[5])) ||
                            (!string.IsNullOrWhiteSpace(ChiefPosts[6]) && ubp.UserPost.Contains(ChiefPosts[6])) ||
                            (!string.IsNullOrWhiteSpace(ChiefPosts[7]) && ubp.UserPost.Contains(ChiefPosts[7])) ||
                            (!string.IsNullOrWhiteSpace(ChiefPosts[8]) && ubp.UserPost.Contains(ChiefPosts[8])) ||
                            (!string.IsNullOrWhiteSpace(ChiefPosts[9]) && ubp.UserPost.Contains(ChiefPosts[9])))
                        .ToListAsync())
                        .GroupBy(ub => ub.Branch);
                }
                else
                {
                    UbGrouped = (await assessment_Db.UserBranchPostStatics
                        .Include(ub => ub.EachMonthStatics)
                        .Include(ubp => ubp.OldStatics)
                        .Where(ub => (selectedBranch == null || ub.Branch == selectedBranch) &&
                            (selectedUserGuid == null || selectedUserGuid == ub.UserGuid) &&
                            (selectedPost == null || selectedPost == ub.UserPost))
                        .ToListAsync())
                        .GroupBy(ubp => (selectedBranch == null ? ubp.Branch : string.Empty) +
                            (selectedUserGuid == null ? ubp.UserGuid : string.Empty) +
                            (selectedPost == null ? ubp.UserPost : string.Empty));
                }

                foreach (var g in UbGrouped)
                {
                    var allSelectedMonths = g
                    .SelectMany(ub => ub.EachMonthStatics)
                    .Where(ms => (fromDateTime == null || fromDateTime.Value <= ms.RegisterDateTime) &&
                        (toDateTime == null || toDateTime.Value >= ms.RegisterDateTime));

                    //Console.WriteLine($"\n\n\n************************* allSelectedMonths.Count(): {allSelectedMonths.Count()} ***********************\n\n\n");

                    string eachBranch = g.First().Branch;
                    string eachPost = g.First().UserPost;
                    MyIdentityUser? eachUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == g.First().UserGuid);

                    Assessment_ScoreTable scoreTable = new()
                    {
                        Branch = selectedBranch ?? eachBranch,
                        TotalPerformances = allSelectedMonths
                                .Sum(ems => ems.TotalPerformances),
                        TotalScore = allSelectedMonths
                                .Sum(ems => ems.TotalScore),
                        TotalMonths = allSelectedMonths.Count(),
                        MyUser = selectedUser ?? eachUser,
                        MyUserPost = selectedPost ?? eachPost
                    };

                    if (fromDateTime == null && toDateTime == null)
                    {
                        scoreTable.TotalScore += g.Sum(ubp => ubp.OldStatics.TotalScore);
                        scoreTable.TotalPerformances += g.Sum(ubp => ubp.OldStatics.TotalPerformances);
                        scoreTable.TotalMonths += g.Sum(ubp => ubp.OldStatics.TotalMonths);
                    }

                    if (scoreTable.TotalPerformances > 0)
                    {
                        scoreTables.Add(scoreTable);
                    }
                    //Console.WriteLine($"\n\n\n************************* scoreTable.TotalPerformances: {scoreTable.TotalPerformances} ***********************\n\n\n");
                }

            }

            /*
                if (selectedBranch == null && selectedUserGuid == null && selectedPost == null)
                {
                    if (await assessment_Db.UserBranchPostStatics.AnyAsync())
                    {
                        var UbGroupedByBranch = (await assessment_Db.UserBranchPostStatics
                                .Include(ub => ub.EachMonthStatics)
                                .Include(ubp => ubp.OldStatics)
                                .Where(ubp => ChiefPosts.Any(cp => ubp.UserPost.Contains(cp)))
                                .ToListAsync())
                                .GroupBy(ub => ub.Branch);

                        foreach (var g in UbGroupedByBranch)
                        {
                            var allSelectedMonths = g
                                .SelectMany(ub => ub.EachMonthStatics)
                                .Where(ms => (fromDateTime == null || fromDateTime >= ms.RegisterDateTime) &&
                                    (toDateTime == null || toDateTime <= ms.RegisterDateTime));

                            var branch = g.Key;

                            Assessment_ScoreTable scoreTable = new()
                            {
                                Branch = branch,
                                TotalPerformances = allSelectedMonths
                                    .Sum(ms => ms.TotalPerformances),
                                TotalScore = allSelectedMonths
                                    .Sum(ms => ms.TotalScore),
                                TotalMonths = allSelectedMonths.Count()
                            };

                            if (fromDateTime == null && toDateTime == null)
                            {
                                scoreTable.TotalScore += g.Sum(ubp => ubp.OldStatics.TotalScore);
                                scoreTable.TotalPerformances += g.Sum(ubp => ubp.OldStatics.TotalPerformances);
                                scoreTable.TotalMonths += g.Sum(ubp => ubp.OldStatics.TotalMonths);
                            }

                            if (scoreTable.TotalPerformances > 0)
                            {
                                scoreTables.Add(scoreTable);
                            }
                        }
                    }
                }
                else if (selectedBranch is not null && selectedUserGuid == null && selectedPost == null)
                {
                    if (await assessment_Db.UserBranchPostStatics.AnyAsync(ub => ub.Branch == selectedBranch))
                    {
                        var UbGroupedByUserGuidPost = (await assessment_Db.UserBranchPostStatics
                            .Include(ub => ub.EachMonthStatics)
                            .Where(ub => ub.Branch == selectedBranch)
                            .ToListAsync())
                            .GroupBy(ub => ub.UserGuid + ub.UserPost);

                        foreach (var g in UbGroupedByUserGuidPost)
                        {
                            var allSelectedMonths = g
                                .SelectMany(ub => ub.EachMonthStatics)
                                .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                    ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue));

                            MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == g.First().UserGuid);
                            string myUserPost = g.First().UserPost;

                            Assessment_ScoreTable scoreTable = new()
                            {
                                Branch = selectedBranch,
                                TotalPerformances = allSelectedMonths
                                    .Sum(ms => ms.TotalPerformances),
                                TotalScore = allSelectedMonths
                                    .Sum(ms => ms.TotalScore),
                                TotalMonths = allSelectedMonths.Count(),
                                MyUser = myUser,
                                MyUserPost = myUserPost
                            };
                            if (scoreTable.TotalPerformances > 0)
                            {
                                scoreTables.Add(scoreTable);
                            }
                        }

                    }
                }
                else if (selectedBranch == null && selectedUserGuid is not null && selectedPost == null)
                {
                    if (await assessment_Db.UserBranchPostStatics.AnyAsync(ub => ub.UserGuid == selectedUserGuid))
                    {
                        var UbGroupedByBranchPost = (await assessment_Db.UserBranchPostStatics
                            .Include(ub => ub.EachMonthStatics)
                            .Where(ub => ub.UserGuid == selectedUserGuid)
                            .ToListAsync())
                            .GroupBy(ub => ub.Branch + ub.UserPost);

                        MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
                        foreach (var g in UbGroupedByBranchPost)
                        {
                            var allSelectedMonths = g
                                .SelectMany(ub => ub.EachMonthStatics)
                                .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                    ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue));

                            //MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
                            string myUserPost = g.First().UserPost;
                            string branch = g.First().Branch;

                            Assessment_ScoreTable scoreTable = new()
                            {
                                Branch = branch,
                                TotalPerformances = allSelectedMonths
                                    .Sum(ms => ms.TotalPerformances),
                                TotalScore = allSelectedMonths
                                    .Sum(ms => ms.TotalScore),
                                TotalMonths = allSelectedMonths.Count(),
                                MyUser = myUser,
                                MyUserPost = myUserPost
                            };
                            if (scoreTable.TotalPerformances > 0)
                            {
                                scoreTables.Add(scoreTable);
                            }
                        }

                    }
                }
                else if (selectedBranch is not null && selectedUserGuid is not null && selectedPost == null)
                {
                    if (await assessment_Db.UserBranchPostStatics
                        .AnyAsync(ub => ub.UserGuid == selectedUserGuid && ub.Branch == selectedBranch))
                    {
                        var UbGroupedByPost = (await assessment_Db.UserBranchPostStatics
                            .Include(ub => ub.EachMonthStatics)
                            .Where(ub => ub.UserGuid == selectedUserGuid && ub.Branch == selectedBranch)
                            .ToListAsync())
                            .GroupBy(ub => ub.UserPost);

                        MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
                        foreach (var g in UbGroupedByPost)
                        {
                            var allSelectedMonths = g
                                .SelectMany(ub => ub.EachMonthStatics)
                                .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                    ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue));

                            //MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
                            string myUserPost = g.First().UserPost;
                            //string branch = g.First().Branch;

                            Assessment_ScoreTable scoreTable = new()
                            {
                                Branch = selectedBranch,
                                TotalPerformances = allSelectedMonths
                                    .Sum(ms => ms.TotalPerformances),
                                TotalScore = allSelectedMonths
                                    .Sum(ms => ms.TotalScore),
                                TotalMonths = allSelectedMonths.Count(),
                                MyUser = myUser,
                                MyUserPost = myUserPost
                            };
                            if (scoreTable.TotalPerformances > 0)
                            {
                                scoreTables.Add(scoreTable);
                            }
                        }

                    }
                }
                else if (selectedBranch is not null && selectedUserGuid == null && selectedPost is not null)
                {
                    if (await assessment_Db.UserBranchPostStatics
                        .AnyAsync(ub => ub.UserPost == selectedPost && ub.Branch == selectedBranch))
                    {
                        var UbGroupedByUser = (await assessment_Db.UserBranchPostStatics
                            .Include(ub => ub.EachMonthStatics)
                            .Where(ub => ub.UserPost == selectedPost && ub.Branch == selectedBranch)
                            .ToListAsync())
                            .GroupBy(ub => ub.UserGuid);

                        foreach (var g in UbGroupedByUser)
                        {
                            var allSelectedMonths = g
                                .SelectMany(ub => ub.EachMonthStatics)
                                .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                    ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue));

                            MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == g.First().UserGuid);
                            //string myUserPost = g.First().UserPost;
                            //string branch = g.First().Branch;

                            Assessment_ScoreTable scoreTable = new()
                            {
                                Branch = selectedBranch,
                                TotalPerformances = allSelectedMonths
                                    .Sum(ms => ms.TotalPerformances),
                                TotalScore = allSelectedMonths
                                    .Sum(ms => ms.TotalScore),
                                TotalMonths = allSelectedMonths.Count(),
                                MyUser = myUser,
                                MyUserPost = selectedPost
                            };
                            if (scoreTable.TotalPerformances > 0)
                            {
                                scoreTables.Add(scoreTable);
                            }
                        }

                    }
                }
                else if (selectedBranch == null && selectedUserGuid is not null && selectedPost is not null)
                        {
                            if (await assessment_Db.UserBranchPostStatics
                                .AnyAsync(ub => ub.UserPost == selectedPost && ub.UserGuid == selectedUserGuid))
                            {
                                var UbGroupedByBranch = (await assessment_Db.UserBranchPostStatics
                                    .Include(ub => ub.EachMonthStatics)
                                    .Where(ub => ub.UserPost == selectedPost && ub.UserGuid == selectedUserGuid)
                                    .ToListAsync())
                                    .GroupBy(ub => ub.Branch);

                                MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);

                                foreach (var g in UbGroupedByBranch)
                                {
                                    var allSelectedMonths = g
                                        .SelectMany(ub => ub.EachMonthStatics)
                                        .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                            ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue));

                                    //MyIdentityUser? myUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
                                    //string myUserPost = g.First().UserPost;
                                    string branch = g.First().Branch;

                                    Assessment_ScoreTable scoreTable = new()
                                    {
                                        Branch = branch,
                                        TotalPerformances = allSelectedMonths
                                            .Sum(ms => ms.TotalPerformances),
                                        TotalScore = allSelectedMonths
                                            .Sum(ms => ms.TotalScore),
                                        TotalMonths = allSelectedMonths.Count(),
                                        MyUser = myUser,
                                        MyUserPost = selectedPost
                                    };
                                    if (scoreTable.TotalPerformances > 0)
                                    {
                                        scoreTables.Add(scoreTable);
                                    }
                                }

                            }
                        }
            */
        }

        return scoreTables;
    }

    public async Task SubmitScoreForStatics(Assessment_PerformanceDbModel performanceDbModel, int Score,
    bool extraStatics = true)
    {
        //*********** get the year and month name ************
        DateTime registerDateTime = performanceDbModel.RegisterDate!.Value;
        int persianYear = new PersianCalendar().GetYear(registerDateTime);
        int persianMonth = new PersianCalendar().GetMonth(registerDateTime);
        string persianMonthName = persianMonth switch
        {
            1 => "فروردین",
            2 => "اردیبهشت",
            3 => "خرداد",
            4 => "تیر",
            5 => "مرداد",
            6 => "شهریور",
            7 => "مهر",
            8 => "آبان",
            9 => "آذر",
            10 => "دی",
            11 => "بهمن",
            12 => "اسفند",
            _ => "نامشخص",
        };

        if (performanceDbModel.ChiefGuid == performanceDbModel.RegistrarGuid)
        {
            await SubmitStaticsForUser(performanceDbModel.ChiefGuid, performanceDbModel.ChiefPost,
            performanceDbModel.Branch, persianYear, persianMonthName, registerDateTime,
            Score);
        }
        else
        {
            await SubmitStaticsForUser(performanceDbModel.RegistrarGuid, performanceDbModel.RegistrarPost,
            performanceDbModel.Branch, persianYear, persianMonthName, registerDateTime,
            Score);

            await SubmitStaticsForUser(performanceDbModel.ChiefGuid, performanceDbModel.ChiefPost,
            performanceDbModel.Branch, persianYear, persianMonthName, registerDateTime,
            Score);
        }

        if (extraStatics)
        {
            await CalculateTopRecentStatics();
            await ReconsiderOldStatics();
        }
    }

    public async Task DeleteScoreForStatics(Assessment_PerformanceDbModel performanceDbModel, int Score)
    {
        //*********** get the year and month name ************
        int persianYear = new PersianCalendar().GetYear(performanceDbModel.RegisterDate!.Value);
        int persianMonth = new PersianCalendar().GetMonth(performanceDbModel.RegisterDate!.Value);
        string persianMonthName = persianMonth switch
        {
            1 => "فروردین",
            2 => "اردیبهشت",
            3 => "خرداد",
            4 => "تیر",
            5 => "مرداد",
            6 => "شهریور",
            7 => "مهر",
            8 => "آبان",
            9 => "آذر",
            10 => "دی",
            11 => "بهمن",
            12 => "اسفند",
            _ => "نامشخص",
        };

        if (performanceDbModel.ChiefGuid == performanceDbModel.RegistrarGuid)
        {
            await DeleteStaticsForUser(performanceDbModel.ChiefGuid, performanceDbModel.ChiefPost,
            performanceDbModel.Branch, persianYear, persianMonthName, Score);
        }
        else
        {
            await DeleteStaticsForUser(performanceDbModel.RegistrarGuid, performanceDbModel.RegistrarPost,
                    performanceDbModel.Branch, persianYear, persianMonthName, Score);

            await DeleteStaticsForUser(performanceDbModel.ChiefGuid, performanceDbModel.ChiefPost,
            performanceDbModel.Branch, persianYear, persianMonthName, Score);
        }

        await CalculateTopRecentStatics();

    }

    private async Task SubmitStaticsForUser(string userGuid, string userPost, string branch,
    int persianYear, string persianMonthName, DateTime registerDateTime, int Score)
    {
        Assessment_UserBranchPostStaticsDbModel? userBranchPostStatics =
            await assessment_Db.UserBranchPostStatics
            .Include(rb => rb.EachMonthStatics)
            .FirstOrDefaultAsync(us =>
                us.UserGuid == userGuid &&
                us.UserPost == userPost &&
                us.Branch == branch);

        if (userBranchPostStatics is null)
        {
            userBranchPostStatics = new()
            {
                UserGuid = userGuid,
                UserPost = userPost,
                Branch = branch
            };
            await assessment_Db.UserBranchPostStatics.AddAsync(userBranchPostStatics);
        }

        //********* submit statics ************

        var eachMonthStaticsDbModel = userBranchPostStatics.EachMonthStatics
        .FirstOrDefault(ms => ms.Year == persianYear && ms.MonthName == persianMonthName);
        if (eachMonthStaticsDbModel is null)
        {
            eachMonthStaticsDbModel = new()
            {
                RegisterDateTime = registerDateTime,
                Year = persianYear,
                MonthName = persianMonthName,
                TotalScore = Score,
                TotalPerformances = 1
            };
            userBranchPostStatics.EachMonthStatics.Add(eachMonthStaticsDbModel);
        }
        else
        {
            eachMonthStaticsDbModel.TotalScore += Score;
            eachMonthStaticsDbModel.TotalPerformances++;
        }
        await assessment_Db.SaveChangesAsync();
    }

    private async Task DeleteStaticsForUser(string userGuid, string userPost, string branch,
    int persianYear, string persianMonthName, int Score)
    {
        Assessment_UserBranchPostStaticsDbModel? userBranchStatics =
            await assessment_Db.UserBranchPostStatics
            .Include(rb => rb.EachMonthStatics)
            .FirstOrDefaultAsync(us =>
                us.UserGuid == userGuid &&
                us.UserPost == userPost &&
                us.Branch == branch);

        if (userBranchStatics is null)
        {
            return;
        }

        //********* submit for user ************
        var userEachMonthStaticsDbModel = userBranchStatics.EachMonthStatics
        .FirstOrDefault(ms => ms.Year == persianYear && ms.MonthName == persianMonthName);
        if (userEachMonthStaticsDbModel is null)
        {
            return;
        }
        else
        {
            userEachMonthStaticsDbModel.TotalScore -= Score;
            userEachMonthStaticsDbModel.TotalPerformances--;
        }

        await assessment_Db.SaveChangesAsync();

        //seed eachMonthStatics
        //await EachMonthStatics_UpdateSeed(registrarEachMonthStaticsDbModel);
    }

    public async Task CalculateTopRecentStatics()
    {
        //calculate recent top beanches statics
        List<Assessment_ScoreTable> orderedScoreTables =
            (await GetScoreTableList(fromDateTime: DateTime.Today - TimeSpan.FromDays(30)))
            .OrderByDescending(st => st.TotalScore)
            .Take(10)
            .ToList();

        /*assessment_Info.RecentTopBranchesScoreTable.Clear();
        foreach (var st in orderedScoreTables)
        {
            assessment_Info.RecentTopBranchesScoreTable.Enqueue(st);
        }*/
        assessment_Info.RecentTopBranchesScoreTable = new(orderedScoreTables);
    }

    private async Task ReconsiderOldStatics()
    {
        if (DateTime.Today != assessment_Info.LastTimeDoneReconsideringOldStatics)
        {
            DateTime specifiedDateTime = DateTime.Today - TimeSpan.FromDays(ConsideredOldDays);
            var oldEachMonthStatics = await assessment_Db.EachMonthStatics
            .Include(ems => ems.UserBranchPost)
            .ThenInclude(ubp => ubp.OldStatics)
            .Where(ems => ems.RegisterDateTime < specifiedDateTime)
            .ToListAsync();

            List<Assessment_EachMonthStaticsDbModel> selectedForRemove = [];
            foreach (Assessment_EachMonthStaticsDbModel ems in oldEachMonthStatics)
            {
                ems.UserBranchPost.OldStatics.TotalScore += ems.TotalScore;
                ems.UserBranchPost.OldStatics.TotalPerformances += ems.TotalPerformances;
                ems.UserBranchPost.OldStatics.TotalMonths++;

                selectedForRemove.Add(ems);
            }
            assessment_Db.EachMonthStatics.RemoveRange(selectedForRemove);
            await assessment_Db.SaveChangesAsync();

            assessment_Info.LastTimeDoneReconsideringOldStatics = DateTime.Today;
            Console.WriteLine($"***** Assessment_Process.ReconsiderOldStatics() in {assessment_Info.LastTimeDoneReconsideringOldStatics} *****");
        }
    }

    //**************** Score Chart ****************
    public async Task<Assessment_ScoreChart> GetScoreChartList(string? selectedBranch = null,
    string? selectedUserGuid = null, string? selectedPost = null, DateTime? fromDateTime = null,
    DateTime? toDateTime = null)
    {
        Assessment_ScoreChart scoreChart = new()
        {
            FromDate = fromDateTime?.ToString("yyyy/MM", new CultureInfo("fa-IR")),
            ToDate = toDateTime?.ToString("yyyy/MM", new CultureInfo("fa-IR")),
            Branch = selectedBranch,
            MyUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid),
            UserPost = selectedPost,
            DateScore = [],
        };

        MyIdentityUser? selectedUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == selectedUserGuid);
        List<MyIdentityUser> allRelatedUsers = await accountProcess.GetAllRelatedUsers(selectedUser);

        if (allRelatedUsers.Count > 0)
        {
            foreach (MyIdentityUser user in allRelatedUsers)
            {
                selectedUserGuid = user.UserGuid;

                if (await assessment_Db.UserBranchPostStatics.AnyAsync(ub =>
                (selectedBranch == null || ub.Branch == selectedBranch) &&
                (selectedUserGuid == null || ub.UserGuid == selectedUserGuid) &&
                (selectedPost == null || ub.UserPost == selectedPost)))
                {
                    IEnumerable<IGrouping<string, Assessment_EachMonthStaticsDbModel>>? MsGroupedByYearMonth;
                    if (selectedUserGuid == null && selectedPost == null)
                    {
                        MsGroupedByYearMonth = (await assessment_Db.UserBranchPostStatics
                            .Include(ub => ub.EachMonthStatics)
                            .Where(ubp => (selectedBranch == null || ubp.Branch == selectedBranch) &&
                                (
                                    (!string.IsNullOrWhiteSpace(ChiefPosts[0]) && ubp.UserPost.Contains(ChiefPosts[0])) ||
                                    (!string.IsNullOrWhiteSpace(ChiefPosts[1]) && ubp.UserPost.Contains(ChiefPosts[1])) ||
                                    (!string.IsNullOrWhiteSpace(ChiefPosts[2]) && ubp.UserPost.Contains(ChiefPosts[2])) ||
                                    (!string.IsNullOrWhiteSpace(ChiefPosts[3]) && ubp.UserPost.Contains(ChiefPosts[3])) ||
                                    (!string.IsNullOrWhiteSpace(ChiefPosts[4]) && ubp.UserPost.Contains(ChiefPosts[4])) ||
                                    (!string.IsNullOrWhiteSpace(ChiefPosts[5]) && ubp.UserPost.Contains(ChiefPosts[5])) ||
                                    (!string.IsNullOrWhiteSpace(ChiefPosts[6]) && ubp.UserPost.Contains(ChiefPosts[6])) ||
                                    (!string.IsNullOrWhiteSpace(ChiefPosts[7]) && ubp.UserPost.Contains(ChiefPosts[7])) ||
                                    (!string.IsNullOrWhiteSpace(ChiefPosts[8]) && ubp.UserPost.Contains(ChiefPosts[8])) ||
                                    (!string.IsNullOrWhiteSpace(ChiefPosts[9]) && ubp.UserPost.Contains(ChiefPosts[9]))
                                )
                            )
                            .ToListAsync())
                            .SelectMany(ub => ub.EachMonthStatics)
                            .Where(ms => (fromDateTime == null || fromDateTime <= ms.RegisterDateTime) &&
                                (toDateTime == null || toDateTime >= ms.RegisterDateTime))
                            .GroupBy(ms => ms.Year.ToString() + " " + ms.MonthName);
                    }
                    else
                    {
                        MsGroupedByYearMonth = (await assessment_Db.UserBranchPostStatics
                            .Include(ub => ub.EachMonthStatics)
                            .Where(ub => (selectedBranch == null || ub.Branch == selectedBranch) &&
                                (selectedUserGuid == null || selectedUserGuid == ub.UserGuid) &&
                                (selectedPost == null || selectedPost == ub.UserPost))
                            .ToListAsync())
                            .SelectMany(ub => ub.EachMonthStatics)
                            .Where(ms => (fromDateTime == null || fromDateTime <= ms.RegisterDateTime) &&
                                (toDateTime == null || toDateTime >= ms.RegisterDateTime))
                            .GroupBy(ms => ms.Year.ToString() + " " + ms.MonthName);
                    }

                    foreach (var g in MsGroupedByYearMonth)
                    {
                        var totalScoreForEachMonth = g.Sum(ms => ms.TotalScore);
                        if (scoreChart.DateScore.ContainsKey(g.Key))
                        {
                            scoreChart.DateScore[g.Key] += totalScoreForEachMonth;
                        }
                        else
                        {
                            scoreChart.DateScore.Add(g.Key, totalScoreForEachMonth);
                        }
                    }
                }
            }
        }
        else
        {
            if (await assessment_Db.UserBranchPostStatics.AnyAsync(ub =>
                (selectedBranch == null || ub.Branch == selectedBranch) &&
                (selectedUserGuid == null || ub.UserGuid == selectedUserGuid) &&
                (selectedPost == null || ub.UserPost == selectedPost)))
            {
                IEnumerable<IGrouping<string, Assessment_EachMonthStaticsDbModel>>? MsGroupedByYearMonth;
                if (selectedUserGuid == null && selectedPost == null)
                {
                    MsGroupedByYearMonth = (await assessment_Db.UserBranchPostStatics
                        .Include(ub => ub.EachMonthStatics)
                        .Where(ubp => (selectedBranch == null || ubp.Branch == selectedBranch) &&
                            (
                                (!string.IsNullOrWhiteSpace(ChiefPosts[0]) && ubp.UserPost.Contains(ChiefPosts[0])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[1]) && ubp.UserPost.Contains(ChiefPosts[1])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[2]) && ubp.UserPost.Contains(ChiefPosts[2])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[3]) && ubp.UserPost.Contains(ChiefPosts[3])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[4]) && ubp.UserPost.Contains(ChiefPosts[4])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[5]) && ubp.UserPost.Contains(ChiefPosts[5])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[6]) && ubp.UserPost.Contains(ChiefPosts[6])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[7]) && ubp.UserPost.Contains(ChiefPosts[7])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[8]) && ubp.UserPost.Contains(ChiefPosts[8])) ||
                                (!string.IsNullOrWhiteSpace(ChiefPosts[9]) && ubp.UserPost.Contains(ChiefPosts[9]))
                            )
                        )
                        .ToListAsync())
                        .SelectMany(ub => ub.EachMonthStatics)
                        .Where(ms => (fromDateTime == null || fromDateTime <= ms.RegisterDateTime) &&
                            (toDateTime == null || toDateTime >= ms.RegisterDateTime))
                        .GroupBy(ms => ms.Year.ToString() + " " + ms.MonthName);
                }
                else
                {
                    MsGroupedByYearMonth = (await assessment_Db.UserBranchPostStatics
                        .Include(ub => ub.EachMonthStatics)
                        .Where(ub => (selectedBranch == null || ub.Branch == selectedBranch) &&
                            (selectedUserGuid == null || selectedUserGuid == ub.UserGuid) &&
                            (selectedPost == null || selectedPost == ub.UserPost))
                        .ToListAsync())
                        .SelectMany(ub => ub.EachMonthStatics)
                        .Where(ms => (fromDateTime == null || fromDateTime <= ms.RegisterDateTime) &&
                            (toDateTime == null || toDateTime >= ms.RegisterDateTime))
                        .GroupBy(ms => ms.Year.ToString() + " " + ms.MonthName);
                }

                foreach (var g in MsGroupedByYearMonth)
                {
                    var totalScoreForEachMonth = g.Sum(ms => ms.TotalScore);
                    if (scoreChart.DateScore.ContainsKey(g.Key))
                    {
                        scoreChart.DateScore[g.Key] += totalScoreForEachMonth;
                    }
                    else
                    {
                        scoreChart.DateScore.Add(g.Key, totalScoreForEachMonth);
                    }
                }
            }

            /*
                if (selectedBranch == null && selectedUserGuid == null && selectedPost == null)
                {
                    if (await assessment_Db.UserBranchPostStatics.AnyAsync())
                    {
                        var MsGroupedByYearMonth = (await assessment_Db.UserBranchPostStatics
                                .Include(ub => ub.EachMonthStatics)
                                .Where(ub => ub.UserPost.Contains("مدیر") ||
                                    ub.UserPost.Contains("رئیس") ||
                                    ub.UserPost.Contains("سرپرست"))
                                .ToListAsync())
                                .SelectMany(ub => ub.EachMonthStatics)
                                .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                    ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue))
                                .GroupBy(ms => ms.Year.ToString() + " " + ms.MonthName);

                        foreach (var g in MsGroupedByYearMonth)
                        {
                            var totalScoreForEachMonth = g.Sum(ms => ms.TotalScore);
                            if (scoreChart.DateScore.ContainsKey(g.Key))
                            {
                                scoreChart.DateScore[g.Key] += totalScoreForEachMonth;
                            }
                            else
                            {
                                scoreChart.DateScore.Add(g.Key, totalScoreForEachMonth);
                            }
                        }
                    }
                }
                else if (selectedBranch is not null && selectedUserGuid == null && selectedPost == null)
                {
                    if (await assessment_Db.UserBranchPostStatics.AnyAsync(ub => ub.Branch == selectedBranch))
                    {
                        var MsGroupedByYearMonth = (await assessment_Db.UserBranchPostStatics
                                .Include(ub => ub.EachMonthStatics)
                                .Where(ub => ub.Branch == selectedBranch &&
                                    (ub.UserPost.Contains("مدیر") ||
                                    ub.UserPost.Contains("رئیس") ||
                                    ub.UserPost.Contains("سرپرست")))
                                .ToListAsync())
                                .SelectMany(ub => ub.EachMonthStatics)
                                .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                    ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue))
                                .GroupBy(ms => ms.Year.ToString() + " " + ms.MonthName);

                        foreach (var g in MsGroupedByYearMonth)
                        {
                            var totalScoreForEachMonth = g.Sum(ms => ms.TotalScore);
                            if (scoreChart.DateScore.ContainsKey(g.Key))
                            {
                                scoreChart.DateScore[g.Key] += totalScoreForEachMonth;
                            }
                            else
                            {
                                scoreChart.DateScore.Add(g.Key, totalScoreForEachMonth);
                            }
                        }
                    }
                }
                else if (selectedBranch == null && selectedUserGuid is not null && selectedPost == null)
                {
                    if (await assessment_Db.UserBranchPostStatics.AnyAsync(ub => ub.UserGuid == selectedUserGuid))
                    {
                        var MsGroupedByYearMonth = (await assessment_Db.UserBranchPostStatics
                                .Include(ub => ub.EachMonthStatics)
                                .Where(ub => ub.UserGuid == selectedUserGuid)
                                .ToListAsync())
                                .SelectMany(ub => ub.EachMonthStatics)
                                .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                    ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue))
                                .GroupBy(ms => ms.Year.ToString() + " " + ms.MonthName);

                        foreach (var g in MsGroupedByYearMonth)
                        {
                            var totalScoreForEachMonth = g.Sum(ms => ms.TotalScore);
                            if (scoreChart.DateScore.ContainsKey(g.Key))
                            {
                                scoreChart.DateScore[g.Key] += totalScoreForEachMonth;
                            }
                            else
                            {
                                scoreChart.DateScore.Add(g.Key, totalScoreForEachMonth);
                            }
                        }
                    }
                }
                else if (selectedBranch is not null && selectedUserGuid is not null && selectedPost == null)
                {
                    if (await assessment_Db.UserBranchPostStatics
                        .AnyAsync(ub => ub.UserGuid == selectedUserGuid && ub.Branch == selectedBranch))
                    {
                        var MsGroupedByYearMonth = (await assessment_Db.UserBranchPostStatics
                                .Include(ub => ub.EachMonthStatics)
                                .Where(ub => ub.UserGuid == selectedUserGuid && ub.Branch == selectedBranch)
                                .ToListAsync())
                                .SelectMany(ub => ub.EachMonthStatics)
                                .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                    ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue))
                                .GroupBy(ms => ms.Year.ToString() + " " + ms.MonthName);

                        foreach (var g in MsGroupedByYearMonth)
                        {
                            var totalScoreForEachMonth = g.Sum(ms => ms.TotalScore);
                            if (scoreChart.DateScore.ContainsKey(g.Key))
                            {
                                scoreChart.DateScore[g.Key] += totalScoreForEachMonth;
                            }
                            else
                            {
                                scoreChart.DateScore.Add(g.Key, totalScoreForEachMonth);
                            }
                        }
                    }
                }
                else if (selectedBranch is not null && selectedUserGuid == null && selectedPost is not null)
                {
                    if (await assessment_Db.UserBranchPostStatics
                        .AnyAsync(ub => ub.UserPost == selectedPost && ub.Branch == selectedBranch))
                    {
                        var MsGroupedByYearMonth = (await assessment_Db.UserBranchPostStatics
                                .Include(ub => ub.EachMonthStatics)
                                .Where(ub => ub.UserPost == selectedPost && ub.Branch == selectedBranch)
                                .ToListAsync())
                                .SelectMany(ub => ub.EachMonthStatics)
                                .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                    ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue))
                                .GroupBy(ms => ms.Year.ToString() + " " + ms.MonthName);

                        foreach (var g in MsGroupedByYearMonth)
                        {
                            var totalScoreForEachMonth = g.Sum(ms => ms.TotalScore);
                            if (scoreChart.DateScore.ContainsKey(g.Key))
                            {
                                scoreChart.DateScore[g.Key] += totalScoreForEachMonth;
                            }
                            else
                            {
                                scoreChart.DateScore.Add(g.Key, totalScoreForEachMonth);
                            }
                        }
                    }
                }
                else if (selectedBranch == null && selectedUserGuid is not null && selectedPost is not null)
                        {
                            if (await assessment_Db.UserBranchPostStatics
                                .AnyAsync(ub => ub.UserPost == selectedPost && ub.UserGuid == selectedUserGuid))
                            {
                                var MsGroupedByYearMonth = (await assessment_Db.UserBranchPostStatics
                                        .Include(ub => ub.EachMonthStatics)
                                        .Where(ub => ub.UserPost == selectedPost && ub.UserGuid == selectedUserGuid)
                                        .ToListAsync())
                                        .SelectMany(ub => ub.EachMonthStatics)
                                        .Where(ms => ms.RegisterDateTime >= (fromDateTime ?? DateTime.MinValue) &&
                                            ms.RegisterDateTime <= (toDateTime ?? DateTime.MaxValue))
                                        .GroupBy(ms => ms.Year.ToString() + " " + ms.MonthName);

                                foreach (var g in MsGroupedByYearMonth)
                                {
                                    var totalScoreForEachMonth = g.Sum(ms => ms.TotalScore);
                                    if (scoreChart.DateScore.ContainsKey(g.Key))
                                    {
                                        scoreChart.DateScore[g.Key] += totalScoreForEachMonth;
                                    }
                                    else
                                    {
                                        scoreChart.DateScore.Add(g.Key, totalScoreForEachMonth);
                                    }
                                }
                            }
                        }
            */
        }

        return scoreChart;
    }

    //**************** Seed **************
    public async Task SeedDb()
    {
        await Performance_SeedDb();
        await ExpertConsideration_SeedDb();

        await CalculateTopRecentStatics();
        await ReconsiderOldStatics();
    }

    //**************** Seed Performances **************
    public async Task Performance_UpdateSeed(Assessment_PerformanceDbModel performanceDbModel)
    {
        Assessment_PerformanceSeedModel performanceSeedModel = new()
        {
            Guid = performanceDbModel.Guid,
            OrderGuid = performanceDbModel.OrderGuid,

            PerformanceType = performanceDbModel.PerformanceType,
            PerformanceField = performanceDbModel.PerformanceField,

            //********* Dates *********
            RegisterDate = performanceDbModel.RegisterDate,
            PerformDate = performanceDbModel.PerformDate,
            ConfirmDate = performanceDbModel.ConfirmDate,

            Branch = performanceDbModel.Branch,
            RegistrarGuid = performanceDbModel.RegistrarGuid,
            RegistrarPost = performanceDbModel.RegistrarPost,
            ConfirmerGuid = performanceDbModel.ConfirmerGuid,
            ChiefGuid = performanceDbModel.ChiefGuid,
            ChiefPost = performanceDbModel.ChiefPost,

            Status = performanceDbModel.Status,
            Subject = performanceDbModel.Subject,
            Attendees = performanceDbModel.Attendees,
            EventPlace = performanceDbModel.EventPlace,
            StartTime = performanceDbModel.StartTime,
            EndTime = performanceDbModel.EndTime,
            Description = performanceDbModel.Description,
            SecurityOpinion = performanceDbModel.SecurityOpinion,
            AttachedFileName = performanceDbModel.AttachedFileName,
            ExpertConsiderationsGuids = performanceDbModel.ExpertConsiderations.Select(ec => ec.Guid).ToArray(),

            //********* Correspondence ***********
            LetterSubject = performanceDbModel.LetterSubject,
            LetterDate = performanceDbModel.LetterDate,
            LetterNumber = performanceDbModel.LetterNumber,
            Receiver = performanceDbModel.Receiver,
            ReportDate = performanceDbModel.ReportDate,
            ReportNumber = performanceDbModel.ReportNumber,

            //********** Archive **********
            IsArchived = performanceDbModel.IsArchived,
        };

        string json = JsonSerializer.Serialize(performanceSeedModel);
        string fileSeedPath = Path.Combine(Performance_SeedDirectoryInfo.FullName, performanceDbModel.Guid);

        await File.WriteAllTextAsync(fileSeedPath, json);
    }

    public void Performance_DeleteSeed(Assessment_PerformanceDbModel performanceDbModel)
    {
        string performanceSeedPath = Path.Combine(Performance_SeedDirectoryInfo.FullName, performanceDbModel.Guid);
        File.Delete(performanceSeedPath);
    }

    private async Task Performance_SeedDb()
    {
        foreach (var fileInfo in Performance_SeedDirectoryInfo.EnumerateFiles())
        {
            var myPerformaceDbModel = await assessment_Db.Performances.FirstOrDefaultAsync(p => p.Guid == fileInfo.Name);
            if (myPerformaceDbModel != null) continue;

            string json = await File.ReadAllTextAsync(fileInfo.FullName);
            Assessment_PerformanceSeedModel? performanceSeedModel;
            try
            {
                performanceSeedModel = JsonSerializer.Deserialize<Assessment_PerformanceSeedModel>(json);
            }
            catch
            {
                //log
                continue;
            }
            if (performanceSeedModel is not null && !performanceSeedModel.IsArchived)
            {
                myPerformaceDbModel = new Assessment_PerformanceDbModel()
                {
                    Guid = performanceSeedModel.Guid,
                    OrderGuid = performanceSeedModel.OrderGuid,

                    PerformanceType = performanceSeedModel.PerformanceType,
                    PerformanceField = performanceSeedModel.PerformanceField,

                    //********* Dates *********
                    RegisterDate = performanceSeedModel.RegisterDate,
                    PerformDate = performanceSeedModel.PerformDate,
                    ConfirmDate = performanceSeedModel.ConfirmDate,

                    Branch = performanceSeedModel.Branch,
                    RegistrarGuid = performanceSeedModel.RegistrarGuid,
                    RegistrarPost = performanceSeedModel.RegistrarPost,
                    ConfirmerGuid = performanceSeedModel.ConfirmerGuid,
                    ChiefGuid = performanceSeedModel.ChiefGuid,
                    ChiefPost = performanceSeedModel.ChiefPost,

                    Status = performanceSeedModel.Status,
                    Subject = performanceSeedModel.Subject,
                    Attendees = performanceSeedModel.Attendees,
                    EventPlace = performanceSeedModel.EventPlace,
                    StartTime = performanceSeedModel.StartTime,
                    EndTime = performanceSeedModel.EndTime,
                    Description = performanceSeedModel.Description,
                    SecurityOpinion = performanceSeedModel.SecurityOpinion,
                    AttachedFileName = performanceSeedModel.AttachedFileName,

                    //********* Correspondence ***********
                    LetterSubject = performanceSeedModel.LetterSubject,
                    LetterDate = performanceSeedModel.LetterDate,
                    LetterNumber = performanceSeedModel.LetterNumber,
                    Receiver = performanceSeedModel.Receiver,
                    ReportDate = performanceSeedModel.ReportDate,
                    ReportNumber = performanceSeedModel.ReportNumber,

                    //********** Archive **********
                    IsArchived = performanceSeedModel.IsArchived,
                };
                await assessment_Db.Performances.AddAsync(myPerformaceDbModel);
            }
        }
        await assessment_Db.SaveChangesAsync();
    }

    //*********** Archive ***********
    public async Task ArchiveOlderPerformances(DateTime oldDateTime)
    {
        if (DateTime.Today != assessment_Info.LastTimeDoneArchive)
        {
            var oldPerformanceDbModels = await assessment_Db.Performances
            .Include(p => p.ExpertConsiderations)
            .Where(p => p.RegisterDate != null &&
                p.RegisterDate < oldDateTime)
            .ToListAsync();

            foreach (Assessment_PerformanceDbModel performanceDbModel in oldPerformanceDbModels)
            {
                if (performanceDbModel.OrderGuid == null ||
                (await ordersDb.Orders.FirstOrDefaultAsync(o => o.Guid == performanceDbModel.OrderGuid)) == null)
                {
                    performanceDbModel.IsArchived = true;
                    await Performance_UpdateSeed(performanceDbModel);
                    assessment_Db.Performances.Remove(performanceDbModel);
                }
            }
            await assessment_Db.SaveChangesAsync();

            assessment_Info.LastTimeDoneArchive = DateTime.Today;
            Console.WriteLine($"***** Assessment_Process.ArchiveOlderPerformances() in {assessment_Info.LastTimeDoneArchive} *****");
        }

    }

    public async Task<List<Assessment_PerformanceDbModel>> ReadArchivedPerformancesList(DateTime? fromDateTime,
    DateTime? toDateTime, int? take = null, int? skip = null)
    {
        if (skip <= 0) skip = null;
        if (take <= 0) take = null;
        List<Assessment_PerformanceDbModel> selectedPerformances = [];
        int haveSkipped = 0;
        int haveTaken = 0;
        foreach (var fileInfo in Performance_SeedDirectoryInfo.EnumerateFiles())
        {
            string json = await File.ReadAllTextAsync(fileInfo.FullName);
            Assessment_PerformanceSeedModel? performanceSeedModel;
            try
            {
                performanceSeedModel = JsonSerializer.Deserialize<Assessment_PerformanceSeedModel>(json);
            }
            catch
            {
                //log
                continue;
            }
            if (performanceSeedModel is not null)
            {
                if (performanceSeedModel.IsArchived)
                {
                    if ((fromDateTime == null || fromDateTime >= performanceSeedModel.RegisterDate) &&
                    (toDateTime == null || toDateTime <= performanceSeedModel.RegisterDate))
                    {
                        if (skip != null && haveSkipped < skip)
                        {
                            haveSkipped++;
                            continue;
                        }

                        Assessment_PerformanceDbModel performaceDbModel = new()
                        {
                            Guid = performanceSeedModel.Guid,
                            OrderGuid = performanceSeedModel.OrderGuid,

                            PerformanceType = performanceSeedModel.PerformanceType,
                            PerformanceField = performanceSeedModel.PerformanceField,

                            //********* Dates *********
                            RegisterDate = performanceSeedModel.RegisterDate,
                            PerformDate = performanceSeedModel.PerformDate,
                            ConfirmDate = performanceSeedModel.ConfirmDate,

                            Branch = performanceSeedModel.Branch,
                            RegistrarGuid = performanceSeedModel.RegistrarGuid,
                            RegistrarPost = performanceSeedModel.RegistrarPost,
                            ConfirmerGuid = performanceSeedModel.ConfirmerGuid,
                            ChiefGuid = performanceSeedModel.ChiefGuid,
                            ChiefPost = performanceSeedModel.ChiefPost,

                            Status = performanceSeedModel.Status,
                            Subject = performanceSeedModel.Subject,
                            Attendees = performanceSeedModel.Attendees,
                            EventPlace = performanceSeedModel.EventPlace,
                            StartTime = performanceSeedModel.StartTime,
                            EndTime = performanceSeedModel.EndTime,
                            Description = performanceSeedModel.Description,
                            SecurityOpinion = performanceSeedModel.SecurityOpinion,
                            AttachedFileName = performanceSeedModel.AttachedFileName,

                            //********* Correspondence ***********
                            LetterSubject = performanceSeedModel.LetterSubject,
                            LetterDate = performanceSeedModel.LetterDate,
                            LetterNumber = performanceSeedModel.LetterNumber,
                            Receiver = performanceSeedModel.Receiver,
                            ReportDate = performanceSeedModel.ReportDate,
                            ReportNumber = performanceSeedModel.ReportNumber,

                            //********** Archive **********
                            IsArchived = performanceSeedModel.IsArchived,
                        };

                        foreach (string ecGuid in performanceSeedModel.ExpertConsiderationsGuids)
                        {
                            var ecFileInfo = new FileInfo(Path.Combine(ExpertConsideration_SeedDirectoryInfo.FullName, ecGuid));
                            if (!ecFileInfo.Exists) continue;

                            string ecJson = await File.ReadAllTextAsync(ecFileInfo.FullName);
                            Assessment_ExpertConsiderationSeedModel? ecSeedModel;
                            try
                            {
                                ecSeedModel = JsonSerializer.Deserialize<Assessment_ExpertConsiderationSeedModel>(ecJson);
                            }
                            catch
                            {
                                //log
                                continue;
                            }
                            if (ecSeedModel is not null)
                            {
                                Assessment_ExpertConsiderationDbModel myEcDbModel = new()
                                {
                                    Guid = ecSeedModel.Guid,
                                    //PerformanceGuid = ecSeedModel.Performance.Guid,
                                    Performance = performaceDbModel,

                                    VpReferrerGuid = ecSeedModel.VpReferrerGuid,
                                    ExpertRefereeGuid = ecSeedModel.ExpertRefereeGuid,

                                    ReferenceDate = ecSeedModel.ReferenceDate,
                                    ConsiderationDate = ecSeedModel.ConsiderationDate,

                                    IsRejected = ecSeedModel.IsRejected,
                                    ExpertDescription = ecSeedModel.RejectDescription,
                                    Score = ecSeedModel.Score,

                                    //********** Archive **********
                                    //IsArchived = ecSeedModel.IsArchived,
                                };

                                performaceDbModel.ExpertConsiderations.Add(myEcDbModel);
                            }
                        }

                        selectedPerformances.Add(performaceDbModel);

                        haveTaken++;
                        if (take != null && haveTaken >= take)
                        {
                            return selectedPerformances;
                        }
                    }
                }

            }
        }
        return selectedPerformances;
    }

    public async Task<Assessment_PerformanceDbModel?> ReadArchivedPerformance(string performanceGuid)
    {
        FileInfo fileInfo = new FileInfo(Path.Combine(Performance_SeedDirectoryInfo.FullName, performanceGuid));
        if (!fileInfo.Exists) return null;

        string json = await File.ReadAllTextAsync(fileInfo.FullName);
        Assessment_PerformanceSeedModel? performanceSeedModel;
        try
        {
            performanceSeedModel = JsonSerializer.Deserialize<Assessment_PerformanceSeedModel>(json);
        }
        catch
        {
            //log
            return null;
        }
        if (performanceSeedModel is not null)
        {
            Assessment_PerformanceDbModel performaceDbModel = new()
            {
                Guid = performanceSeedModel.Guid,
                OrderGuid = performanceSeedModel.OrderGuid,

                PerformanceType = performanceSeedModel.PerformanceType,
                PerformanceField = performanceSeedModel.PerformanceField,

                //********* Dates *********
                RegisterDate = performanceSeedModel.RegisterDate,
                PerformDate = performanceSeedModel.PerformDate,
                ConfirmDate = performanceSeedModel.ConfirmDate,

                Branch = performanceSeedModel.Branch,
                RegistrarGuid = performanceSeedModel.RegistrarGuid,
                RegistrarPost = performanceSeedModel.RegistrarPost,
                ConfirmerGuid = performanceSeedModel.ConfirmerGuid,
                ChiefGuid = performanceSeedModel.ChiefGuid,
                ChiefPost = performanceSeedModel.ChiefPost,

                Status = performanceSeedModel.Status,
                Subject = performanceSeedModel.Subject,
                Attendees = performanceSeedModel.Attendees,
                EventPlace = performanceSeedModel.EventPlace,
                StartTime = performanceSeedModel.StartTime,
                EndTime = performanceSeedModel.EndTime,
                Description = performanceSeedModel.Description,
                SecurityOpinion = performanceSeedModel.SecurityOpinion,
                AttachedFileName = performanceSeedModel.AttachedFileName,

                //********* Correspondence ***********
                LetterSubject = performanceSeedModel.LetterSubject,
                LetterDate = performanceSeedModel.LetterDate,
                LetterNumber = performanceSeedModel.LetterNumber,
                Receiver = performanceSeedModel.Receiver,
                ReportDate = performanceSeedModel.ReportDate,
                ReportNumber = performanceSeedModel.ReportNumber,

                //********** Archive **********
                IsArchived = performanceSeedModel.IsArchived,
            };

            foreach (string ecGuid in performanceSeedModel.ExpertConsiderationsGuids)
            {
                var ecFileInfo = new FileInfo(Path.Combine(ExpertConsideration_SeedDirectoryInfo.FullName, ecGuid));
                if (!ecFileInfo.Exists) continue;

                string ecJson = await File.ReadAllTextAsync(ecFileInfo.FullName);
                Assessment_ExpertConsiderationSeedModel? ecSeedModel;
                try
                {
                    ecSeedModel = JsonSerializer.Deserialize<Assessment_ExpertConsiderationSeedModel>(ecJson);
                }
                catch
                {
                    //log
                    continue;
                }
                if (ecSeedModel is not null)
                {
                    Assessment_ExpertConsiderationDbModel myEcDbModel = new()
                    {
                        Guid = ecSeedModel.Guid,
                        //PerformanceGuid = ecSeedModel.Performance.Guid,
                        Performance = performaceDbModel,

                        VpReferrerGuid = ecSeedModel.VpReferrerGuid,
                        ExpertRefereeGuid = ecSeedModel.ExpertRefereeGuid,

                        ReferenceDate = ecSeedModel.ReferenceDate,
                        ConsiderationDate = ecSeedModel.ConsiderationDate,

                        IsRejected = ecSeedModel.IsRejected,
                        ExpertDescription = ecSeedModel.RejectDescription,
                        Score = ecSeedModel.Score,

                        //********** Archive **********
                        //IsArchived = ecSeedModel.IsArchived,
                    };

                    performaceDbModel.ExpertConsiderations.Add(myEcDbModel);
                }
            }
            return performaceDbModel;
        }
        return null;
    }



    //**************** Seed ExpertConsiderations **************
    public async Task ExpertConsideration_UpdateSeed(Assessment_ExpertConsiderationDbModel ecDbModel)
    {
        Assessment_ExpertConsiderationSeedModel ecSeedModel = new()
        {
            Guid = ecDbModel.Guid,
            PerformanceGuid = ecDbModel.Performance.Guid,

            VpReferrerGuid = ecDbModel.VpReferrerGuid,
            ExpertRefereeGuid = ecDbModel.ExpertRefereeGuid,

            ReferenceDate = ecDbModel.ReferenceDate,
            ConsiderationDate = ecDbModel.ConsiderationDate,

            IsRejected = ecDbModel.IsRejected,
            RejectDescription = ecDbModel.ExpertDescription,
            Score = ecDbModel.Score,

            //********** Archive **********
            //IsArchived = ecDbModel.IsArchived,
        };

        string json = JsonSerializer.Serialize(ecSeedModel);
        string fileSeedPath = Path.Combine(ExpertConsideration_SeedDirectoryInfo.FullName, ecDbModel.Guid);

        await File.WriteAllTextAsync(fileSeedPath, json);
    }

    public void ExpertConsideration_DeleteSeed(Assessment_ExpertConsiderationDbModel ecDbModel)
    {
        string ecSeedPath = Path.Combine(ExpertConsideration_SeedDirectoryInfo.FullName, ecDbModel.Guid);
        File.Delete(ecSeedPath);
    }

    private async Task ExpertConsideration_SeedDb()
    {
        foreach (var fileInfo in ExpertConsideration_SeedDirectoryInfo.EnumerateFiles())
        {
            var myEcDbModel = await assessment_Db.ExpertConsiderations.FirstOrDefaultAsync(ec => ec.Guid == fileInfo.Name);
            if (myEcDbModel != null) continue;

            string json = await File.ReadAllTextAsync(fileInfo.FullName);
            Assessment_ExpertConsiderationSeedModel? ecSeedModel;
            try
            {
                ecSeedModel = JsonSerializer.Deserialize<Assessment_ExpertConsiderationSeedModel>(json);
            }
            catch
            {
                //log
                continue;
            }
            if (ecSeedModel is not null)
            {
                var performanceDbModel = await assessment_Db.Performances.FirstOrDefaultAsync(p => p.Guid == ecSeedModel.PerformanceGuid);
                if (performanceDbModel is null)//performance may be archived
                {
                    if (ecSeedModel.Score > 0)
                    {
                        FileInfo performanceSeedFileInfo = new FileInfo(Path.Combine(Performance_SeedDirectoryInfo.FullName, ecSeedModel.PerformanceGuid));
                        if (performanceSeedFileInfo.Exists)
                        {
                            Assessment_PerformanceDbModel? archivedPerformanceDbModel = await ReadArchivedPerformance(ecSeedModel.PerformanceGuid);
                            if (archivedPerformanceDbModel is not null)
                            {
                                await SubmitScoreForStatics(archivedPerformanceDbModel, ecSeedModel.Score, false);
                            }
                        }
                    }
                    continue;
                }

                myEcDbModel = new Assessment_ExpertConsiderationDbModel()
                {
                    Guid = ecSeedModel.Guid,
                    //PerformanceGuid = ecSeedModel.Performance.Guid,
                    Performance = performanceDbModel,

                    VpReferrerGuid = ecSeedModel.VpReferrerGuid,
                    ExpertRefereeGuid = ecSeedModel.ExpertRefereeGuid,

                    ReferenceDate = ecSeedModel.ReferenceDate,
                    ConsiderationDate = ecSeedModel.ConsiderationDate,

                    IsRejected = ecSeedModel.IsRejected,
                    ExpertDescription = ecSeedModel.RejectDescription,
                    Score = ecSeedModel.Score,

                    //********** Archive **********
                    //IsArchived = ecSeedModel.IsArchived,
                };
                await assessment_Db.ExpertConsiderations.AddAsync(myEcDbModel);
                if (myEcDbModel.Score > 0)
                {
                    await SubmitScoreForStatics(performanceDbModel, myEcDbModel.Score, false);
                }
            }
        }
        await assessment_Db.SaveChangesAsync();
    }


    //**************** Seed UserBranchPostStatics **************
    /*
    public async Task UserBranchPostStatics_UpdateSeed(Assessment_UserBranchStaticsDbModel ubpDbModel)
    {
        Assessment_UserBarnchPostStaticsSeedModel ubpSeedModel = new()
        {
            Guid = ubpDbModel.Guid,
            UserGuid = ubpDbModel.UserGuid,
            UserPost = ubpDbModel.UserPost,
            Branch = ubpDbModel.Branch,
        };

        string json = JsonSerializer.Serialize(ubpSeedModel);
        string fileSeedPath = Path.Combine(UserBranchPostStatics_SeedDirectoryInfo.FullName, ubpDbModel.Guid);

        await File.WriteAllTextAsync(fileSeedPath, json);
    }

    public void UserBranchPostStatics_DeleteSeed(Assessment_UserBranchStaticsDbModel ubpDbModel)
    {
        string ubpSeedPath = Path.Combine(UserBranchPostStatics_SeedDirectoryInfo.FullName, ubpDbModel.Guid);
        File.Delete(ubpSeedPath);
    }

    public async Task UserBranchPostStatics_SeedDb()
    {
        foreach (var fileInfo in UserBranchPostStatics_SeedDirectoryInfo.EnumerateFiles())
        {
            var myUbpDbModel = await assessment_Db.UserBranchStatics.FirstOrDefaultAsync(ubp => ubp.Guid == fileInfo.Name);
            if (myUbpDbModel != null) continue;

            string json = await File.ReadAllTextAsync(fileInfo.FullName);
            Assessment_UserBarnchPostStaticsSeedModel? ubpSeedModel;
            try
            {
                ubpSeedModel = JsonSerializer.Deserialize<Assessment_UserBarnchPostStaticsSeedModel>(json);
            }
            catch
            {
                //log
                continue;
            }
            if (ubpSeedModel is not null)
            {
                myUbpDbModel = new Assessment_UserBranchStaticsDbModel()
                {
                    Guid = ubpSeedModel.Guid,
                    UserGuid = ubpSeedModel.UserGuid,
                    UserPost = ubpSeedModel.UserPost,
                    Branch = ubpSeedModel.Branch,
                };
                await assessment_Db.UserBranchStatics.AddAsync(myUbpDbModel);
            }
        }
        await assessment_Db.SaveChangesAsync();
    }


    //**************** Seed EachMonthStaticss **************
    public async Task EachMonthStatics_UpdateSeed(Assessment_EachMonthStaticsDbModel emDbModel)
    {
        Assessment_EachMonthStaticsSeedModel emSeedModel = new()
        {
            UserBranchPostGuid = emDbModel.UserBranch.Guid,
            RegistrarDateTime = emDbModel.RegistrarDateTime,
            Year = emDbModel.Year,
            MonthName = emDbModel.MonthName,
            TotalScore = emDbModel.TotalScore,
            TotalPerformances = emDbModel.TotalPerformances,
        };

        string json = JsonSerializer.Serialize(emSeedModel);
        string fileSeedPath = Path.Combine(EachMonthStatics_SeedDirectoryInfo.FullName, emDbModel.Guid);

        await File.WriteAllTextAsync(fileSeedPath, json);
    }

    public void EachMonthStatics_DeleteSeed(Assessment_EachMonthStaticsDbModel emDbModel)
    {
        string emSeedPath = Path.Combine(EachMonthStatics_SeedDirectoryInfo.FullName, emDbModel.Guid);
        File.Delete(emSeedPath);
    }

    public async Task EachMonthStatics_SeedDb()
    {
        foreach (var fileInfo in EachMonthStatics_SeedDirectoryInfo.EnumerateFiles())
        {
            var myEmDbModel = await assessment_Db.EachMonthStatics.FirstOrDefaultAsync(em => em.Guid == fileInfo.Name);
            if (myEmDbModel != null) continue;

            string json = await File.ReadAllTextAsync(fileInfo.FullName);
            Assessment_EachMonthStaticsSeedModel? emSeedModel;
            try
            {
                emSeedModel = JsonSerializer.Deserialize<Assessment_EachMonthStaticsSeedModel>(json);
            }
            catch
            {
                //log
                continue;
            }
            if (emSeedModel is not null)
            {
                var ubpStatics = await assessment_Db.UserBranchStatics.FirstOrDefaultAsync(ubp => ubp.Guid == emSeedModel.UserBranchPostGuid);
                if (ubpStatics is null) continue;

                myEmDbModel = new Assessment_EachMonthStaticsDbModel()
                {
                    UserBranch = ubpStatics,
                    RegistrarDateTime = emSeedModel.RegistrarDateTime,
                    Year = emSeedModel.Year,
                    MonthName = emSeedModel.MonthName,
                    TotalScore = emSeedModel.TotalScore,
                    TotalPerformances = emSeedModel.TotalPerformances,
                };
                await assessment_Db.EachMonthStatics.AddAsync(myEmDbModel);
            }
        }
        await assessment_Db.SaveChangesAsync();
    }
*/


}

