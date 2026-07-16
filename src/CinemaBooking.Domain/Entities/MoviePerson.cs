namespace CinemaBooking.Domain.Entities;

public class MoviePerson
{
    public int MovieId { get; set; }
    public int PersonId { get; set; }
    public string Role { get; set; } = null!;
    public int DisplayOrder { get; set; }

    public Movie Movie { get; set; } = null!;
    public Person Person { get; set; } = null!;
}
