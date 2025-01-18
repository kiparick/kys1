using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static string baseUrl = "http://localhost:5034/api";
    private static string token = string.Empty;

    static async Task Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Выберите действие:");
            Console.WriteLine("1. Регистрация");
            Console.WriteLine("2. Вход");
            Console.WriteLine("3. Просмотр истории запросов");
            Console.WriteLine("4. Удаление истории запросов");
            Console.WriteLine("5. Изменение пароля");
            Console.WriteLine("6. Добавить контакт");
            Console.WriteLine("7. Удалить контакт");
            Console.WriteLine("8. Редактировать контакт");
            Console.WriteLine("9. Просмотреть все контакты");
            Console.WriteLine("10. Просмотреть один контакт");
            Console.WriteLine("11. Поиск контактов");
            Console.WriteLine("12. Выход");

            string choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await Register();
                        break;
                    case "2":
                        await Login();
                        break;
                    case "3":
                        await GetHistory();
                        break;
                    case "4":
                        await DeleteHistory();
                        break;
                    case "5":
                        await ChangePassword();
                        break;
                    case "6":
                        await AddContact();
                        break;
                    case "7":
                        await DeleteContact();
                        break;
                    case "8":
                        await UpdateContact();
                        break;
                    case "9":
                        await GetAllContacts();
                        break;
                    case "10":
                        await GetContact();
                        break;
                    case "11":
                        await SearchContacts();
                        break;
                    case "12":
                        return;
                    default:
                        Console.WriteLine("Неверный выбор. Попробуйте снова.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }
    }

    private static async Task Register()
    {
        Console.Write("Введите имя пользователя: ");
        string username = Console.ReadLine();
        Console.Write("Введите пароль: ");
        string password = Console.ReadLine();

        var user = new { Username = username, Password = password };

        try
        {
            var response = await httpClient.PostAsJsonAsync($"{baseUrl}/users/register", user);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Регистрация успешна.");
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    Console.WriteLine("Ошибка: Пользователь с таким именем уже существует.");
                }
                else
                {
                    Console.WriteLine($"Ошибка: {response.StatusCode}. {errorResponse}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
        }
    }

    private static async Task Login()
    {
        Console.Write("Введите имя пользователя: ");
        string username = Console.ReadLine();
        Console.Write("Введите пароль: ");
        string password = Console.ReadLine();

        var user = new { Username = username, Password = password };

        try
        {
            var response = await httpClient.PostAsJsonAsync($"{baseUrl}/users/login", user);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                token = result.GetProperty("token").GetString();
                Console.WriteLine("Вход выполнен.");
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("Ошибка: Неверное имя пользователя или пароль.");
                }
                else
                {
                    Console.WriteLine($"Ошибка: {response.StatusCode}. {errorResponse}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
        }
    }

    private static async Task GetHistory()
    {
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Сначала выполните вход.");
            return;
        }

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/users/history"))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var history = JsonSerializer.Deserialize<List<RequestHistoryItem>>(responseContent, options);

                    Console.WriteLine("История запросов:");
                    if (history != null && history.Any())
                    {
                        foreach (var item in history)
                        {
                            Console.WriteLine($"URL: {item.RequestUrl}, Время: {item.RequestTime}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("История запросов пуста.");
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine("Ошибка: Необходимо авторизоваться.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка: {response.StatusCode}. {errorResponse}");
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Ошибка при десериализации ответа: {ex.Message}");
        }
    }

    private static async Task DeleteHistory()
    {
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Сначала выполните вход.");
            return;
        }

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/users/history"))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("История запросов удалена.");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine("Ошибка: Необходимо авторизоваться.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка: {response.StatusCode}. {errorResponse}");
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
        }
    }

    private static async Task ChangePassword()
    {
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Сначала выполните вход.");
            return;
        }

        Console.Write("Введите новый пароль: ");
        string newPassword = Console.ReadLine();

        var request = new { NewPassword = newPassword };

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"{baseUrl}/users/password"))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                requestMessage.Content = JsonContent.Create(request);

                var response = await httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    token = result.GetProperty("token").GetString();
                    Console.WriteLine("Пароль успешно изменен.");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine("Ошибка: Необходимо авторизоваться.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка: {response.StatusCode}. {errorResponse}");
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
        }
    }

    private static async Task AddContact()
    {
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Сначала выполните вход.");
            return;
        }

        Console.Write("Введите имя: ");
        string name = Console.ReadLine();
        Console.Write("Введите номер телефона: ");
        string phoneNumber = Console.ReadLine();
        Console.Write("Введите email (необязательно): ");
        string email = Console.ReadLine();
        Console.Write("Введите адрес (необязательно): ");
        string address = Console.ReadLine();

        var contact = new { Name = name, PhoneNumber = phoneNumber, Email = email, Address = address };

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/contacts"))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                requestMessage.Content = JsonContent.Create(contact);

                var response = await httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Контакт успешно добавлен.");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. {errorResponse}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
        }
    }

    private static async Task DeleteContact()
    {
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Сначала выполните вход.");
            return;
        }

        Console.Write("Введите ID контакта для удаления: ");
        string contactId = Console.ReadLine();

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/contacts/{contactId}"))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Контакт успешно удален.");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. {errorResponse}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
        }
    }

    private static async Task UpdateContact()
    {
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Сначала выполните вход.");
            return;
        }

        Console.Write("Введите ID контакта для редактирования: ");
        string contactId = Console.ReadLine();
        Console.Write("Введите новое имя: ");
        string name = Console.ReadLine();
        Console.Write("Введите новый номер телефона: ");
        string phoneNumber = Console.ReadLine();
        Console.Write("Введите новый email (необязательно): ");
        string email = Console.ReadLine();
        Console.Write("Введите новый адрес (необязательно): ");
        string address = Console.ReadLine();

        var contact = new { Name = name, PhoneNumber = phoneNumber, Email = email, Address = address };

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"{baseUrl}/contacts/{contactId}"))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                requestMessage.Content = JsonContent.Create(contact);

                var response = await httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Контакт успешно обновлен.");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. {errorResponse}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
        }
    }

    private static async Task GetAllContacts()
    {
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Сначала выполните вход.");
            return;
        }

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/contacts"))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var contacts = JsonSerializer.Deserialize<List<ContactResponse>>(responseContent, options);

                    Console.WriteLine("Список контактов:");
                    if (contacts != null && contacts.Any())
                    {
                        foreach (var contact in contacts)
                        {
                            Console.WriteLine($"ID: {contact.Id}, Имя: {contact.Name}, Телефон: {contact.PhoneNumber}, Email: {contact.Email}, Адрес: {contact.Address}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Контакты не найдены.");
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. {errorResponse}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Ошибка при десериализации ответа: {ex.Message}");
        }
    }

    private static async Task GetContact()
    {
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Сначала выполните вход.");
            return;
        }

        Console.Write("Введите ID контакта: ");
        string contactId = Console.ReadLine();

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/contacts/{contactId}"))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var contact = JsonSerializer.Deserialize<ContactResponse>(responseContent, options);

                    Console.WriteLine($"ID: {contact.Id}, Имя: {contact.Name}, Телефон: {contact.PhoneNumber}, Email: {contact.Email}, Адрес: {contact.Address}");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. {errorResponse}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Ошибка при десериализации ответа: {ex.Message}");
        }
    }

    private static async Task SearchContacts()
    {
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Сначала выполните вход.");
            return;
        }

        Console.Write("Введите поисковый запрос: ");
        string searchTerm = Console.ReadLine();

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/contacts/search"))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                requestMessage.Content = JsonContent.Create(new { SearchTerm = searchTerm });

                var response = await httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var contacts = JsonSerializer.Deserialize<List<ContactResponse>>(responseContent, options);

                    Console.WriteLine("Результаты поиска:");
                    if (contacts != null && contacts.Any())
                    {
                        foreach (var contact in contacts)
                        {
                            Console.WriteLine($"ID: {contact.Id}, Имя: {contact.Name}, Телефон: {contact.PhoneNumber}, Email: {contact.Email}, Адрес: {contact.Address}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Контакты не найдены.");
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. {errorResponse}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Ошибка при десериализации ответа: {ex.Message}");
        }
    }
}

public class RequestHistoryItem
{
    public required string RequestUrl { get; set; }
    public DateTime RequestTime { get; set; }
}

public class ContactResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}