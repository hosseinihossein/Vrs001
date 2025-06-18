using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace App.Models;

public class Downloads_DbContext : DbContext
{
    public Downloads_DbContext(DbContextOptions<Downloads_DbContext> options) : base(options) { }

    public DbSet<Downloads_FileDbModel> Files { get; set; } = null!;
}

public class Downloads_FileDbModel
{
    public int Id { get; set; }
    public string Guid { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public class Downloads_FormModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class Downloads_FileSeedModel
{
    public string Guid { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

//singleton
public class Downloads_Info(IConfiguration configuration)
{
    public List<string> Categories { get; set; } =
    configuration.GetSection("DownloadCategories").Get<List<string>>() ?? [];
}

//scoped
public class Downloads_Process
{
    readonly Downloads_DbContext downloadsDb;
    readonly DirectoryInfo SeedDirectoryInfo;
    readonly IWebHostEnvironment env;
    public Downloads_Process(IWebHostEnvironment env, Downloads_DbContext downloadsDb)
    {
        this.env = env;
        this.downloadsDb = downloadsDb;
        SeedDirectoryInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Storage", "Downloads", "DbSeed"));
    }
    public async Task UpdateFileSeed(Downloads_FileDbModel fileDbModel)
    {
        Downloads_FileSeedModel fileSeedModel = new()
        {
            Guid = fileDbModel.Guid,
            Title = fileDbModel.Title,
            Description = fileDbModel.Description,
            Category = fileDbModel.Category,
            FileName = fileDbModel.FileName,
        };

        string json = JsonSerializer.Serialize(fileSeedModel);
        string fileSeedPath = Path.Combine(SeedDirectoryInfo.FullName, fileDbModel.Guid);

        await File.WriteAllTextAsync(fileSeedPath, json);
    }

    public void DeleteFileSeed(Downloads_FileDbModel fileDbModel)
    {
        string fileSeedPath = Path.Combine(SeedDirectoryInfo.FullName, fileDbModel.Guid);
        File.Delete(fileSeedPath);
    }

    public async Task SeedDb()
    {
        foreach (var fileInfo in SeedDirectoryInfo.EnumerateFiles())
        {
            var myFileDbModel = await downloadsDb.Files.FirstOrDefaultAsync(f => f.Guid == fileInfo.Name);
            if (myFileDbModel != null) continue;

            string json = await File.ReadAllTextAsync(fileInfo.FullName);
            Downloads_FileSeedModel? fileSeedModel;
            try
            {
                fileSeedModel = JsonSerializer.Deserialize<Downloads_FileSeedModel>(json);
            }
            catch
            {
                //log
                continue;
            }
            if (fileSeedModel is not null)
            {
                myFileDbModel = new Downloads_FileDbModel()
                {
                    Guid = fileSeedModel.Guid,
                    Title = fileSeedModel.Title,
                    Description = fileSeedModel.Description,
                    Category = fileSeedModel.Category,
                    FileName = fileSeedModel.FileName,
                };
                await downloadsDb.Files.AddAsync(myFileDbModel);
            }
        }
        await downloadsDb.SaveChangesAsync();
    }

}