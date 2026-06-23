using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace CinemaBooking.Shared.Constants;

public static class BookingStatus
{
    public const string Pending = "pending";
    public const string Paid = "paid";
    public const string Cancelled = "cancelled";
    public const string Refunded = "refunded";
    public const string Used = "used";
    public const string Expired = "expired";
    public const string PaymentFailed = "payment_failed";
    public const string PartiallyRefunded = "partially_refunded";
}