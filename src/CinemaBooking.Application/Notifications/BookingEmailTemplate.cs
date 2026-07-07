using System.Net;

namespace CinemaBooking.Application.Notifications;

internal static class BookingEmailTemplate
{
    public static string BuildBookingConfirmed(
        string fullName,
        string bookingCode,
        string movieTitle,
        int durationMinutes,
        string format,
        string showtime,
        string cinemaName,
        string roomName,
        string amount,
        string paymentMethod,
        string ticketsHtml) => $"""
        {OpenCard()}
            <p style="margin: 0 0 6px; color: #111111; font-size: 20px; font-weight: 700;">Ticket booking confirmed</p>
            <p style="margin: 0 0 24px; color: #555555; font-size: 14px; line-height: 1.6;">
                Hello <strong>{Encode(fullName)}</strong>,<br />
                The order <strong>{Encode(bookingCode)}</strong> has been sucessfully paid.
            </p>
            {DetailRow("Movie", $"{Encode(movieTitle)} ({durationMinutes} phút)")}
            {DetailRow("Format", Encode(format))}
            {DetailRow("Showtime", Encode(showtime))}
            {DetailRow("Cinema", $"{Encode(cinemaName)} - {Encode(roomName)}")}
            {DetailRow("Payment", $"{Encode(amount)} VND - {Encode(paymentMethod)}")}
            <p style="margin: 24px 0 8px; color: #111111; font-size: 16px; font-weight: 700;">Digital ticket</p>
            <div style="text-align: center;">{ticketsHtml}</div>
            {Notice("Each QR code is valid for only one ticket and one check-in.")}
        {CloseCard()}
        """;

    public static string BuildTicket(
        string seatCode,
        string seatType,
        string qrCode,
        string contentId,
        string fileName) => $"""
        <div style="display: inline-block; width: 210px; margin: 8px; padding: 14px; vertical-align: top; border: 1px solid #e0e0e0; border-radius: 6px; text-align: center;">
            <p style="margin: 0 0 4px; color: #111111; font-size: 16px; font-weight: 700;">Seat {Encode(seatCode)}</p>
            <p style="margin: 0 0 10px; color: #777777; font-size: 13px;">{Encode(seatType)}</p>
            <img src="cid:{Encode(contentId)}" alt="QR code for seat tickets {Encode(seatCode)}" width="180" height="180" style="display: block; margin: 0 auto 8px;" />
            <p style="margin: 0; color: #999999; font-family: Consolas, 'Courier New', monospace; font-size: 10px; overflow-wrap: anywhere;">{Encode(qrCode)}</p>
        </div>
        """;

    public static string BuildRefundProcessed(
        string fullName,
        string bookingCode,
        string movieTitle,
        string originalShowtime,
        string refundAmount,
        string completedAt) => $"""
        {OpenCard()}
            <p style="margin: 0 0 6px; color: #111111; font-size: 20px; font-weight: 700;">Refund successful</p>
            <p style="margin: 0 0 24px; color: #555555; font-size: 14px; line-height: 1.6;">
                Hello <strong>{Encode(fullName)}</strong>,<br />
                Your refund request for your ticket order has been successfully processed.
            </p>
            {DetailRow("Booking code", Encode(bookingCode))}
            {DetailRow("Movie", Encode(movieTitle))}
            {DetailRow("Original showtime", Encode(originalShowtime))}
            {DetailRow("Refund amount", $"{Encode(refundAmount)} VND")}
            {DetailRow("Refund method", "Add to wallet")}
            {DetailRow("Completed at", Encode(completedAt))}
            {Notice("Please check your balance in the EGift Wallet section at profile.")}
        {CloseCard()}
        """;

    private static string OpenCard() => """
        <div style="font-family: Arial, Helvetica, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; border: 1px solid #e0e0e0;">
            <div style="background: #c62828; padding: 22px 32px; text-align: center;">
                <span style="color: #ffffff; font-size: 20px; font-weight: 700; letter-spacing: 1px;">CGV Premium</span>
            </div>
            <div style="padding: 32px 36px 24px;">
        """;

    private static string CloseCard() => $"""
            </div>
            <div style="background: #f9f9f9; border-top: 1px solid #eeeeee; padding: 14px 36px; text-align: center;">
                <p style="margin: 0 0 2px; font-size: 11px; color: #aaaaaa;">&copy; {DateTime.UtcNow.Year} CGV Premium. All rights reserved.</p>
            </div>
        </div>
        """;

    private static string DetailRow(string label, string value) => $"""
        <div style="padding: 9px 12px; border-bottom: 1px solid #eeeeee; font-size: 14px; line-height: 1.5;">
            <span style="display: inline-block; width: 145px; color: #777777;">{label}</span>
            <strong style="color: #222222;">{value}</strong>
        </div>
        """;

    private static string Notice(string message) => $"""
        <div style="margin-top: 24px; background: #fff8e1; border-left: 3px solid #f9a825; border-radius: 0 4px 4px 0; padding: 10px 14px;">
            <p style="margin: 0; font-size: 13px; color: #7a6000; line-height: 1.5;">{message}</p>
        </div>
        """;

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
