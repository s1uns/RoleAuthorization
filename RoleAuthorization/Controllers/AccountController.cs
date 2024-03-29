﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RoleAuthorization.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RoleAuthorization.Models;

namespace RolesApp.Controllers
{
    public class AccountController : Controller
    {
        private ApplicationContext _context;
        public AccountController(ApplicationContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    // creating new User
                    user = new User { Email = model.Email, Password = model.Password };
                    Role userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "user");
                    if (userRole != null)
                        user.Role = userRole;

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    await Authenticate(user); // Authentication

                    return RedirectToAction("Index", "Home");
                }
                else
                    ModelState.AddModelError("", "Incorrect login and/or password");
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);
                if (user != null)
                {
                    await Authenticate(user); // Authentication

                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Incorrect login and/or password");
            }
            return View(model);
        }
        private async Task Authenticate(User user)
        {
            // creating a claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role?.Name)
            };
            // creating a ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            // installing the cookies
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }
    }
}