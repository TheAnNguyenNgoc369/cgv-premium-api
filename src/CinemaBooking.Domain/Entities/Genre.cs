namespace CinemaBooking.Domain.Entities;

public class Genre
{
    public int GenreID { get; set; }
    public string GenreName { get; set; } = null!;

    public ICollection<MovieGenre> MovieGenres { get; set; } = [];
}
