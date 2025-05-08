using System.Text.RegularExpressions;
using BiuroPodrozy.Exceptions;
using BiuroPodrozy.Models;
using BiuroPodrozy.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace BiuroPodrozy.Services;

public interface IDbService
{
    public Task<IEnumerable<TripGetDTO>> GetAllTripsAsync();
    // public Task<ClientGetDTO> GetTripsByClientIdAsync(int id);
    public Task<IEnumerable<TripGetDTO>> GetTripsByClientIdAsync(int clientId);
    public Task<ClientGetDTO> CreateClientAsync(ClientCreateDTO body);
    public Task<string> RegisterClientToTripAsync(int clientId, int tripId);
    public Task UnregisterClientFromTripAsync(int clientId, int tripId);
}

public class DbService(IConfiguration configuration) : IDbService
{
    private readonly string? _connectionString = configuration.GetConnectionString("Default");//pobiera connection string z appsettings.json
    public async Task<IEnumerable<TripGetDTO>> GetAllTripsAsync()
    {
        var result = new List<TripGetDTO>();
        
        await using var connection = new SqlConnection(_connectionString);   // tworzy nowe polaczenie z baza
        var sql = "select t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name from Trip t join Country_Trip ct on t.IdTrip = ct.IdTrip join Country c on ct.IdCountry = c.IdCountry"; //zapytanie pobierajace dane o wszystkich wycieczkach wraz z krajami do tych wycieczek
        
        await using var command = new SqlCommand(sql, connection);  // tworzy obiekt reprezentujacy zapytanie sql
        await connection.OpenAsync();   // otwiera asynchroniczne polaczenie z baza danych - nie blokuje watku
        
        await using var reader = await command.ExecuteReaderAsync();    // wykonuje zapytanie i pobiera asyn dane po ktorych mozna iterowac

        while (await reader.ReadAsync())    // przechodzimy po kazdnym wierszu
        {
            result.Add(new TripGetDTO   // dla kazdego wiersza tworzy nowy obiekt TripGetDTO i dodaje go do result
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = Convert.ToInt32(5)
            });
        }
        
