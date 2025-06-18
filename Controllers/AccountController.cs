using System.ComponentModel.DataAnnotations;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace App.Controllers;

[AutoValidateAntiforgeryToken]
public class AccountController : Controller
{
    readonly UserManager<MyIdentityUser> userManager;
    readonly RoleManager<MyIdentityRole> roleManager;
    readonly IWebHostEnvironment env;
    //readonly char ds = Path.DirectorySeparatorChar;
    readonly SignInManager<MyIdentityUser> signInManager;
    readonly Assessment_DbContext assessmentDb;
    readonly IConfiguration configuration;
    readonly Account_Process accountProcess;

    public AccountController(UserManager<MyIdentityUser> userManager, RoleManager<MyIdentityRole> _roleManager,
    IWebHostEnvironment env, SignInManager<MyIdentityUser> signInManager, Assessment_DbContext assessmentDb,
    IConfiguration configuration, Account_Process accountProcess)
    {
        this.userManager = userManager;
        roleManager = _roleManager;
        this.env = env;
        this.signInManager = signInManager;
        this.assessmentDb = assessmentDb;
        this.configuration = configuration;
        this.accountProcess = accountProcess;
    }

    public IActionResult Login(string returnUrl = "/")
    {
        if (User.Identity is null || !User.Identity.IsAuthenticated)
        {
            LoginModel loginModel = new()
            {
                ReturnUrl = returnUrl
            };
            return View(loginModel);
        }
        else
        {
            return RedirectToAction(nameof(Profile));
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitLogin(LoginModel loginModel)
    {
        if (ModelState.IsValid)
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                await signInManager.SignOutAsync();
            }

            MyIdentityUser? user = await userManager.FindByNameAsync(loginModel.UsernameOrEmail);
            if (user is null)
            {
                ModelState.AddModelError("", "نام کاربری وارد شده اشتباه میباشد!");
                return View(nameof(Login));
            }

            if (user.InActive)
            {
                ModelState.AddModelError("", "اکانت غیرفعال میباشد!");
                return View(nameof(Login));
            }

            Microsoft.AspNetCore.Identity.SignInResult result =
            await signInManager.PasswordSignInAsync(user, loginModel.Password, loginModel.IsPersistent ?? false, false);

            if (result.Succeeded)
            {
                return Redirect(loginModel.ReturnUrl ?? "/");
            }
            else
            {
                ModelState.AddModelError("", "پسورد اشتباه میباشد!");
                return View(nameof(Login));
            }

        }
        ModelState.AddModelError("", "نام کاربری یا پسورد وارد شده اشتباه میباشد!");
        return View(nameof(Login));
    }

    [Authorize]
    public async Task<IActionResult> Logout(string? returnUrl)
    {
        await signInManager.SignOutAsync();
        return Redirect(returnUrl ?? "/");
    }

