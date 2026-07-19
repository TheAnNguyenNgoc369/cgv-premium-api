namespace CinemaBooking.Domain.Entities;

public class ReviewRewardSettings
{
    public int Id { get; set; }
    public int FirstReviewPoints { get; set; }
    public int NextReviewPoints { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public User? UpdatedByUser { get; set; }
}
