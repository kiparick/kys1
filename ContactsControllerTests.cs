using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;

namespace Server.Tests
{
    [TestFixture]
    public class ContactsControllerTests
    {
        private HttpClient? _client;
        private string? _token;

        [SetUp]
        public async Task Setup()
        {
            var factory = new ServerApplication();
            _client = factory.CreateClient();
            _token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        }

        [Test]
        public async Task AddContact_ValidContact_ReturnsSuccess()
        {
            var contact = new 
            { 
                Name = "John Doe", 
                PhoneNumber = "1234567890", 
                Email = "john.doe@example.com", 
                Address = "123 Main St" 
            };
            var content = new StringContent(JsonSerializer.Serialize(contact), Encoding.UTF8, "application/json");

            var response = await _client!.PostAsync("/api/contacts", content);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseString);
            responseJson.GetProperty("message").GetString().Should().Be("Контакт успешно добавлен.");
            responseJson.GetProperty("id").GetInt64().Should().BeGreaterThan(0);
        }

        [Test]
        public async Task GetContact_ValidId_ReturnsContact()
        {
            var contact = new 
            { 
                Name = "Jane Doe", 
                PhoneNumber = "0987654321", 
                Email = "jane.doe@example.com", 
                Address = "456 Elm St" 
            };
            var addContent = new StringContent(JsonSerializer.Serialize(contact), Encoding.UTF8, "application/json");
            var addResponse = await _client!.PostAsync("/api/contacts", addContent);
            addResponse.EnsureSuccessStatusCode();

            var addResult = await addResponse.Content.ReadAsStringAsync();
            var contactId = JsonSerializer.Deserialize<JsonElement>(addResult).GetProperty("id").GetInt32();

            var getResponse = await _client.GetAsync($"/api/contacts/{contactId}");
            var getResult = await getResponse.Content.ReadAsStringAsync();

            getResponse.EnsureSuccessStatusCode();
            var contactResponse = JsonSerializer.Deserialize<ContactResponse>(getResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            contactResponse.Name.Should().Be("Jane Doe");
            contactResponse.PhoneNumber.Should().Be("0987654321");
        }

        [Test]
        public async Task UpdateContact_ValidId_ReturnsSuccess()
        {
            var contact = new 
            { 
                Name = "John Smith", 
                PhoneNumber = "1112223333", 
                Email = "john.smith@example.com", 
                Address = "789 Oak St" 
            };
            var addContent = new StringContent(JsonSerializer.Serialize(contact), Encoding.UTF8, "application/json");
            var addResponse = await _client!.PostAsync("/api/contacts", addContent);
            addResponse.EnsureSuccessStatusCode();

            var addResult = await addResponse.Content.ReadAsStringAsync();
            var contactId = JsonSerializer.Deserialize<JsonElement>(addResult).GetProperty("id").GetInt32();

            var updatedContact = new 
            { 
                Name = "John Smith Updated", 
                PhoneNumber = "9998887777", 
                Email = "john.updated@example.com", 
                Address = "789 Oak St, Apt 101" 
            };
            var updateContent = new StringContent(JsonSerializer.Serialize(updatedContact), Encoding.UTF8, "application/json");

            var updateResponse = await _client.PatchAsync($"/api/contacts/{contactId}", updateContent);
            var updateResult = await updateResponse.Content.ReadAsStringAsync();

            updateResponse.EnsureSuccessStatusCode();
            var updateJson = JsonSerializer.Deserialize<JsonElement>(updateResult);
            updateJson.GetProperty("message").GetString().Should().Be("Контакт успешно обновлен.");
        }

        [Test]
        public async Task DeleteContact_ValidId_ReturnsSuccess()
        {
            var contact = new 
            { 
                Name = "Alice Johnson", 
                PhoneNumber = "5556667777", 
                Email = "alice.johnson@example.com", 
                Address = "321 Pine St" 
            };
            var addContent = new StringContent(JsonSerializer.Serialize(contact), Encoding.UTF8, "application/json");
            var addResponse = await _client!.PostAsync("/api/contacts", addContent);
            addResponse.EnsureSuccessStatusCode();

            var addResult = await addResponse.Content.ReadAsStringAsync();
            var contactId = JsonSerializer.Deserialize<JsonElement>(addResult).GetProperty("id").GetInt32();

            var deleteResponse = await _client.DeleteAsync($"/api/contacts/{contactId}");
            deleteResponse.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task GetAllContacts_ReturnsListOfContacts()
        {
            var contact1 = new 
            { 
                Name = "Bob Brown", 
                PhoneNumber = "1112223333", 
                Email = "bob.brown@example.com", 
                Address = "123 Maple St" 
            };
            var contact2 = new 
            { 
                Name = "Charlie Davis", 
                PhoneNumber = "4445556666", 
                Email = "charlie.davis@example.com", 
                Address = "456 Birch St" 
            };

            var addContent1 = new StringContent(JsonSerializer.Serialize(contact1), Encoding.UTF8, "application/json");
            var addContent2 = new StringContent(JsonSerializer.Serialize(contact2), Encoding.UTF8, "application/json");

            await _client!.PostAsync("/api/contacts", addContent1);
            await _client.PostAsync("/api/contacts", addContent2);

            var getAllResponse = await _client.GetAsync("/api/contacts");
            var getAllResult = await getAllResponse.Content.ReadAsStringAsync();

            getAllResponse.EnsureSuccessStatusCode();
            var contacts = JsonSerializer.Deserialize<List<ContactResponse>>(getAllResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            contacts.Should().Contain(c => c.Name == "Bob Brown");
            contacts.Should().Contain(c => c.Name == "Charlie Davis");
        }

        [Test]
        public async Task SearchContacts_ValidSearchTerm_ReturnsMatchingContacts()
        {
            var contact1 = new 
            { 
                Name = "David Green", 
                PhoneNumber = "7778889999", 
                Email = "david.green@example.com", 
                Address = "789 Cedar St" 
            };
            var contact2 = new 
            { 
                Name = "Eve White", 
                PhoneNumber = "2223334444", 
                Email = "eve.white@example.com", 
                Address = "321 Spruce St" 
            };

            var addContent1 = new StringContent(JsonSerializer.Serialize(contact1), Encoding.UTF8, "application/json");
            var addContent2 = new StringContent(JsonSerializer.Serialize(contact2), Encoding.UTF8, "application/json");

            await _client!.PostAsync("/api/contacts", addContent1);
            await _client.PostAsync("/api/contacts", addContent2);

            var searchRequest = new { SearchTerm = "Green" };
            var searchContent = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");

            var searchResponse = await _client.PostAsync("/api/contacts/search", searchContent);
            var searchResult = await searchResponse.Content.ReadAsStringAsync();

            searchResponse.EnsureSuccessStatusCode();
            var contacts = JsonSerializer.Deserialize<List<ContactResponse>>(searchResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            contacts.Should().Contain(c => c.Name == "David Green");
        }

        private async Task<string> GetTokenAsync()
        {
            var username = $"testuser_{Guid.NewGuid()}";
            var password = "testpassword";

            var registerContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var registerResponse = await _client!.PostAsync("/api/users/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();

            var loginContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/users/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<JsonElement>(loginResult).GetProperty("token").GetString();

            return token!;
        }
    }

    public class ContactResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }
}