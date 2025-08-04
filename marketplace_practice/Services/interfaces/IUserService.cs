using marketplace_practice.Controllers.dto;

namespace marketplace_practice.Services.interfaces
{
    public interface IUserService
    {
        string GetUserById();
        public Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto);
        string UpdateUser();
        string DeleteUser();
    }
}
