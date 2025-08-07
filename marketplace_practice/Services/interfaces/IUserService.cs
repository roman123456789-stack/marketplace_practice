using marketplace_practice.Controllers.dto;

namespace marketplace_practice.Services.interfaces
{
    public interface IUserService
    {
        string GetUserById();
        string UpdateUser();
        string DeleteUser();
    }
}
