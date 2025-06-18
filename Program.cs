using App.Filters;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //test git
        //******************* SQL Server DataBase Services *******************
        //***** Identity *****
        /*builder.Services.AddDbContext<IdentityDb>(opts =>
        {
            opts.UseSqlServer(builder.Configuration["ConnectionStrings:IdentityConnection"]);
        });
        builder.Services.AddIdentity<MyIdentityUser, MyIdentityRole>(options =>
        {
            //options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 5;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<IdentityDb>();
        builder.Services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.Zero;
        });

        //***** AdminAllMessages *****
        builder.Services.AddDbContext<Messages_DbContext>(opts =>
        {
            opts.UseSqlServer(builder.Configuration["ConnectionStrings:MessagesDbConnection"]);
        });

        //***** Regulations *****
        builder.Services.AddDbContext<Regulations_DbContext>(opts =>
        {
            opts.UseSqlServer(builder.Configuration["ConnectionStrings:RegulationsDbConnection"]);
        });

        //***** Downloads *****
        builder.Services.AddDbContext<Downloads_DbContext>(opts =>
        {
            opts.UseSqlServer(builder.Configuration["ConnectionStrings:DownloadsDbConnection"]);
        });

        //***** Assessment *****
        builder.Services.AddDbContext<Assessment_DbContext>(opts =>
        {
            opts.UseSqlServer(builder.Configuration["ConnectionStrings:AssessmentDbConnection"],
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        });

        //***** Orders *****
        builder.Services.AddDbContext<Orders_DbContext>(opts =>
        {
            opts.UseSqlServer(builder.Configuration["ConnectionStrings:OrdersDbConnection"]);
        });*/

        //******************* MySQL Server DataBase Services *******************
        //***** Identity *****
        builder.Services.AddDbContext<IdentityDb>(opts =>
        {
            opts.UseMySql(builder.Configuration["ConnectionStrings_MySql:IdentityConnection"], new MySqlServerVersion(new Version(80, 0, 42)));
        });
        builder.Services.AddIdentity<MyIdentityUser, MyIdentityRole>(options =>
        {
            //options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 5;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<IdentityDb>();
        builder.Services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.Zero;
        });

        //***** AdminAllMessages *****
        builder.Services.AddDbContext<Messages_DbContext>(opts =>
        {
            opts.UseMySql(builder.Configuration["ConnectionStrings_MySql:MessagesDbConnection"], new MySqlServerVersion(new Version(80, 0, 42)));
        });

        //***** Regulations *****
        builder.Services.AddDbContext<Regulations_DbContext>(opts =>
        {
            opts.UseMySql(builder.Configuration["ConnectionStrings_MySql:RegulationsDbConnection"], new MySqlServerVersion(new Version(80, 0, 42)));
        });

        //***** Downloads *****
        builder.Services.AddDbContext<Downloads_DbContext>(opts =>
        {
            opts.UseMySql(builder.Configuration["ConnectionStrings_MySql:DownloadsDbConnection"], new MySqlServerVersion(new Version(80, 0, 42)));
        });

        //***** Assessment *****
        builder.Services.AddDbContext<Assessment_DbContext>(opts =>
        {
            opts.UseMySql(builder.Configuration["ConnectionStrings_MySql:AssessmentDbConnection"], new MySqlServerVersion(new Version(80, 0, 42)),
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        });

        //***** Orders *****
        builder.Services.AddDbContext<Orders_DbContext>(opts =>
        {
            opts.UseMySql(builder.Configuration["ConnectionStrings_MySql:OrdersDbConnection"], new MySqlServerVersion(new Version(80, 0, 42)));
        });

        //****************************** Services *****************************
        builder.Services.AddSingleton<Assessment_Info>();
        builder.Services.AddScoped<Assessment_Process>();

        builder.Services.AddSingleton<UploadLargeFile>();

        builder.Services.AddScoped<Messages_Process>();
        builder.Services.AddSingleton<Messages_Info>();

        builder.Services.AddScoped<Notification_Process>();

        builder.Services.AddSingleton<PersianDateProcess>();

        builder.Services.AddScoped<Account_Process>();

        builder.Services.AddScoped<Downloads_Process>();
        builder.Services.AddSingleton<Downloads_Info>();

        builder.Services.AddScoped<Regulations_Process>();
        builder.Services.AddSingleton<Regulation_Info>();

        builder.Services.AddScoped<Orders_Process>();
        builder.Services.AddSingleton<Orders_Info>();

        builder.Services.AddSingleton<DisplayMedia_Info>();

        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add<DisableControllerFilter>();
        });

        //******************************** app ********************************
        var app = builder.Build();

        app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapDefaultControllerRoute();

        //************************** Seed SQL Server DataBases ***************************
        IWebHostEnvironment env = app.Services.GetRequiredService<IWebHostEnvironment>();

        //***** Seed "admin" Identity *****
        IdentityDb identityDb = app.Services.CreateScope().ServiceProvider.GetRequiredService<IdentityDb>();
        identityDb.Database.Migrate();
        UserManager<MyIdentityUser> userManager = app.Services.CreateScope().ServiceProvider.GetRequiredService<UserManager<MyIdentityUser>>();
        RoleManager<MyIdentityRole> roleManager = app.Services.CreateScope().ServiceProvider.GetRequiredService<RoleManager<MyIdentityRole>>();
        MyIdentityUser? admin = await userManager.FindByNameAsync("admin");
        if (admin == null)
        {
            admin = new MyIdentityUser
            {
                UserName = "admin",
                UserGuid = "admin",
                PasswordLiteral = builder.Configuration["Identity:AdminPassword"]!,
                FullName = "ادمین",
                Branch = string.Empty,
                Post = "مدیر وب سایت",
                Description = "اکانت مخصوص مدیر کل وب سایت"
            };
            IdentityResult result = await userManager.CreateAsync(admin, admin.PasswordLiteral);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine(error.Description);
                }
                return;
            }
        }
        /*if (admin.Branch != string.Empty)
        {
            admin.Branch = string.Empty;
            await userManager.UpdateAsync(admin);
        }

        //***** Seed Roles *****
        
        /*if (await roleManager.FindByNameAsync("Admins") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Admins") { Description = "مدیران کل وب سایت" });
            await userManager.AddToRoleAsync(admin, "Admins");
        }*/

        if (await roleManager.FindByNameAsync("Account_Admins") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Account_Admins") { Description = "مدیران سرویس کاربران" });
            await userManager.AddToRoleAsync(admin, "Account_Admins");
        }

        if (await roleManager.FindByNameAsync("Downloads_Admins") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Downloads_Admins") { Description = "مدیران سرویس دانلودها" });
            await userManager.AddToRoleAsync(admin, "Downloads_Admins");
        }

        if (await roleManager.FindByNameAsync("Regulations_Admins") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Regulations_Admins") { Description = "مدیران سرویس دستورالعملها" });
            await userManager.AddToRoleAsync(admin, "Regulations_Admins");
        }

        if (await roleManager.FindByNameAsync("Messages_Admins") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Messages_Admins") { Description = "مدیران سرویس پیامها" });
            await userManager.AddToRoleAsync(admin, "Messages_Admins");
        }

        if (await roleManager.FindByNameAsync("Performance_Registrars") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Performance_Registrars") { Description = "ثبت کنندگان عملکرد، دارای مجوز ثبت، ویرایش و حذف عملکرد" });
            await userManager.AddToRoleAsync(admin, "Performance_Registrars");
        }

        if (await roleManager.FindByNameAsync("Performance_Confirmers") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Performance_Confirmers") { Description = "مدیران تایید کننده، دارای مجوز تایید عملکرد کارمندان و ارسال عملکرد به معاونین" });
            await userManager.AddToRoleAsync(admin, "Performance_Confirmers");
        }

        if (await roleManager.FindByNameAsync("VP_Referrers") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("VP_Referrers") { Description = "معاونین، دارای مجوز مشاهده و ارجاع عملکردها" });
            await userManager.AddToRoleAsync(admin, "VP_Referrers");
        }

        if (await roleManager.FindByNameAsync("Expert_Referees") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Expert_Referees") { Description = "کارشناسان، دارای مجوز مشاهده، بررسی و تایید یا مرجوع عملکردها" });
            await userManager.AddToRoleAsync(admin, "Expert_Referees");
        }

        if (await roleManager.FindByNameAsync("Branch_Chiefs") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Branch_Chiefs") { Description = "رئیسها، مدیران و سرپرستان عملکرد شعبات که امتیاز هر عملکرد شعبه برای آنها نیز لحاظ میگردد" });
            await userManager.AddToRoleAsync(admin, "Branch_Chiefs");
        }

        if (await roleManager.FindByNameAsync("Can_Watch_Backgrounds") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Can_Watch_Backgrounds") { Description = "مدیرانی که (مدیرکل) میتوانند سوابق عملکرد تمام کارکنان را ببینند" });
            await userManager.AddToRoleAsync(admin, "Can_Watch_Backgrounds");
        }

        if (await roleManager.FindByNameAsync("Order_Registrars") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Order_Registrars") { Description = "کارشناسانی که میتوانند درخواست جدید برای اداره ها ثبت کنند" });
            await userManager.AddToRoleAsync(admin, "Order_Registrars");
        }

        if (await roleManager.FindByNameAsync("Can_Watch_Orders") == null)
        {
            await roleManager.CreateAsync(new MyIdentityRole("Can_Watch_Orders") { Description = "مدیرانی که (مدیرکل) میتوانند تمام درخواست ها برای تمام اداره ها را ببینند" });
            await userManager.AddToRoleAsync(admin, "Can_Watch_Orders");
        }


        /********************** migrate pending databases **********************/
        Messages_DbContext adminAllMessagesDb = app.Services.CreateScope().ServiceProvider.GetRequiredService<Messages_DbContext>();
        adminAllMessagesDb.Database.Migrate();

        Regulations_DbContext regulationsDb = app.Services.CreateScope().ServiceProvider.GetRequiredService<Regulations_DbContext>();
        regulationsDb.Database.Migrate();

        Downloads_DbContext downloadsDb = app.Services.CreateScope().ServiceProvider.GetRequiredService<Downloads_DbContext>();
        downloadsDb.Database.Migrate();

        Assessment_DbContext assessmentDb = app.Services.CreateScope().ServiceProvider.GetRequiredService<Assessment_DbContext>();
        assessmentDb.Database.Migrate();

        Orders_DbContext ordersDb = app.Services.CreateScope().ServiceProvider.GetRequiredService<Orders_DbContext>();
        ordersDb.Database.Migrate();

        Console.WriteLine("** All DB Migration Completed! **");


        /********************** seed databases **********************/
        if (app.Configuration["Seed:Account"] == "true")
        {
            Console.WriteLine("** Seeding Account Service **");
            Account_Process account_Process = app.Services.CreateScope().ServiceProvider.GetRequiredService<Account_Process>();
            await account_Process.SeedDb();
            Console.WriteLine("** Seeding Account Service Completed! **");
        }

        Assessment_Process assessment_Process = app.Services.CreateScope().ServiceProvider.GetRequiredService<Assessment_Process>();
        if (app.Configuration["Seed:Assessment"] == "true")
        {
            Console.WriteLine("** Seeding Assessment Service **");
            await assessment_Process.SeedDb();
            Console.WriteLine("** Seeding Assessment Service Completed! **");
        }
        await assessment_Process.CalculateTopRecentStatics();

        if (app.Configuration["Seed:Downloads"] == "true")
        {
            Console.WriteLine("** Seeding Downloads Service **");
            Downloads_Process downloads_Process = app.Services.CreateScope().ServiceProvider.GetRequiredService<Downloads_Process>();
            await downloads_Process.SeedDb();
            Console.WriteLine("** Seeding Downloads Service Completed! **");
        }
        if (app.Configuration["Seed:Messages"] == "true")
        {
            Console.WriteLine("** Seeding Messages Service **");
            Messages_Process messages_Process = app.Services.CreateScope().ServiceProvider.GetRequiredService<Messages_Process>();
            await messages_Process.SeedDb();
            Console.WriteLine("** Seeding Messages Service Completed! **");
        }
        if (app.Configuration["Seed:Regulations"] == "true")
        {
            Console.WriteLine("** Seeding Regulations Service **");
            Regulations_Process regulations_Process = app.Services.CreateScope().ServiceProvider.GetRequiredService<Regulations_Process>();
            await regulations_Process.SeedDb();
            Console.WriteLine("** Seeding Regulations Service Completed! **");
        }
        if (app.Configuration["Seed:Orders"] == "true")
        {
            Console.WriteLine("** Seeding Orders Service **");
            Orders_Process orders_Process = app.Services.CreateScope().ServiceProvider.GetRequiredService<Orders_Process>();
            await orders_Process.SeedDb();
            Console.WriteLine("** Seeding Orders Service Completed! **");
        }

        //*********** run ***********
        Console.WriteLine("** app.Run() **");
        app.Run();
    }
}
