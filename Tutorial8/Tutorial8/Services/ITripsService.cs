using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<bool> DoesClientExist(int idClient);
    Task<bool> DoesTripExist(int idTrip);
    Task<bool> DoesRegistrationExist(int idClient, int idTrip);
    Task<List<TripDTO>> GetAllTripsAsync();
    Task<ClientTripsDTO> GetClientTripsAsync(int idClient);
    Task<int> CreateNewClientAsync(ClientDTO clientDTO);
    Task RegisterClientOnTripAsync(int idClient, int idTrip);
    Task DeleteClientRegistrationAsync(int idClient, int idTrip);
}