    [Authorize]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize(Roles = "Account_Admins")]
    public IActionResult Signup(/*string? returnUrl*/)
    {
        /*if (User.Identity is null || !User.Identity.IsAuthenticated)
        {*/
        SignupModel signupModel = new();
        return View(signupModel);
        /*}
        else
        {
            return RedirectToAction(nameof(Dashboard));
        }*/
    }

    [HttpPost]
    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> SubmitSignup(SignupModel signupModel)
    {
        if (ModelState.IsValid)
        {
            /*if (User.Identity?.IsAuthenticated ?? false)
            {
                await signInManager.SignOutAsync();
            }*/

            MyIdentityUser user = new MyIdentityUser()
            {
                UserName = signupModel.Username,
                //Email = signupModel.Email,
                //EmailConfirmed = false,
                UserGuid = Guid.NewGuid().ToString().Replace("-", ""),
                PasswordLiteral = signupModel.Password,
                FullName = signupModel.FullName,
                Branch = signupModel.Branch,
                Post = signupModel.Post
            };
            //string branchPost = $"{user.Branch}_{user.Post}";
            //user.SubUsersGuids.Add(branchPost);
            IdentityResult result = await userManager.CreateAsync(user, signupModel.Password);
            if (result.Succeeded)
            {
                // create a user seed
                await accountProcess.UpdateUserSeed(user);

                object resultMessage = $"<h2>کاربر \'{user.UserName}\' با موفقیت ثبت نام شد.</h2>"; //+
                //$"<p>validation token: {validationCode}</p>" +
                //"<p>Please go to <a href=\"/Account/Dashboard\">Dashboard</a> to activate your account</p>";
                ViewBag.ResultState = "success";
                ViewBag.InfoBtnHref = "/Account/UsersList";
                ViewBag.InfoBtnName = "لیست کاربران";
                return View("Result", resultMessage);
            }
            //ModelState.AddModelError("", "Please enter appropriate information");
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
        return View(nameof(Signup));
    }

    [Authorize]
    public async Task<IActionResult> Profile(string? userGuid = null)
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        MyIdentityUser? user = null;
        if (userGuid is not null)
        {
            user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        }
        if (user is null)
        {
            user = me;
        }

        List<MyIdentityUser> allRelatedUsers = await accountProcess.GetAllRelatedUsers(user);
        List<string> allRelatedUsersGuids = [];
        if (allRelatedUsers.Count > 0)
        {
            allRelatedUsersGuids = allRelatedUsers.Select(u => u.UserGuid).ToList();
        }
        else
        {
            allRelatedUsersGuids.Add(user.UserGuid);
        }

        var totalMonthStatics = (await assessmentDb.UserBranchPostStatics
            .Include(ub => ub.EachMonthStatics)
            .Where(ub => /*ub.UserGuid == user.UserGuid*/allRelatedUsersGuids.Contains(ub.UserGuid))
            .ToListAsync())
            .SelectMany(ub => ub.EachMonthStatics);

        int totalScores = totalMonthStatics.Sum(ms => ms.TotalScore);
        int totalPerformances = totalMonthStatics.Sum(ms => ms.TotalPerformances);
        double averageScore = 0;
        if (totalPerformances != 0)
        {
            averageScore = (double)totalScores / (double)totalPerformances;
        }

        Account_Profile profileModel = new()
        {
            MyUser = user,
            TotalScore = totalScores,
            AverageScore = averageScore
        };

        return View(profileModel);

    }

    public async Task<IActionResult> ClientImage(string username)
    {
        MyIdentityUser? client = await userManager.FindByNameAsync(username);
        if (client is null)
        {
            return NotFound();
        }

        string clientImagePath = Path.Combine(env.ContentRootPath, "Storage", "Account", "UserImage", client.UserGuid); //env.ContentRootPath + ds + "Storage" + ds + "Account" + ds + "UserImage" + ds + client.UserGuid;

        if (System.IO.File.Exists(clientImagePath))
        {
            return PhysicalFile(clientImagePath, "Image/*");
        }

        string defaultClientImagePath = Path.Combine(env.WebRootPath, "Images", "defaultProfile.jpg");
        return PhysicalFile(defaultClientImagePath, "Image/*");
    }

    [HttpPost]
    [Authorize]
    [RequestSizeLimit(512_000)]
    public async Task<IActionResult> SubmitClientImage(IFormFile? clientImageFile = null)
    {
        MyIdentityUser? user = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        DirectoryInfo userImageDirectoryInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Storage", "Account", "UserImage"));
        string userImagePath = Path.Combine(userImageDirectoryInfo.FullName, user.UserGuid);
        if (clientImageFile is null)
        {
            if (System.IO.File.Exists(userImagePath))
            {
                System.IO.File.Delete(userImagePath);
                user.Version++;
                await userManager.UpdateAsync(user);
            }
        }
        else
        {
            using (FileStream fs = System.IO.File.Create(userImagePath))
            {
                await clientImageFile.CopyToAsync(fs);
            }
            user.Version++;
            await userManager.UpdateAsync(user);
        }

        return RedirectToAction(nameof(Profile));
    }

    /*[Authorize]
    public async Task<IActionResult> DeleteClientImage()
    {
        MyIdentityUser user = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        string userImagePath = $"{env.ContentRootPath}{ds}Storage{ds}Account{ds}UserImage{ds}{user.UserGuid}";
        if (System.IO.File.Exists(userImagePath))
        {
            System.IO.File.Delete(userImagePath);
        }

        user.Version++;
        await userManager.UpdateAsync(user);

        return RedirectToAction(nameof(Profile));
    }*/

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SubmitDescription([StringLength(500)] string description)
    {
        MyIdentityUser? user = await userManager.FindByNameAsync(User.Identity!.Name!);
        if (user is null)
        {
            return NotFound();
        }
        if (ModelState.IsValid)
        {
            user.Description = description;
            await userManager.UpdateAsync(user);
            // user seed
            await accountProcess.UpdateUserSeed(user);
            return RedirectToAction(nameof(Profile));
        }
        return View(nameof(Profile), user);
    }

    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SubmitNewPassword(ChangePasswordForm formModel)
    {
        if (ModelState.IsValid)
        {
            MyIdentityUser? user = await userManager.FindByNameAsync(User.Identity!.Name!);
            if (user is null)
            {
                return NotFound();
            }
            if (user.PasswordLiteral != formModel.CurrentPassword)
            {
                ModelState.AddModelError("پسورد فعلی", "پسورد فعلی اشتباه وارد شده است!");
                return View("ChangePassword");
            }
            if (formModel.NewPassword != formModel.RepeatNewPassword)
            {
                ModelState.AddModelError("پسورد جدید", "پسورد جدید و تکرار پسورد جدید یکسان نمیباشند!");
                return View("ChangePassword");
            }
            var result = await userManager.ChangePasswordAsync(user, formModel.CurrentPassword, formModel.NewPassword);
            if (result.Succeeded)
            {
                await userManager.UpdateAsync(user);
                // user seed
                await accountProcess.UpdateUserSeed(user);

                ModelState.AddModelError("پسورد", "پسورد تغییر کرد");
                return View(nameof(Profile), user);
            }
        }
        return View("ChangePassword");
    }

    [Authorize]
    public async Task<IActionResult> SwitchSubUser(string userGuid, string returnUrl = "/")
    {
        MyIdentityUser me = (await userManager.FindByNameAsync(User.Identity!.Name!))!;
        List<MyIdentityUser> allRelatedUsers = await accountProcess.GetAllRelatedUsers(me);
        if (allRelatedUsers.Count == 0)//user doesn't have multiple branch post
        {
            object o1 = $"اکانت زیر مجموعه برای شما وجود ندارد!";
            ViewBag.ResultState = "danger";
            return View("Result", o1);
        }

        if (!allRelatedUsers.Any(u => u.UserGuid == userGuid)/*!me.SubUsersGuids.Contains(userGuid)*/)
        {
            object o1 = $"اطلاعات اکانت زیر مجموعه صحیح نمیباشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o1);
        }

        var selectedSubUser = allRelatedUsers.FirstOrDefault(u => u.UserGuid == userGuid);
        if (selectedSubUser is null)
        {
            object o1 = $"اکانت زیر مجموعه پیدا نشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o1);
        }

        if (selectedSubUser.InActive)
        {
            object o1 = $"اکانت زیر مجموعه غیر فعال میباشد!";
            ViewBag.ResultState = "danger";
            return View("Result", o1);
        }

        await signInManager.SignOutAsync();

        var result = await signInManager
        .PasswordSignInAsync(selectedSubUser, selectedSubUser.PasswordLiteral, false, false);

        return Redirect(returnUrl ?? "/");
    }


    /************************************ Users ************************************/
    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> UsersList()
    {
        var orderedUsers = accountProcess.OrderUsersByPost(await userManager.Users.ToListAsync());
        return View(orderedUsers);
    }

    [Authorize(Roles = "Account_Admins")]
    public IActionResult AddUser()
    {
        //return View(new Administration_AddNewUserModel());
        return RedirectToAction("Signup", "Account");
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> EditUser(string userGuid)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }
        Administration_UsersListModel userModel = new(user, [.. await userManager.GetRolesAsync(user)]);
        userModel.SubUsers = await userManager.Users.Where(u => user.SubUsersGuids.Contains(u.UserGuid)).ToListAsync();
        userModel.MainUser = user.MainUserGuid != null ? await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == user.MainUserGuid) : null;
        return View(userModel);
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> SubmitUsername(string userGuid, string username)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }
        user.UserName = username;
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            //string resultMessage = "";
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
                //resultMessage += error.Description + "<br>";
                //return RedirectToAction("Index", "Result", new { resultMessage = resultMessage });
            }
            Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
            return View(nameof(EditUser), userModel);
        }

        // user seed
        await accountProcess.UpdateUserSeed(user);

        return RedirectToAction(nameof(EditUser), new { userGuid });
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> SubmitPassword(string userGuid, string password)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }
        IdentityResult result = await userManager.ChangePasswordAsync(user, user.PasswordLiteral, password);

        if (result.Succeeded)
        {
            user.PasswordLiteral = password;
            await userManager.UpdateAsync(user);
            // user seed
            await accountProcess.UpdateUserSeed(user);
        }
        else
        {
            //string resultMessage = "";
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
                //resultMessage += error.Description + "<br>";
                //return RedirectToAction("Index", "Result", new { resultMessage = resultMessage });
            }
            Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
            return View(nameof(EditUser), userModel);
        }
        return RedirectToAction(nameof(EditUser), new { userGuid });
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> SubmitFullName(string userGuid, string fullname)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }
        user.FullName = fullname;
        //user.EmailConfirmed = true;
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
            return View(nameof(EditUser), userModel);
        }

        // user seed
        await accountProcess.UpdateUserSeed(user);

        return RedirectToAction(nameof(EditUser), new { userGuid });
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> SubmitCity(string userGuid, string branch)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }
        branch = branch.Trim();
        if (!(configuration.GetSection("AllBranches").Get<List<string>>()?.Contains(branch) ?? false))
        {
            ModelState.AddModelError("", $"{configuration["BranchTitle"]} با نام {branch} پیدا نشد!");
            Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
            return View(nameof(EditUser), userModel);
        }
        user.Branch = branch;
        //user.EmailConfirmed = true;
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
            return View(nameof(EditUser), userModel);
        }

        // user seed
        await accountProcess.UpdateUserSeed(user);

        return RedirectToAction(nameof(EditUser), new { userGuid });
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> SubmitPost(string userGuid, string post)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }
        user.Post = post;
        //user.EmailConfirmed = true;
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
            return View(nameof(EditUser), userModel);
        }

        // user seed
        await accountProcess.UpdateUserSeed(user);

        return RedirectToAction(nameof(EditUser), new { userGuid });
    }
    /*
        [Authorize(Roles = "Account_Admins")]
        public async Task<IActionResult> AddBranchPost(string userGuid, string post, string branch)
        {
            MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
            if (user is null)
            {
                return NotFound();
            }

            branch = branch.Trim();
            if (!(configuration.GetSection("AllBranches").Get<List<string>>()?.Contains(branch) ?? false))
            {
                ModelState.AddModelError("", $"{configuration["BranchTitle"]} با نام {branch} پیدا نشد!");
                Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
                return View(nameof(EditUser), userModel);
            }

            string branchPost = $"{branch}_{post}";
            user.MultipleBranchPost.Add(branchPost);
            IdentityResult result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
                return View(nameof(EditUser), userModel);
            }

            // user seed
            await accountProcess.UpdateUserSeed(user);

            return RedirectToAction(nameof(EditUser), new { userGuid });
        }

        [Authorize(Roles = "Account_Admins")]
        public async Task<IActionResult> RemoveBranchPost(string userGuid, string branchPost)
        {
            MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
            if (user is null)
            {
                return NotFound();
            }

            if (user.MultipleBranchPost.Remove(branchPost))
            {
                IdentityResult result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (IdentityError error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
                    return View(nameof(EditUser), userModel);
                }

                string currentBranchPost = $"{user.Branch}_{user.Post}";
                if (branchPost == currentBranchPost)
                {
                    await userManager.UpdateSecurityStampAsync(user);
                }

                // user seed
                await accountProcess.UpdateUserSeed(user);

            }

            return RedirectToAction(nameof(EditUser), new { userGuid });
        }
    */
    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> AddSubUser(string userGuid, string username)
    {
        MyIdentityUser? mainUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (mainUser is null)
        {
            return NotFound();
        }

        var subUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);
        if (subUser is null)
        {
            ModelState.AddModelError("", "نام کاربری مشخص شده اشتباه میباشد!");
            Administration_UsersListModel userModel = new(mainUser, [.. (await userManager.GetRolesAsync(mainUser))]);
            return View(nameof(EditUser), userModel);
        }

        if (mainUser.SubUsersGuids.Contains(subUser.UserGuid))
        {
            if (subUser.MainUserGuid != mainUser.UserGuid)
            {
                subUser.MainUserGuid = mainUser.UserGuid;
                var result = await userManager.UpdateAsync(subUser);
                if (!result.Succeeded)
                {
                    foreach (IdentityError error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    Administration_UsersListModel userModel1 = new(mainUser, [.. (await userManager.GetRolesAsync(mainUser))]);
                    return View(nameof(EditUser), userModel1);
                }
                // subUser seed
                await accountProcess.UpdateUserSeed(subUser);
            }
            ModelState.AddModelError("", "نام کاربری مشخص شده قبلا اضافه شده است!");
            Administration_UsersListModel userModel = new(mainUser, [.. (await userManager.GetRolesAsync(mainUser))]);
            return View(nameof(EditUser), userModel);
        }
        else
        {
            //mainUser.SubUsersGuids.Add(subUser.UserGuid);
            mainUser.SubUsersGuids = [.. mainUser.SubUsersGuids, subUser.UserGuid];
            IdentityResult result = await userManager.UpdateAsync(mainUser);
            if (!result.Succeeded)
            {
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                Administration_UsersListModel userModel = new(mainUser, [.. (await userManager.GetRolesAsync(mainUser))]);
                return View(nameof(EditUser), userModel);
            }

            subUser.MainUserGuid = mainUser.UserGuid;
            result = await userManager.UpdateAsync(subUser);
            if (!result.Succeeded)
            {
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                Administration_UsersListModel userModel = new(mainUser, [.. (await userManager.GetRolesAsync(mainUser))]);
                return View(nameof(EditUser), userModel);
            }

            // mainUser seed
            await accountProcess.UpdateUserSeed(mainUser);
            // subUser seed
            await accountProcess.UpdateUserSeed(subUser);

            return RedirectToAction(nameof(EditUser), new { userGuid });
        }
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> RemoveSubUser(string userGuid, string username)
    {
        MyIdentityUser? mainUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (mainUser is null)
        {
            return NotFound();
        }

        var subUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);
        if (subUser is null)
        {
            ModelState.AddModelError("", "نام کاربری مشخص شده اشتباه میباشد!");
            Administration_UsersListModel userModel = new(mainUser, [.. (await userManager.GetRolesAsync(mainUser))]);
            return View(nameof(EditUser), userModel);
        }

        if (!mainUser.SubUsersGuids.Contains(subUser.UserGuid))
        {
            if (subUser.MainUserGuid == mainUser.UserGuid)
            {
                subUser.MainUserGuid = null;
                var result = await userManager.UpdateAsync(subUser);
                if (!result.Succeeded)
                {
                    foreach (IdentityError error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    Administration_UsersListModel userModel1 = new(mainUser, [.. (await userManager.GetRolesAsync(mainUser))]);
                    return View(nameof(EditUser), userModel1);
                }
                // subUser seed
                await accountProcess.UpdateUserSeed(subUser);
            }
            ModelState.AddModelError("", "نام کاربری مشخص شده قبلا حذف شده است!");
            Administration_UsersListModel userModel = new(mainUser, [.. (await userManager.GetRolesAsync(mainUser))]);
            return View(nameof(EditUser), userModel);
        }
        else
        {
            //mainUser.SubUsersGuids.Remove(subUser.UserGuid);
            List<string> mySubUsersGuids = mainUser.SubUsersGuids;
            mySubUsersGuids.Remove(subUser.UserGuid);
            mainUser.SubUsersGuids = [.. mySubUsersGuids];
            IdentityResult result = await userManager.UpdateAsync(mainUser);
            if (!result.Succeeded)
            {
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                Administration_UsersListModel userModel = new(mainUser, [.. (await userManager.GetRolesAsync(mainUser))]);
                return View(nameof(EditUser), userModel);
            }

            subUser.MainUserGuid = null;
            result = await userManager.UpdateAsync(subUser);
            if (!result.Succeeded)
            {
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                Administration_UsersListModel userModel = new(mainUser, [.. (await userManager.GetRolesAsync(mainUser))]);
                return View(nameof(EditUser), userModel);
            }

            // mainUser seed
            await accountProcess.UpdateUserSeed(mainUser);
            // subUser seed
            await accountProcess.UpdateUserSeed(subUser);

            return RedirectToAction(nameof(EditUser), new { userGuid });
        }
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> AddPerformanceField(string userGuid, string[] performanceFields)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }

        List<string> validPerformanceFields = [];
        foreach (string performanceField in performanceFields)
        {
            if (configuration.GetSection("PerformanceFields").Get<List<string>>()?.Contains(performanceField) ?? false &&
                    !user.PerformanceField.Contains(performanceField))
            {
                validPerformanceFields.Add(performanceField);
            }
        }

        if (validPerformanceFields.Any())
        {
            //user.PerformanceField.AddRange(validPerformanceFields);
            user.PerformanceField = [.. validPerformanceFields];
            IdentityResult result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                Administration_UsersListModel userModel = new(user, [.. await userManager.GetRolesAsync(user)]);
                return View(nameof(EditUser), userModel);
            }

            // user seed
            await accountProcess.UpdateUserSeed(user);
        }

        return RedirectToAction(nameof(EditUser), new { userGuid });
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> RemovePerformanceField(string userGuid, string performanceField)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }
        //user.PerformanceField.Remove(performanceField);
        List<string> myUserPerformanceField = user.PerformanceField;
        myUserPerformanceField.Remove(performanceField);
        user.PerformanceField = [.. myUserPerformanceField];
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            Administration_UsersListModel userModel = new(user, [.. await userManager.GetRolesAsync(user)]);
            return View(nameof(EditUser), userModel);
        }

        // user seed
        await accountProcess.UpdateUserSeed(user);

        return RedirectToAction(nameof(EditUser), new { userGuid });
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> DeleteUser(string userGuid)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }

        if (userGuid == "admin")
        {
            ModelState.AddModelError("", "نمیتوان کاربر \'admin\' را حذف کرد!");
            Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
            return View(nameof(EditUser), userModel);
        }

        IdentityResult result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
            return View(nameof(EditUser), userModel);
        }

        string clientImagePath = Path.Combine(env.ContentRootPath, "Storage", "Account", "UserImage", user.UserGuid);//env.WebRootPath + ds + "Images" + ds + "Clients" + ds + user.UserGuid;
        if (System.IO.File.Exists(clientImagePath))
        {
            System.IO.File.Delete(clientImagePath);
        }

        // user seed
        accountProcess.DeleteUserSeed(user);

        return RedirectToAction(nameof(UsersList));
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> SubmitUserActive(string userGuid, bool active)
    {
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }
        /*Console.WriteLine("***************************************");
        Console.WriteLine($"active = {active}");
        Console.WriteLine("***************************************");*/
        user.InActive = !active;
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            Administration_UsersListModel userModel = new(user, [.. (await userManager.GetRolesAsync(user))]);
            return View(nameof(EditUser), userModel);
        }

        // user seed
        await accountProcess.UpdateUserSeed(user);

        return RedirectToAction(nameof(EditUser), new { userGuid });
    }



    /************************************ Roles ************************************/
    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> RolesList()
    {
        List<Administration_RolesListModel> roleListModel_List = [];
        foreach (var role in await roleManager.Roles.ToListAsync())
        {
            if (role.Name is null) continue;
            Administration_RolesListModel roleListModel =
            new(role.Name, role.Description, (await userManager.GetUsersInRoleAsync(role.Name)).Count);
            roleListModel_List.Add(roleListModel);
        }
        return View(roleListModel_List);
    }

    /*[Authorize(Roles = "Account_Admins")]
    public IActionResult AddRole()
    {
        return View(new Administration_AddNewRoleModel());
    }

    [HttpPost]
    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> SubmitNewRole(Administration_AddNewRoleModel newRoleModel)
    {
        if (ModelState.IsValid)
        {
            MyIdentityRole? role = await roleManager.FindByNameAsync(newRoleModel.RoleName);
            if (role is null)
            {
                role = new(newRoleModel.RoleName) { Description = newRoleModel.Description };
                IdentityResult result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(RolesList));
                }
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            else
            {
                ModelState.AddModelError("", $"دسترسی با نام \'{newRoleModel.RoleName}\' وجود دارد!");
            }
        }
        return View(nameof(AddRole));
    }*/

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> EditRole(string roleName)
    {
        MyIdentityRole? role = await roleManager.FindByNameAsync(roleName);
        if (role is not null)
        {
            List<MyIdentityUser> members = (await userManager.GetUsersInRoleAsync(role.Name!)).ToList() ?? [];
            List<MyIdentityUser> notMembers = (await userManager.Users.ToListAsync()).ExceptBy(members.Select(m => m.UserGuid), u => u.UserGuid).ToList();
            List<string> notices = [];
            if (roleName == "Branch_Chiefs")
            {
                //notices.Add("هر اداره نیاز دارد یک عضو در این دسترسی داشته باشد!");
                //notices.Add("هر اداره تنها مجاز به داشتن یک عضو در این دسترسی میباشد!");

                foreach (string branch in configuration.GetSection("AllBranches").Get<List<string>>() ?? [])
                {
                    var membersWithSameBranch = members.Where(u => u.Branch == branch);
                    if (!membersWithSameBranch.Any())
                    {
                        notices.Add($"اداره {branch} هیچ عضوی در این دسترسی ندارد! برای این اداره یک رئیس انتخاب کنید.");
                    }
                    else if (membersWithSameBranch.Count() > 1)
                    {
                        notices.Add($"اداره {branch} بیش از یک عضو رئیس دارد!");
                    }
                }
            }
            Administration_EditRoleModel editRoleModel = new()
            {
                RoleName = role.Name!,
                Members = accountProcess.OrderUsersByPost(members),
                NotMembers = accountProcess.OrderUsersByPost(notMembers),
                Notices = notices
            };

            return View(editRoleModel);
        }
        return NotFound();
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> RemoveMemberRole(string userGuid, string roleName)
    {
        if (userGuid == "admin")
        {
            ViewBag.ResultState = "danger";
            //return View("Result", "Can NOT remove admin from Admins!");
            object o = $"کاربر admin را نمیتوان از هیچ دسترسی حذف کرد!";
            return View("Result", o);
        }
        MyIdentityRole? role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            return NotFound();
        }
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }

        IdentityResult result = await userManager.RemoveFromRoleAsync(user, roleName);
        if (result.Succeeded)
        {
            // user seed
            await accountProcess.UpdateUserSeed(user);

            if (roleName == "Branch_Chiefs")
            {
                var chiefs = (await userManager.GetUsersInRoleAsync("Branch_Chiefs"))
                .Where(u => u.Branch == user.Branch && u.UserGuid != "admin")!;

                if (chiefs.Count() == 0)
                {
                    object o = $"رئیس اداره {user.Branch} مشخص نمیباشد! لطفا سریعا رئیس اداره {user.Branch} را تایین کنید.";
                    ViewBag.ResultState = "danger";
                    return View("Result", o);
                }
                /*else if (chiefs.Count() > 1)
                {
                    object o = "عملیات اضافه کردن دسترسی نامفق! رئیس اداره بیش از یک نفر نمیتواند درنظر گرفته شود! ابتدا باید رئیس قبلی را از دسترسی مربوطه حذف کنید.";
                    ViewBag.ResultState = "danger";
                    return View("Result", o);
                }*/
            }
            if (user.UserName == User.Identity!.Name)
            {
                return RedirectToAction("Logout", "Account");
            }
            return RedirectToAction(nameof(EditRole), new { roleName = roleName });
        }
        string errorMessage = string.Empty;
        foreach (var error in result.Errors)
        {
            errorMessage += error.Description;
        }
        ViewBag.ResultState = "danger";
        return RedirectToAction("Index", "Result", new { resultMessage = errorMessage });
    }

    [Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> AddMemberRole(string userGuid, string roleName)
    {
        MyIdentityRole? role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            return NotFound();
        }
        MyIdentityUser? user = await userManager.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        if (user is null)
        {
            return NotFound();
        }

        if (roleName == "Branch_Chiefs")
        {
            var chiefs = (await userManager.GetUsersInRoleAsync("Branch_Chiefs"))
            .Where(u => u.Branch == user.Branch && u.UserGuid != "admin")!;

            /*if (chiefs.Count() == 0)
            {
                object o = $"رئیس اداره {user.Branch} مشخص نمیباشد! لطفا سریعا رئیس اداره {user.Branch} را تایین کنید.";
                ViewBag.ResultState = "danger";
                return View("Result", o);
            }
            else */
            if (chiefs.Count() > 1)
            {
                object o = "عملیات اضافه کردن دسترسی نامفق! رئیس اداره بیش از یک نفر نمیتواند درنظر گرفته شود! ابتدا باید رئیس قبلی را از دسترسی مربوطه حذف کنید.";
                ViewBag.ResultState = "danger";
                return View("Result", o);
            }
        }

        IdentityResult result = await userManager.AddToRoleAsync(user, roleName);
        if (result.Succeeded)
        {
            // user seed
            await accountProcess.UpdateUserSeed(user);

            return RedirectToAction(nameof(EditRole), new { roleName = roleName });
        }
        string errorMessage = string.Empty;
        foreach (var error in result.Errors)
        {
            errorMessage += error.Description;
        }
        ViewBag.ResultState = "danger";
        return RedirectToAction("Index", "Result", new { resultMessage = errorMessage });
    }

    /*[Authorize(Roles = "Account_Admins")]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        MyIdentityRole? role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            return NotFound();
        }

        IdentityResult result = await roleManager.DeleteAsync(role);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(RolesList));
        }
        foreach (IdentityError error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }
        return View("Result", roleName);
    }*/

}