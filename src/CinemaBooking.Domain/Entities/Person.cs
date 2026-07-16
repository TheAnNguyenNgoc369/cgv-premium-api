namespace CinemaBooking.Domain.Entities;

public class Person
{
    public int PersonId { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MoviePerson> MoviePersons { get; set; } = [];
}
