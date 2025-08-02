using marketplace_practice.Controllers.dto;

namespace marketplace_practice.Services.interfaces
{
    public interface IUserService
    {
        string GetUserById();
        CreateUserResultDto CreateUser(CreateUserDto dto);
        string UpdateUser();
        string DeleteUser();
    }
}
