namespace CinemaBooking.Application.Common.Interfaces;

public sealed record EmailInlineImage(
    string ContentId,
    byte[] Content,
    string MediaType,
    string FileName);
