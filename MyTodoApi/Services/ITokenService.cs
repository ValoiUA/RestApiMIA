using MyTodoApi.Models;

namespace MyTodoApi.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}