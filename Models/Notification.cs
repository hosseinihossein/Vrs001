using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Models;

public class Notification_Process
{
    readonly UserManager<MyIdentityUser> userManager;
    public Notification_Process(UserManager<MyIdentityUser> userManager)
    {
        this.userManager = userManager;
    }
    public async Task SetNewNotif_ToAll(string notifName)
    {
        foreach (MyIdentityUser user in await userManager.Users.ToListAsync())
        {
            if (!user.Notifications.Contains(notifName))
            {
                //user.Notifications.Add(notifName);
                user.Notifications = [.. user.Notifications, notifName];
                await userManager.UpdateAsync(user);
            }
        }
    }
    public async Task SetNewNotif_ToUser(string notifName, string userGuid)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is not null && !user.Notifications.Contains(notifName))
        {
            //user.Notifications.Add(notifName);
            user.Notifications = [.. user.Notifications, notifName];
            await userManager.UpdateAsync(user);
        }
    }
    public async Task SetNewNotif_ToUser(string notifName, MyIdentityUser user)
    {
        if (user is not null && !user.Notifications.Contains(notifName))
        {
            //user.Notifications.Add(notifName);
            user.Notifications = [.. user.Notifications, notifName];
            await userManager.UpdateAsync(user);
        }
    }
    public async Task RemoveNotif_FromAll(string notifName)
    {
        foreach (MyIdentityUser user in await userManager.Users.ToListAsync())
        {
            //user.Notifications.Remove(notifName);
            List<string> myUserNotifications = user.Notifications;
            myUserNotifications.Remove(notifName);
            user.Notifications = myUserNotifications;
            await userManager.UpdateAsync(user);
        }
    }
    public async Task RemoveNotif_FromUser(string notifName, string userGuid)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is not null)
        {
            //user.Notifications.Remove(notifName);
            List<string> myUserNotifications = user.Notifications;
            myUserNotifications.Remove(notifName);
            user.Notifications = myUserNotifications;
            await userManager.UpdateAsync(user);
        }
    }
    public async Task RemoveNotif_FromUser(string notifName, MyIdentityUser user)
    {
        if (user is not null)
        {
            //user.Notifications.Remove(notifName);
            List<string> myUserNotifications = user.Notifications;
            myUserNotifications.Remove(notifName);
            user.Notifications = myUserNotifications;
            await userManager.UpdateAsync(user);
        }
    }

}