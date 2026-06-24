using CinemaBooking.Application.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public sealed class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvoiceById(
        int id,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(id, cancellationToken);

        if (invoice is null)
            return NotFound(new { message = "Không tìm thấy hóa đơn" });

        return Ok(invoice);
    }

    [HttpGet("booking/{bookingId}")]
    public async Task<IActionResult> GetInvoiceByBookingId(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetInvoiceByBookingIdAsync(bookingId, cancellationToken);

        if (invoice is null)
            return NotFound(new { message = "Không tìm thấy hóa đơn cho booking này" });

        return Ok(invoice);
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetInvoiceByCode(
        string code,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetInvoiceByCodeAsync(code, cancellationToken);

        if (invoice is null)
            return NotFound(new { message = "Không tìm thấy hóa đơn với mã này" });

        return Ok(invoice);
    }
}
