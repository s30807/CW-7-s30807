namespace BiuroPodrozy.Models.DTOs;

public class ClientCreateDTO
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Telephone { get; set; }
    public required string Pesel { get; set; }
}