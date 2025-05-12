namespace Tutorial8.Models.DTOs;

public class ClientTripsDTO
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public List<ClientTripDTO> Trips { get; set; } = new List<ClientTripDTO>();
}

public class ClientTripDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public int RegisteredAt { get; set; }
    public int? PaymentDate { get; set; }
}