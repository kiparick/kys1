using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly DatabaseContext _dbContext;

    public ContactsController(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public IActionResult AddContact([FromBody] ContactRequest request)
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
                INSERT INTO Contacts (UserId, Name, PhoneNumber, Email, Address)
                VALUES (@UserId, @Name, @PhoneNumber, @Email, @Address);
                SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Name", request.Name);
            command.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@Address", request.Address);

            var result = command.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return StatusCode(500, "Не удалось получить ID добавленного контакта.");
            }

            var contactId = (long)result;

            SaveRequestHistory("/api/contacts", userId);

            return Ok(new { Id = contactId, Message = "Контакт успешно добавлен." });
        }
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteContact(int id)
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
                DELETE FROM Contacts
                WHERE Id = @Id AND UserId = @UserId";
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@UserId", userId);

            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                return NotFound("Контакт не найден или у вас нет прав на его удаление.");
            }

            SaveRequestHistory($"/api/contacts/{id}", userId);

            return NoContent();
        }
    }

    [HttpPatch("{id}")]
    public IActionResult UpdateContact(int id, [FromBody] ContactRequest request)
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
                UPDATE Contacts
                SET Name = @Name, PhoneNumber = @PhoneNumber, Email = @Email, Address = @Address
                WHERE Id = @Id AND UserId = @UserId";
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Name", request.Name);
            command.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@Address", request.Address);

            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                return NotFound("Контакт не найден или у вас нет прав на его редактирование.");
            }

            SaveRequestHistory($"/api/contacts/{id}", userId);

            return Ok(new { Message = "Контакт успешно обновлен." });
        }
    }

    [HttpGet]
    public IActionResult GetAllContacts()
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
                SELECT Id, Name, PhoneNumber, Email, Address
                FROM Contacts
                WHERE UserId = @UserId";
            command.Parameters.AddWithValue("@UserId", userId);

            using (var reader = command.ExecuteReader())
            {
                var contacts = new List<ContactResponse>();
                while (reader.Read())
                {
                    contacts.Add(new ContactResponse
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        PhoneNumber = reader.GetString(2),
                        Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Address = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }

                SaveRequestHistory("/api/contacts", userId);

                return Ok(contacts);
            }
        }
    }

    [HttpGet("{id}")]
    public IActionResult GetContact(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("ID пользователя не найден в токене.");
        }

        using (var connection = _dbContext.GetConnection())
        {
            connection.Open();

            SaveRequestHistory($"/api/contacts/{id}", userId);

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, PhoneNumber, Email, Address
                FROM Contacts
                WHERE Id = @Id AND UserId = @UserId";
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@UserId", userId);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    var contact = new ContactResponse
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        PhoneNumber = reader.GetString(2),
                        Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Address = reader.IsDBNull(4) ? null : reader.GetString(4)
                    };

                    return Ok(contact);
                }
                else
                {
                    return NotFound("Контакт не найден или у вас нет прав на его просмотр.");
                }
            }
        }
    }

    [HttpPost("search")]
    public IActionResult SearchContacts([FromBody] ContactSearchRequest request)
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
                SELECT Id, Name, PhoneNumber, Email, Address
                FROM Contacts
                WHERE UserId = @UserId AND (Name LIKE @SearchTerm OR PhoneNumber LIKE @SearchTerm OR Email LIKE @SearchTerm OR Address LIKE @SearchTerm)";
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@SearchTerm", $"%{request.SearchTerm}%");

            using (var reader = command.ExecuteReader())
            {
                var contacts = new List<ContactResponse>();
                while (reader.Read())
                {
                    contacts.Add(new ContactResponse
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        PhoneNumber = reader.GetString(2),
                        Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Address = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }

                SaveRequestHistory("/api/contacts/search", userId);

                return Ok(contacts);
            }
        }
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

public class ContactRequest
{
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class ContactResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class ContactSearchRequest
{
    public required string SearchTerm { get; set; }
}