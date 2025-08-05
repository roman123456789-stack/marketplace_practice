using System.Text;
using System.Security.Cryptography;

namespace marketplace_practice.Utils
{
    public class AuthUtils
    {
        //public string HashPassword(string password)
        //{
        //    using (var sha256 = SHA256.Create())
        //    {
        //        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        //        return Convert.ToBase64String(bytes);
        //    }
        //}

        // Метод UserManager<User>.CreateAsync(User user, string password) по умолчанию хэширует пароль,
        // поэтому HashPassword выше я убрал.
        // Но сам класс пока оставил на случай, если добавится какая-то другая логика по аутентификации.
    }
}
