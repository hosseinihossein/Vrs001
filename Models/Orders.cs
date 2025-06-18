using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace App.Models;

public class Orders_DbContext : DbContext
{
    public Orders_DbContext(DbContextOptions<Orders_DbContext> options) : base(options) { }

    public DbSet<Orders_DbModel> Orders { get; set; } = null!;
}

public class Orders_DbModel
{
    public int Id { get; set; }
    public string Guid { get; set; } = System.Guid.NewGuid().ToString().Replace("-", "");
    public string RegistrarGuid { get; set; } = string.Empty;
    public string PerformanceType { get; set; } = string.Empty;
    public string PerformanceField { get; set; } = string.Empty;
    public DateTime RegisterDate { get; set; } = DateTime.Now;
    public DateTime Deadline { get; set; } = DateTime.Today + TimeSpan.FromDays(7);
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string BranchesCommaSeparated { get; set; } = string.Empty;
    [NotMapped]
    public List<string> Branches
    {
        get => [.. BranchesCommaSeparated.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        set => BranchesCommaSeparated = "," + string.Join(",", value) + ",";
    }
    //public bool IsArchived { get; set; } = false;
}


//****************************
public class Orders_OpenModel
{
    public string Guid { get; set; } = System.Guid.NewGuid().ToString().Replace("-", "");
    public MyIdentityUser? Registrar { get; set; }
    public string PerformanceType { get; set; } = string.Empty;
    public string PerformanceField { get; set; } = string.Empty;
    public DateTime RegisterDate { get; set; } = DateTime.Now;
    public DateTime Deadline { get; set; } = DateTime.Today + TimeSpan.FromDays(7);
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, List<Assessment_PerformanceDbModel>> BranchPerformances { get; set; } = [];
    public MyIdentityUser MeOpener { get; set; } = null!;
}

public class Orders_FormModel
{
    public string PerformanceType { get; set; } = string.Empty;
    public string PerformanceField { get; set; } = string.Empty;
    public string Deadline { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Branches { get; set; } = [];
}

public class Orders_IndexModel
{
    public string? Branch { get; set; }
    public MyIdentityUser? Registrar { get; set; }
    public string FromDate { get; set; } = string.Empty;
    public string? ToDate { get; set; }
    public List<Orders_DbModel> Orders { get; set; } = [];
    public Dictionary<string, DateTime>? OrderGuidRespondDates { get; set; }

}

public class Orders_ReportModel
{
    public string FromDate { get; set; } = string.Empty;
    public string? ToDate { get; set; }
    public List<Orders_DbModel> Orders { get; set; } = [];
    public Dictionary<string, Dictionary<string, DateTime?>> BranchOrderGuidRespondDates { get; set; } = [];
}

public class Orders_SeedModel
{
    public string Guid { get; set; } = System.Guid.NewGuid().ToString().Replace("-", "");
    public string RegistrarGuid { get; set; } = string.Empty;
    public string PerformanceType { get; set; } = string.Empty;
    public string PerformanceField { get; set; } = string.Empty;
    public DateTime RegisterDate { get; set; } = DateTime.Now;
    public DateTime Deadline { get; set; } = DateTime.Today + TimeSpan.FromDays(7);
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Branches { get; set; } = [];
    //public bool IsArchived { get; set; } = false;
}

//singleton
public class Orders_Info
{
    public DateTime? LastTimeDoneRemoveOlderOrders = null;
}

//scoped
public class Orders_Process
{
    readonly DirectoryInfo SeedDirectoryInfo;
    readonly Orders_DbContext ordersDb;
    readonly Orders_Info orders_Info;
    public Orders_Process(IWebHostEnvironment env, Orders_DbContext ordersDb, Orders_Info orders_Info)
    {
        this.ordersDb = ordersDb;
        this.orders_Info = orders_Info;
        SeedDirectoryInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Storage", "Orders", "DbSeed"));
    }

    public async Task UpdateOrderSeed(Orders_DbModel orderDbModel)
    {
        Orders_SeedModel fileSeedModel = new()
        {
            Branches = [.. orderDbModel.Branches],
            Deadline = orderDbModel.Deadline,
            Description = orderDbModel.Description,
            Guid = orderDbModel.Guid,
            PerformanceField = orderDbModel.PerformanceField,
            PerformanceType = orderDbModel.PerformanceType,
            RegisterDate = orderDbModel.RegisterDate,
            RegistrarGuid = orderDbModel.RegistrarGuid,
            Subject = orderDbModel.Subject,
            //IsArchived = orderDbModel.IsArchived
        };

        string json = JsonSerializer.Serialize(fileSeedModel);
        string fileSeedPath = Path.Combine(SeedDirectoryInfo.FullName, orderDbModel.Guid);

        await File.WriteAllTextAsync(fileSeedPath, json);
    }

    public void DeleteOrderSeed(Orders_DbModel orderDbModel)
    {
        string orderSeedPath = Path.Combine(SeedDirectoryInfo.FullName, orderDbModel.Guid);
        File.Delete(orderSeedPath);
    }

    public async Task SeedDb()
    {
        foreach (var fileInfo in SeedDirectoryInfo.EnumerateFiles())
        {
            var ordersDbModel = await ordersDb.Orders.FirstOrDefaultAsync(o => o.Guid == fileInfo.Name);
            if (ordersDbModel != null) continue;

            string json = await File.ReadAllTextAsync(fileInfo.FullName);
            Orders_SeedModel? orderSeedModel;
            try
            {
                orderSeedModel = JsonSerializer.Deserialize<Orders_SeedModel>(json);
            }
            catch
            {
                //log
                continue;
            }
            if (orderSeedModel is not null)
            {
                ordersDbModel = new Orders_DbModel()
                {
                    Branches = [.. orderSeedModel.Branches],
                    Deadline = orderSeedModel.Deadline,
                    Description = orderSeedModel.Description,
                    Guid = orderSeedModel.Guid,
                    PerformanceField = orderSeedModel.PerformanceField,
                    PerformanceType = orderSeedModel.PerformanceType,
                    RegisterDate = orderSeedModel.RegisterDate,
                    RegistrarGuid = orderSeedModel.RegistrarGuid,
                    Subject = orderSeedModel.Subject,
                    //IsArchived = orderSeedModel.IsArchived
                };
                await ordersDb.Orders.AddAsync(ordersDbModel);
            }
        }
        await ordersDb.SaveChangesAsync();
    }

    public async Task RemoveOlderOrders(int days = 30)
    {
        if (DateTime.Today != orders_Info.LastTimeDoneRemoveOlderOrders)
        {
            var oldOrders = (await ordersDb.Orders
            .Where(o => o.Deadline < DateTime.Today)
            .ToListAsync())
            .Where(o => (DateTime.Now - o.Deadline).Days > days);

            foreach (Orders_DbModel orderDbModel in oldOrders)
            {
                DeleteOrderSeed(orderDbModel);
                ordersDb.Orders.Remove(orderDbModel);
            }
            await ordersDb.SaveChangesAsync();

            orders_Info.LastTimeDoneRemoveOlderOrders = DateTime.Today;
            Console.WriteLine($"***** Assessment_Process.RemoveOlderOrders() in {orders_Info.LastTimeDoneRemoveOlderOrders} *****");
        }
    }

}
