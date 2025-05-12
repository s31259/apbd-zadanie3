using System.Data.Common;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.Data.SqlClient;
using Tutorial8.Exceptions;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString;

    public TripsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<bool> DoesClientExist(int idClient)
    {
        var command = "SELECT * FROM Client WHERE IdClient = @IdClient";

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand();

        cmd.Connection = conn;
        cmd.CommandText = command;
        cmd.Parameters.AddWithValue("@IdClient", idClient);

        await conn.OpenAsync();

        var client = await cmd.ExecuteScalarAsync();

        return client is not null;
    }

    public async Task<bool> DoesTripExist(int idTrip)
    {
        var command = "SELECT * FROM Trip WHERE IdTrip = @IdTrip";

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand();

        cmd.Connection = conn;
        cmd.CommandText = command;
        cmd.Parameters.AddWithValue("@IdTrip", idTrip);

        await conn.OpenAsync();

        var client = await cmd.ExecuteScalarAsync();

        return client is not null;
    }

    public async Task<bool> DoesRegistrationExist(int idClient, int idTrip)
    {
        var command = "SELECT * FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand();
        
        cmd.Connection = conn;
        cmd.CommandText = command;
        
        cmd.Parameters.AddWithValue("@IdClient", idClient);
        cmd.Parameters.AddWithValue("@IdTrip", idTrip);
        
        await conn.OpenAsync();
        
        var registration = await cmd.ExecuteScalarAsync();
        
        return registration is not null;
    }

    public async Task<List<TripDTO>> GetAllTripsAsync()
    {
        string command = @"SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name 
FROM Trip t JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip JOIN Country c ON c.IdCountry = ct.IdCountry ORDER BY t.IdTrip";

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand(command, conn);
        await conn.OpenAsync();

        var reader = await cmd.ExecuteReaderAsync();

        var trips = new List<TripDTO>();

        while (await reader.ReadAsync())
        {
            int tripId = reader.GetInt32(0);

            var doesTripAlreadyAdded = trips.FirstOrDefault(e => e.Id == tripId);

            if (doesTripAlreadyAdded == null)
            {
                var tripToAdd = new TripDTO()
                {
                    Id = tripId,
                    Name = reader.GetString(1),
                    Description = await reader.IsDBNullAsync(2) ? null : reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                };

                var countryName = reader.GetString(6);
                tripToAdd.Countries.Add(new CountryDTO()
                {
                    Name = countryName
                });

                trips.Add(tripToAdd);
            }
            else
            {
                var countryName = reader.GetString(6);

                var doesCountryAlreadyAdded =
                    trips.FirstOrDefault(t => t.Countries.Any(c => c.Name == countryName));

                if (doesCountryAlreadyAdded == null)
                {
                    var tripWithCurrentId = trips.FirstOrDefault(t => t.Id == tripId);

                    tripWithCurrentId.Countries.Add(new CountryDTO() { Name = countryName });

                }
            }
        }

        return trips;
    }

    public async Task<ClientTripsDTO> GetClientTripsAsync(int idClient)
    {
        string command =
            @"SELECT c.IdClient, c.FirstName, c.LastName, t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.RegisteredAt, ct.PaymentDate
FROM Trip t JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip JOIN Client C on c.IdClient = ct.IdClient WHERE ct.IdClient = @IdClient";

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand(command, conn);
        await conn.OpenAsync();

        cmd.Parameters.AddWithValue("@IdClient", idClient);
        var reader = await cmd.ExecuteReaderAsync();

        ClientTripsDTO? clientTripsDTO = null;

        while (await reader.ReadAsync())
        {
            if (clientTripsDTO == null)
            {
                clientTripsDTO = new ClientTripsDTO()
                {
                    Id = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                };
            }

            var trip = new ClientTripDTO()
            {
                Id = reader.GetInt32(3),
                Name = reader.GetString(4),
                Description = await reader.IsDBNullAsync(5) ? null : reader.GetString(5),
                DateFrom = reader.GetDateTime(6),
                DateTo = reader.GetDateTime(7),
                MaxPeople = reader.GetInt32(8),
                RegisteredAt = reader.GetInt32(9),
                PaymentDate = await reader.IsDBNullAsync(10) ? null : reader.GetInt32(10)
            };

            clientTripsDTO.Trips.Add(trip);
        }

        if (clientTripsDTO == null)
        {
            throw new NotFoundException("No trips found for the specified client");
        }

        return clientTripsDTO;
    }

    public async Task<int> CreateNewClientAsync(ClientDTO clientDTO)
    {
        string command = @"INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) VALUES
(@FirstName, @LastName, @Email, @Telephone, @Pesel); SELECT @@IDENTITY AS IdClient";

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand(command, conn);
        await conn.OpenAsync();

        DbTransaction transaction = await conn.BeginTransactionAsync();
        cmd.Transaction = transaction as SqlTransaction;

        try
        {
            cmd.Parameters.AddWithValue("@FirstName", clientDTO.FirstName);
            cmd.Parameters.AddWithValue("@LastName", clientDTO.LastName);
            cmd.Parameters.AddWithValue("@Email", clientDTO.Email);
            cmd.Parameters.AddWithValue("@Telephone", clientDTO.Telephone);
            cmd.Parameters.AddWithValue("@Pesel", clientDTO.Pesel);

            var clientId = await cmd.ExecuteScalarAsync();

            await transaction.CommitAsync();

            return Convert.ToInt32(clientId);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RegisterClientOnTripAsync(int idClient, int idTrip)
    {
        string command = "SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip";

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand(command, conn);
        await conn.OpenAsync();

        DbTransaction transaction = await conn.BeginTransactionAsync();
        cmd.Transaction = transaction as SqlTransaction;

        try
        {

            cmd.Parameters.AddWithValue("@IdTrip", idTrip);
            var maxClients = await cmd.ExecuteScalarAsync();
            cmd.Parameters.Clear();

            command = "SELECT * FROM Client_Trip WHERE IdTrip = @IdTrip";
            cmd.CommandText = command;
            cmd.Parameters.AddWithValue("@IdTrip", idTrip);

            int clientCounter = 0;

            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                clientCounter++;
            }
            await reader.DisposeAsync();

            if (clientCounter > Convert.ToInt32(maxClients))
            {
                throw new ConflictException("Too much clients registered on this trip");
            }

            cmd.Parameters.Clear();


            string[] localDate = DateTime.Now.ToString("yyyy/MM/dd").Split(".");

            string registeredDateString = "";

            foreach (var str in localDate)
            {
                registeredDateString += str.Trim();
            }

            int registeredDate = Convert.ToInt32(registeredDateString);

            command = "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@IdClient, @IdTrip, @RegisteredAt)";
            cmd.CommandText = command;
            cmd.Parameters.AddWithValue("@IdClient", idClient);
            cmd.Parameters.AddWithValue("@IdTrip", idTrip);
            cmd.Parameters.AddWithValue("@RegisteredAt", registeredDate);
            await cmd.ExecuteNonQueryAsync();
            
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteClientRegistrationAsync(int idClient, int idTrip)
    {
        string command = "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand(command, conn);
        await conn.OpenAsync();
        
        cmd.Parameters.AddWithValue("@IdClient", idClient);
        cmd.Parameters.AddWithValue("@IdTrip", idTrip);
        
        DbTransaction transaction = await conn.BeginTransactionAsync();
        cmd.Transaction = transaction as SqlTransaction;

        try
        {
            await cmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}