using marketplace_practice.Models;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Identity;

namespace marketplace_practice.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ILoyaltyService _loyaltyService;

        public UserService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ILoyaltyService loyaltyService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _loyaltyService = loyaltyService;
        }

        public string GetUserById()
        {
            return "Dev version";
        }

        public string UpdateUser()
        {
            return "Dev version";
        }

        public string DeleteUser()
        {
            return "Dev version";
        }
    }
}
