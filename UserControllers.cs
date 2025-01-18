using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly DatabaseContext _dbContext;
    private readonly IConfiguration _configuration;

    public UsersController(DatabaseContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] UserRegistrationRequest request)
    {
        if (request.Username == null || request.Password == null)
        {
            return BadRequest("Имя пользователя и пароль обязательны.");
        }

        using (var connection = _dbContext.GetConnection())
        {
            connection.Open();

            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
            checkCommand.Parameters.AddWithValue("@Username", request.Username);
            var result = checkCommand.ExecuteScalar();
            var userExists = result != null && (long)result > 0;

            if (userExists)
                return Conflict("Пользователь уже существует.");

            var passwordHash = HashPassword(request.Password);

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO Users (Username, PasswordHash)
                VALUES (@Username, @PasswordHash);
                SELECT last_insert_rowid();";
            insertCommand.Parameters.AddWithValue("@Username", request.Username);
            insertCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);

            var userIdObject = insertCommand.ExecuteScalar();

            if (userIdObject == null)
            {
                return StatusCode(500, "Не удалось получить ID пользователя после регистрации.");
            }

            var userId = (long)userIdObject;

            var token = GenerateToken(request.Username, userId.ToString());

            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE Users SET Token = @Token WHERE Id = @UserId";
            updateCommand.Parameters.AddWithValue("@Token", token);
            updateCommand.Parameters.AddWithValue("@UserId", userId);
            updateCommand.ExecuteNonQuery();

            return Ok(new { Token = token });
        }
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] UserLoginRequest request)
    {
        if (request.Username == null || request.Password == null)
        {
            return BadRequest("Имя пользователя и пароль обязательны.");
        }

        using (var connection = _dbContext.GetConnection())
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, PasswordHash FROM Users WHERE Username = @Username";
            command.Parameters.AddWithValue("@Username", request.Username);

            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return Unauthorized("Неверное имя пользователя или пароль.");
                }

                var userId = reader.GetInt64(0);
                var passwordHash = reader.GetString(1);

                var inputPasswordHash = HashPassword(request.Password);
                if (inputPasswordHash != passwordHash)
                {
                    return Unauthorized("Неверное имя пользователя или пароль.");
                }

                var token = GenerateToken(request.Username, userId.ToString());

                var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = "UPDATE Users SET Token = @Token WHERE Id = @UserId";
                updateCommand.Parameters.AddWithValue("@Token", token);
                updateCommand.Parameters.AddWithValue("@UserId", userId);
                updateCommand.ExecuteNonQuery();

                return Ok(new { Token = token });
            }
        }
    }

    [Authorize]
    [HttpGet("history")]
    public IActionResult GetHistory()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("ID пользователя не найден в токене.");
        }

        using (var connection = _dbContext.GetConnection())
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT RequestUrl, RequestTime
                FROM RequestHistory
                WHERE UserId = @UserId";
            command.Parameters.AddWithValue("@UserId", userId);

            using (var reader = command.ExecuteReader())
            {
                var history = new List<RequestHistoryItem>();
                while (reader.Read())
                {
                    history.Add(new RequestHistoryItem
                    {
                        RequestUrl = reader.GetString(0),
                        RequestTime = reader.GetDateTime(1)
                    });
                }

                return Ok(history);
            }
        }
    }

    [Authorize]
    [HttpDelete("history")]
    public IActionResult DeleteHistory()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("ID пользователя не найден в токене.");
        }

        using (var connection = _dbContext.GetConnection())
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM RequestHistory
                WHERE UserId = @UserId";
            command.Parameters.AddWithValue("@UserId", userId);

            command.ExecuteNonQuery();

            return NoContent();
        }
    }

    [Authorize]
    [HttpPatch("password")]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (request.NewPassword == null)
        {
            return BadRequest("Новый пароль обязателен.");
        }

        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("ID пользователя не найден в токене.");
        }

        using (var connection = _dbContext.GetConnection())
        {
            connection.Open();

            var newPasswordHash = HashPassword(request.NewPassword);
            var newToken = GenerateToken(username, userId);

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Users
                SET PasswordHash = @PasswordHash, Token = @Token
                WHERE Username = @Username";
            command.Parameters.AddWithValue("@PasswordHash", newPasswordHash);
            command.Parameters.AddWithValue("@Token", newToken);
            command.Parameters.AddWithValue("@Username", username);
            command.ExecuteNonQuery();

            SaveRequestHistory("/api/users/password", userId);

            return Ok(new { Token = newToken });
        }
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private string GenerateToken(string username, string userId)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userId))
        {
            throw new ArgumentNullException("Имя пользователя и ID обязательны.");
        }

        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("Ключ JWT не настроен в appsettings.json.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SaveRequestHistory(string requestUrl, string userId)
    {
        using (var connection = _dbContext.GetConnection())
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT INTO RequestHistory (UserId, RequestUrl)
                        VALUES (@UserId, @RequestUrl)";
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@RequestUrl", requestUrl);

                    command.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}

public class UserRegistrationRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class UserLoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class ChangePasswordRequest
{
    public required string NewPassword { get; set; }
}

public class RequestHistoryItem
{
    public required string RequestUrl { get; set; }
    public DateTime RequestTime { get; set; }
}