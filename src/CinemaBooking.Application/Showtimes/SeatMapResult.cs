using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace CinemaBooking.Application.Showtimes;

public sealed record SeatMapResult(
    int ShowtimeID,
    string RoomName,
    string RoomType,
    List<SeatMapSeatResult> Seats
);

public sealed record SeatMapSeatResult(
    int SeatID,
    string SeatRow,
    int SeatCol,
    string SeatType,
    decimal ExtraPrice,
    decimal Price,
    string Status   // available | held | booked
);