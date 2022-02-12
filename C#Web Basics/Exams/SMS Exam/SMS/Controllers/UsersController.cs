﻿using BasicWebServer.Server.Attributes;
using BasicWebServer.Server.Controllers;
using BasicWebServer.Server.HTTP;
using SMS.Data;
using SMS.Data.Models;
using SMS.Models.Users;
using SMS.Services;
using System;
using System.Linq;

namespace SMS.Controllers
{
    public class UsersController : Controller
    {
        private readonly SMSDbContext context;
        private readonly IValidator validator;
        private readonly IPasswordHasher passwordHasher;
        public UsersController(Request request, SMSDbContext context, IValidator validator, IPasswordHasher passwordHasher)
            : base(request)
        {
            this.context = context;
            this.validator = validator;
            this.passwordHasher = passwordHasher;
        }

        public Response Login()
        {
            if (User.IsAuthenticated)
            {
                return Redirect("/");
            }
            return this.View(new { IsAuthenticated = false });
        }
        [HttpPost]
        public Response Login(LoginViewForm model)
        {
            var hashedPasword = this.passwordHasher.Hash(model.Password);
            var userId = this.context.Users
                .Where(u => u.Username == model.Username && u.Password == hashedPasword)
                .Select(u => u.Id)
                .FirstOrDefault();
            if (userId == null)
            {
                return View(new { ErrorMessage = "Login incorrect!" }, "/Error");
            }

            this.SignIn(userId);

            return Redirect("/");
        }
        public Response Register()
        {
            if (User.IsAuthenticated)
            {
                return Redirect("/");
            }
            return this.View(new { IsAuthenticated = false });
        }
        [HttpPost]
        public Response Register(RegisterViewFormModel model)
        {
            var (isValid, error) = validator.ValidateRegisterModel(model);
            if (this.context.Users.Any(u => u.Username == model.Username))
            {
                isValid = false;
                error = $"User with '{model.Username}' username already exist!";
            }
            if (this.context.Users.Any(u => u.Email == model.Email))
            {
                isValid = false;
                error = $"User with '{model.Email}' e-mail already exist!";
            }
            if (!isValid)
            {
                return View(new { ErrorMessage = error }, "/Error");
            }
            Cart cart = new Cart();
            User user = new User
            {
                Email = model.Email,
                Username = model.Username,
                Password = this.passwordHasher.Hash(model.Password),
                Cart = cart,
                CardId = cart.Id
            };
            try
            {
                context.Users.Add(user);
                context.SaveChanges();
            }
            catch (Exception)
            {

                return View("Unexpected error", "/Error");
            }

            return Redirect("/Users/Login");
        }
        [Authorize]
        public Response Logout()
        {
            this.SignOut();

            return Redirect("/");
        }
    }
}
