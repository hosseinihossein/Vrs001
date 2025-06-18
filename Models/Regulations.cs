using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace App.Models;

public class Regulations_DbContext : DbContext
{
    public Regulations_DbContext(DbContextOptions<Regulations_DbContext> options) : base(options) { }

    public DbSet<Regulations_DbModel> Regulations { get; set; } = null!;
}

public class Regulations_DbModel
{
    public int Id { get; set; }
    public string Guid { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BriefDescription { get; set; } = string.Empty;
    public string? AttachedFileName { get; set; } = null;
}

public class Regulations_FormModel
{
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;
    [StringLength(50)]
    public string Title { get; set; } = string.Empty;
    [StringLength(500)]
    public string Brief { get; set; } = string.Empty;
    public IFormFile? RegulationFile { get; set; } = null;
}

public class Regulations_SeedModel
{
    public string Guid { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BriefDescription { get; set; } = string.Empty;
    public string? AttachedFileName { get; set; } = null;
}

//singleton
public class Regulation_Info(IConfiguration configuration)
{
    public List<string> Categories { get; set; } =
    configuration.GetSection("RegulationCategories").Get<List<string>>() ?? [];
}
//scoped
public class Regulations_Process
{
    readonly Regulations_DbContext regulationsDb;
    readonly DirectoryInfo SeedDirectoryInfo;
    readonly IWebHostEnvironment env;
    public Regulations_Process(IWebHostEnvironment env, Regulations_DbContext regulationsDb)
    {
        this.env = env;
        this.regulationsDb = regulationsDb;
        SeedDirectoryInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Storage", "Regulations", "DbSeed"));
    }
    public async Task UpdateRegulationSeed(Regulations_DbModel regulationDbModel)
    {
        Regulations_SeedModel regulationSeedModel = new()
        {
            Guid = regulationDbModel.Guid,
            Category = regulationDbModel.Category,
            Title = regulationDbModel.Title,
            BriefDescription = regulationDbModel.BriefDescription,
            AttachedFileName = regulationDbModel.AttachedFileName,
        };

        string json = JsonSerializer.Serialize(regulationSeedModel);
        string fileSeedPath = Path.Combine(SeedDirectoryInfo.FullName, regulationDbModel.Guid);

        await File.WriteAllTextAsync(fileSeedPath, json);
    }

    public void DeleteRegulationSeed(Regulations_DbModel regulationDbModel)
    {
        string regulationSeedPath = Path.Combine(SeedDirectoryInfo.FullName, regulationDbModel.Guid);
        File.Delete(regulationSeedPath);
    }

    public async Task SeedDb()
    {
        foreach (var fileInfo in SeedDirectoryInfo.EnumerateFiles())
        {
            var myRegulationDbModel = await regulationsDb.Regulations.FirstOrDefaultAsync(r => r.Guid == fileInfo.Name);
            if (myRegulationDbModel != null) continue;

            string json = await File.ReadAllTextAsync(fileInfo.FullName);
            Regulations_SeedModel? regulationSeedModel;
            try
            {
                regulationSeedModel = JsonSerializer.Deserialize<Regulations_SeedModel>(json);
            }
            catch
            {
                //log
                continue;
            }
            if (regulationSeedModel is not null)
            {
                myRegulationDbModel = new Regulations_DbModel()
                {
                    Guid = regulationSeedModel.Guid,
                    Category = regulationSeedModel.Category,
                    Title = regulationSeedModel.Title,
                    BriefDescription = regulationSeedModel.BriefDescription,
                    AttachedFileName = regulationSeedModel.AttachedFileName,
                };
                await regulationsDb.Regulations.AddAsync(myRegulationDbModel);
            }
        }
        await regulationsDb.SaveChangesAsync();
    }
}