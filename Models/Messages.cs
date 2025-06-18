using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace App.Models;

public class Messages_DbContext : DbContext
{
    public Messages_DbContext(DbContextOptions<Messages_DbContext> options) : base(options) { }

    public DbSet<Messages_DbModel> Messages { get; set; } = null!;

}

public class Messages_DbModel
{
    public int Id { get; set; }
    public string MessageGuid { get; set; } = string.Empty;
    public string FromUserGuid { get; set; } = string.Empty;
    public string ToUsersGuidsCommaSeparated { get; set; } = string.Empty;
    [NotMapped]
    public List<string> ToUsersGuids
    {
        get => [.. ToUsersGuidsCommaSeparated.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        set => ToUsersGuidsCommaSeparated = "," + string.Join(",", value) + ",";
    }
    public string Subject { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public string? AttachedFileName { get; set; } = null;
    public string SeenByClientsGuidsCommaSeparated { get; set; } = string.Empty;
    [NotMapped]
    public List<string> SeenByClientsGuids
    {
        get => [.. SeenByClientsGuidsCommaSeparated.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        set => SeenByClientsGuidsCommaSeparated = "," + string.Join(",", value) + ",";
    }
    public DateTime Date { get; set; } = DateTime.Now;
}

public class Messages_NewMessageModel
{
    public string Subject { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public IFormFile? AttachedFile { get; set; } = null;
    public List<MyIdentityUser>? UsersList { get; set; }
    public string ReceiversGuid { get; set; } = string.Empty;
    public MyIdentityUser SenderUser { get; set; } = null!;
}

public class Messages_FormModel
{
    public string Subject { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public string ToUserGuids { get; set; } = string.Empty;
}

public class Messages_ListRowModel
{
    /*public string FromUserName { get; set; } = string.Empty;
    public int FromProfileImageVersion { get; set; } = 0;
    public string FromFullName { get; set; } = string.Empty;*/
    public MyIdentityUser? FromUser { get; set; }
    /*public string Branch { get; set; } = string.Empty;
    public string Post { get; set; } = string.Empty;*/
    public string Subject { get; set; } = string.Empty;
    public string MessageGuid { get; set; } = string.Empty;
    public bool IsSeen { get; set; } = false;
    public DateTime Date { get; set; } = DateTime.Now;
}

public class Messages_OpenModel
{
    public string MessageGuid { get; set; } = string.Empty;
    public MyIdentityUser? FromUser { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public string? AttachedFileName { get; set; } = null;
    public List<MyIdentityUser> SeenByClients { get; set; } = [];
    public DateTime Date { get; set; } = DateTime.Now;
    public List<MyIdentityUser> ToUsers { get; set; } = new();
    //public bool ToAll { get; set; } = false;
}

public class Messages_SeedModel
{
    public string MessageGuid { get; set; } = string.Empty;
    public string FromUserGuid { get; set; } = string.Empty;
    public string ToUsersGuidsCommaSeparated { get; set; } = string.Empty;
    //public List<string> ToUsersGuids { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public string? AttachedFileName { get; set; } = null;
    public string SeenByClientsGuidsCommaSeparated { get; set; } = string.Empty;
    //public List<string> SeenByClientsGuids { get; set; } = new();
    public DateTime Date { get; set; } = DateTime.Now;
    //public bool IsArchived { get; set; } = false;
}

//singleton
public class Messages_Info
{
    public DateTime? LastTimeDoneRemoveOlderMessages = null;
}

//scoped
public class Messages_Process
{
    public readonly int NumberOfMessagesPerPage = 10;
    readonly UserManager<MyIdentityUser> userManager;
    readonly Messages_DbContext messageDb;
    readonly DirectoryInfo SeedDirectoryInfo;
    readonly IWebHostEnvironment env;
    readonly Messages_Info messages_Info;
    public Messages_Process(UserManager<MyIdentityUser> userManager, Messages_DbContext messageDb,
    IWebHostEnvironment env, Messages_Info messages_Info)
    {
        this.userManager = userManager;
        this.messageDb = messageDb;
        this.env = env;
        this.messages_Info = messages_Info;
        SeedDirectoryInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Storage", "Messages", "DbSeed"));
    }

    public async Task<List<Messages_ListRowModel>> GetMessagesList(MyIdentityUser user/*, int page*/)
    {
        List<Messages_ListRowModel> messageList = new();

        if (await userManager.IsInRoleAsync(user, "Messages_Admins")/*user.UserName == "admin"*/)
        {
            List<Messages_DbModel> allMessages =
            await messageDb.Messages.OrderByDescending(m => m.Date)
            //.Skip((page - 1) * NumberOfMessagesPerPage).Take(NumberOfMessagesPerPage)
            .ToListAsync();

            //allMessages = allMessages.Skip((page - 1) * 10).Take(10).ToList();

            foreach (Messages_DbModel messageDbModel in allMessages)
            {
                MyIdentityUser? fromUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == messageDbModel.FromUserGuid);
                Messages_ListRowModel rowData = new()
                {
                    FromUser = fromUser,
                    Subject = messageDbModel.Subject,
                    MessageGuid = messageDbModel.MessageGuid,
                    IsSeen = messageDbModel.SeenByClientsGuids.Contains(user.UserGuid),
                    Date = messageDbModel.Date,
                };
                messageList.Add(rowData);
            }
        }
        else
        {
            string commaSeparatedUserGuid = "," + user.UserGuid + ",";
            List<Messages_DbModel> adminMessages = await messageDb.Messages
            .Where(m => m.FromUserGuid == user.UserGuid ||
                m.ToUsersGuidsCommaSeparated.Contains(commaSeparatedUserGuid))
            .OrderByDescending(m => m.Date)
            .ToListAsync();
            //.Skip((page - 1) * NumberOfMessagesPerPage).Take(NumberOfMessagesPerPage)
            //.ToList();

            //adminMessages = adminMessages.Skip((page - 1) * 10).Take(10).ToList();

            foreach (Messages_DbModel messageDbModel in adminMessages)
            {
                MyIdentityUser? fromUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == messageDbModel.FromUserGuid);
                Messages_ListRowModel rowData = new()
                {
                    FromUser = fromUser,
                    Subject = messageDbModel.Subject,
                    MessageGuid = messageDbModel.MessageGuid,
                    IsSeen = messageDbModel.SeenByClientsGuids.Contains(user.UserGuid),
                    Date = messageDbModel.Date,
                };
                messageList.Add(rowData);
            }
        }

        return messageList;
    }

    public async Task UpdateMessageSeed(Messages_DbModel messageDbModel)
    {
        Messages_SeedModel fileSeedModel = new()
        {
            AttachedFileName = messageDbModel.AttachedFileName,
            Date = messageDbModel.Date,
            FromUserGuid = messageDbModel.FromUserGuid,
            MessageGuid = messageDbModel.MessageGuid,
            MessageText = messageDbModel.MessageText,
            SeenByClientsGuidsCommaSeparated = messageDbModel.SeenByClientsGuidsCommaSeparated,
            Subject = messageDbModel.Subject,
            ToUsersGuidsCommaSeparated = messageDbModel.ToUsersGuidsCommaSeparated,
            //IsArchived = messageDbModel.IsArchived
        };

        string json = JsonSerializer.Serialize(fileSeedModel);
        string fileSeedPath = Path.Combine(SeedDirectoryInfo.FullName, messageDbModel.MessageGuid);

        await File.WriteAllTextAsync(fileSeedPath, json);
    }

    public void DeleteMessageSeed(Messages_DbModel messageDbModel)
    {
        string messageSeedPath = Path.Combine(SeedDirectoryInfo.FullName, messageDbModel.MessageGuid);
        File.Delete(messageSeedPath);
    }

    public async Task SeedDb()
    {
        foreach (var fileInfo in SeedDirectoryInfo.EnumerateFiles())
        {
            var messageDbModel = await messageDb.Messages.FirstOrDefaultAsync(f => f.MessageGuid == fileInfo.Name);
            if (messageDbModel != null) continue;

            string json = await File.ReadAllTextAsync(fileInfo.FullName);
            Messages_SeedModel? messageSeedModel;
            try
            {
                messageSeedModel = JsonSerializer.Deserialize<Messages_SeedModel>(json);
            }
            catch
            {
                //log
                continue;
            }
            if (messageSeedModel is not null)
            {
                messageDbModel = new Messages_DbModel()
                {
                    AttachedFileName = messageSeedModel.AttachedFileName,
                    Date = messageSeedModel.Date,
                    FromUserGuid = messageSeedModel.FromUserGuid,
                    MessageGuid = messageSeedModel.MessageGuid,
                    MessageText = messageSeedModel.MessageText,
                    SeenByClientsGuidsCommaSeparated = messageSeedModel.SeenByClientsGuidsCommaSeparated,
                    Subject = messageSeedModel.Subject,
                    ToUsersGuidsCommaSeparated = messageSeedModel.ToUsersGuidsCommaSeparated,
                    //IsArchived = messageSeedModel.IsArchived
                };
                await messageDb.Messages.AddAsync(messageDbModel);
            }
        }
        await messageDb.SaveChangesAsync();
    }

    public async Task RemoveOlderMessages(DateTime oldDateTime)
    {
        if (DateTime.Today != messages_Info.LastTimeDoneRemoveOlderMessages)
        {
            var oldMessages = await messageDb.Messages
            .Where(m => m.Date < oldDateTime)
            .ToListAsync();

            foreach (Messages_DbModel messageDbModel in oldMessages)
            {
                DeleteMessageSeed(messageDbModel);
                messageDb.Messages.Remove(messageDbModel);
            }
            await messageDb.SaveChangesAsync();

            messages_Info.LastTimeDoneRemoveOlderMessages = DateTime.Today;
            Console.WriteLine($"***** Assessment_Process.RemoveOlderMessages() in {messages_Info.LastTimeDoneRemoveOlderMessages} *****");
        }
    }

}