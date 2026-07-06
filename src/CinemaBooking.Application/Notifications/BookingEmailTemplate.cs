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
            <p style="margin: 0 0 6px; color: #111111; font-size: 20px; font-weight: 700;">Xác nhận đặt vé thành công</p>
            <p style="margin: 0 0 24px; color: #555555; font-size: 14px; line-height: 1.6;">
                Xin chào <strong>{Encode(fullName)}</strong>,<br />
                Đơn <strong>{Encode(bookingCode)}</strong> đã thanh toán thành công.
            </p>
            {DetailRow("Phim", $"{Encode(movieTitle)} ({durationMinutes} phút)")}
            {DetailRow("Định dạng", Encode(format))}
            {DetailRow("Suất chiếu", Encode(showtime))}
            {DetailRow("Rạp", $"{Encode(cinemaName)} - {Encode(roomName)}")}
            {DetailRow("Thanh toán", $"{Encode(amount)} VND - {Encode(paymentMethod)}")}
            <p style="margin: 24px 0 8px; color: #111111; font-size: 16px; font-weight: 700;">Vé điện tử</p>
            <div style="text-align: center;">{ticketsHtml}</div>
            {Notice("Mỗi QR chỉ có hiệu lực cho đúng một vé và một lần check-in.")}
        {CloseCard()}
        """;

    public static string BuildTicket(
        string seatCode,
        string seatType,
        string qrCode,
        string contentId,
        string fileName) => $"""
        <div style="display: inline-block; width: 210px; margin: 8px; padding: 14px; vertical-align: top; border: 1px solid #e0e0e0; border-radius: 6px; text-align: center;">
            <p style="margin: 0 0 4px; color: #111111; font-size: 16px; font-weight: 700;">Ghế {Encode(seatCode)}</p>
            <p style="margin: 0 0 10px; color: #777777; font-size: 13px;">{Encode(seatType)}</p>
            <img src="cid:{Encode(contentId)}" alt="QR vé ghế {Encode(seatCode)}" width="180" height="180" style="display: block; margin: 0 auto 8px;" />
            <p style="margin: 0; color: #999999; font-family: Consolas, 'Courier New', monospace; font-size: 10px; overflow-wrap: anywhere;">{Encode(qrCode)}</p>
            <a href="cid:{Encode(contentId)}" download="{Encode(fileName)}" style="display: inline-block; margin-top: 12px; background: #c62828; color: #ffffff; font-size: 13px; font-weight: 700; text-decoration: none; padding: 10px 18px; border-radius: 6px;">Tải QR (.png)</a>
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
            <p style="margin: 0 0 6px; color: #111111; font-size: 20px; font-weight: 700;">Hoàn tiền thành công</p>
            <p style="margin: 0 0 24px; color: #555555; font-size: 14px; line-height: 1.6;">
                Xin chào <strong>{Encode(fullName)}</strong>,<br />
                Yêu cầu hoàn tiền cho đơn đặt vé của bạn đã được xử lý thành công.
            </p>
            {DetailRow("Mã đặt vé", Encode(bookingCode))}
            {DetailRow("Phim", Encode(movieTitle))}
            {DetailRow("Suất chiếu ban đầu", Encode(originalShowtime))}
            {DetailRow("Số tiền hoàn trả", $"{Encode(refundAmount)} VND")}
            {DetailRow("Hình thức hoàn trả", "Cộng vào Ví CGV")}
            {DetailRow("Hoàn tất lúc", Encode(completedAt))}
            {Notice("Vui lòng kiểm tra số dư trong mục My Wallet trên Dashboard.")}
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
