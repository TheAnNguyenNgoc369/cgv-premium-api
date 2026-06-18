namespace CinemaBooking.Domain.Entities;

public class Movie
{
    public int MovieID { get; set; }
    public string Title { get; set; } = null!;
    public string AgeRating { get; set; } = null!;
    public string? Director { get; set; }
    public string? Cast { get; set; }
    public string? Description { get; set; }
    public string? PosterURL { get; set; }
    public string? PosterPublicId { get; set; }
    public string? TrailerURL { get; set; }
    public int DurationMin { get; set; }
    public DateOnly? ShowingFrom { get; set; }
    public DateOnly? ShowingTo { get; set; }
    public string Status { get; set; } = "coming_soon";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MovieGenre> MovieGenres { get; set; } = [];
    public ICollection<Showtime> Showtimes { get; set; } = [];
}