        return result;
    }
    
    public async Task<IEnumerable<TripGetDTO>> GetTripsByClientIdAsync(int clientId)
    {
        await using var connection = new SqlConnection(_connectionString);
        //zapytanie sprawdzajace czy taki klient istnieje
        const string sql = "select 1 from Client where IdClient = @clientId";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        await connection.OpenAsync();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (!reader.HasRows)
            {
                throw new NotFoundException($"Client with id: {clientId} does not exist");
            }
        }

        var result = new List<TripGetDTO>();
        //zapytanie zwracajace informacje o wycieczce + platnosci na ktorej byl klient o danym id
        const string sql2 = "select t.IdTrip ,t.Name,t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.RegisteredAt, ct.PaymentDate from Trip t join Client_Trip ct on t.IdTrip = ct.IdTrip join Client c on c.IdClient = ct.IdClient where c.IdClient = @clientId";
        await using var command2 = new SqlCommand(sql2, connection);
        command2.Parameters.AddWithValue("@clientId", clientId);
        await using var reader2 = await command2.ExecuteReaderAsync();
        while (await reader2.ReadAsync())
        {
            result.Add(new TripGetDTO
            {
                IdTrip = reader2.GetInt32(0),
                Name = reader2.GetString(1),
                Description = reader2.GetString(2),
                DateFrom = reader2.GetDateTime(3),
                DateTo = reader2.GetDateTime(4),
                MaxPeople = reader2.GetInt32(5)
            });
        }

        return result;
    }
    
    public async Task<ClientGetDTO> CreateClientAsync(ClientCreateDTO client)
    {
        //walidacja danych
        //email
        if (string.IsNullOrWhiteSpace(client.Email) || !Regex.IsMatch(client.Email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            throw new Exception("Nieprawidłowy adres e-mail.");
        
        //pesel
        if (string.IsNullOrWhiteSpace(client.Pesel) || !Regex.IsMatch(client.Pesel, @"^\d{11}$"))
            throw new Exception("PESEL musi zawierać dokładnie 11 cyfr.");
        
        await using var connection = new SqlConnection(_connectionString);
        //zapytanie wprowadzajace nowe dane o kliencie
        const string sql = "insert into Client (FirstName, LastName, Email, Telephone, Pesel) values (@FirstName, @LastName, @Email, @Telephone, @Pesel); Select scope_identity()";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);
        await connection.OpenAsync();
        var id = Convert.ToInt32(await command.ExecuteScalarAsync());
        
        return new ClientGetDTO
        {
            IdClient = id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Telephone = client.Telephone,
            Pesel = client.Pesel
        };
    }
    
    public async Task<string> RegisterClientToTripAsync(int clientId, int tripId)
    {
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    // sprawdzenie czy klient istnieje
    //zapytanie pobierajace klienta o danym id
    const string sql = "SELECT 1 FROM Client WHERE IdClient = @clientid";
    await using var checkClient = new SqlCommand(sql, connection);
    checkClient.Parameters.AddWithValue("@clientid", clientId);
    if (await checkClient.ExecuteScalarAsync() is null)
        throw new KeyNotFoundException($"Client with ID {clientId} does not exist.");

    //sprawdzenie czy wycieczka istnieje
    //zapytanie pobierajace maxymalna ilosc ludzi na wycieczce o danym id
    const string sql2 = "SELECT MaxPeople FROM Trip WHERE IdTrip = @tripId";
    await using var checkTrip = new SqlCommand(sql2, connection);
    checkTrip.Parameters.AddWithValue("@tripId", tripId);
    var maxPeopleObj = await checkTrip.ExecuteScalarAsync();

    if (maxPeopleObj is null)
    {
        throw new KeyNotFoundException($"Trip with ID {tripId} does not exist.");
    }
    
    //max liczba ludzi
    var maxPeople = Convert.ToInt32(maxPeopleObj);

    // obecna ilosc ludzi
    //zapytanie zwracajace zliczona ilosc miejsc
    var count = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @tripId", connection);
    count.Parameters.AddWithValue("@tripId", tripId);
    int currentCount = Convert.ToInt32(await count.ExecuteScalarAsync());
    if (currentCount >= maxPeople)
        throw new InvalidOperationException("Nie ma juz miejsc.");
    
    //zapytanie sprawdzajace czy istnieje juz taki klient na tej wycieczce
    const string checkSql = "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
    
    await using (var check = new SqlCommand(checkSql, connection))
    {
        check.Parameters.AddWithValue("@IdClient", clientId);
        check.Parameters.AddWithValue("@IdTrip", tripId);

        var exists = await check.ExecuteScalarAsync();
        if (exists != null)
            throw new InvalidOperationException("Klient jest już zapisany na tę wycieczkę.");
    }

    // zapisanie klienta
    //dodanie klienta do tablicy klient-wycieczka
    var insert = new SqlCommand("INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@id, @tripId, @date)", connection);
    insert.Parameters.AddWithValue("@id", clientId);
    insert.Parameters.AddWithValue("@tripId", tripId);
    insert.Parameters.AddWithValue("@date", int.Parse(DateTime.Now.ToString("yyyyMMdd")));
    await insert.ExecuteNonQueryAsync();

    return "Client successfully registered for the trip.";
    }

    public async Task UnregisterClientFromTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        //zapytanie sprawdzajace czy taki klient jest na takiej wycieczce
        const string checkSql = "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

        await using (var check = new SqlCommand(checkSql, connection))
        {
            check.Parameters.AddWithValue("@IdClient", clientId);
            check.Parameters.AddWithValue("@IdTrip", tripId);

            var exists = await check.ExecuteScalarAsync();
            if (exists == null)
                throw new KeyNotFoundException("Rejestracja klienta na tę wycieczkę nie istnieje.");
        }
        //zapytanie usuwajace polaczenie klienta z wycieczka
        const string deleteSql = "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

        await using (var delete = new SqlCommand(deleteSql, connection))
        {
            delete.Parameters.AddWithValue("@IdClient", clientId);
            delete.Parameters.AddWithValue("@IdTrip", tripId);
            await delete.ExecuteNonQueryAsync();
        }
    }
    
}