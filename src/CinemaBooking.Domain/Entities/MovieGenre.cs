namespace CinemaBooking.Domain.Entities;

public class MovieGenre
{
    public int MovieGenreID { get; set; }
    public int MovieID { get; set; }
    public int GenreID { get; set; }

    public Movie Movie { get; set; } = null!;
    public Genre Genre { get; set; } = null!;
}
