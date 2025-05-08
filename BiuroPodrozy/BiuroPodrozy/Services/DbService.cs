using BiuroPodrozy.Exceptions;
using BiuroPodrozy.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace BiuroPodrozy.Services;

public interface IDbService
{
    public Task<IEnumerable<TripGetDTO>> GetAllTripsAsync();
    public Task<ClientGetDTO> GetClientDetailsByIdAsync(int id);
}

public class DbService(IConfiguration configuration) : IDbService
{
    private readonly string? _connectionString = configuration.GetConnectionString("Default");//pobiera connection string z appsettings.json
    public async Task<IEnumerable<TripGetDTO>> GetAllTripsAsync()
    {
        var result = new List<TripGetDTO>();
        
        await using var connection = new SqlConnection(_connectionString);   // tworzy nowe polaczenie z baza
        var sql = "select t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name from Trips t join Country_Trip ct on t.IdTrip = ct.IdTrip join Country c on ct.IdCountry = c.IdCountry"; //zapytanie pobierajace dane o wszystkich wycieczkach
        
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
                DateFrom = Convert.ToDateTime(reader.GetString(3)),
                DateTo = Convert.ToDateTime(reader.GetString(4)),
                MaxPeople = Convert.ToInt32(reader.GetString(5))
            });
        }
        
        return result;
    }
    
    public async Task<ClientGetDTO> GetClientDetailsByIdAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "select t.IdTrip ,t.Name,t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.RegisteredAt, ct.PaymentDate from Trips t join Client_Trip ct on t.IdTrip = ct.IdTrip join Client c on c.IdClient = ct.IdClient where c.IdClient = @id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        
        if (!await reader.ReadAsync())
        {
            throw new NotFoundException($"Client with id: {id} does not exist");
        }

        return new ClientGetDTO
        {
            IdClient = reader.GetInt32(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            Email = reader.GetString(3),
            Telephone = reader.GetString(4),
            Pesel = reader.GetString(5)
        };
    }
}