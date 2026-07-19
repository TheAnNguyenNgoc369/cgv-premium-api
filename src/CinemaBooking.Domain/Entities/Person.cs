namespace CinemaBooking.Domain.Entities;

public class Person
{
    public int PersonId { get; set; }
    public string Name { get; set; } = null!;
    public string? Biography { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? Gender { get; set; }
    public string? PhotoUrl { get; set; }
    public string? PhotoPublicId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MoviePerson> MoviePersons { get; set; } = [];
}
