using CinemaBooking.API.Contracts.Wallet;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public sealed class WalletController : ControllerBase
{
    private readonly IWalletRepository _walletRepository;

    public WalletController(IWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? transactionType = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        if (page < 1)
            page = 1;

        if (pageSize < 1 || pageSize > 100)
            pageSize = 20;

        if (!string.IsNullOrEmpty(transactionType))
        {
            if (transactionType != WalletTransactionType.Payment
                && transactionType != WalletTransactionType.Refund
                && transactionType != WalletTransactionType.TopUp)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid transaction type. Valid types: payment, refund, top_up"
                });
            }
        }

        var (transactions, totalCount) = await _walletRepository.GetTransactionsWithFiltersAsync(
            userId,
            page,
            pageSize,
            fromDate,
            toDate,
            transactionType,
            cancellationToken);

        var response = new PagedWalletTransactionResponse(
            transactions.Select(t => new WalletTransactionResponse(
                t.TransactionID,
                t.TransactionType,
                t.Amount,
                t.BalanceAfter,
                t.Booking?.BookingCode,
                t.Description,
                t.CreatedAt
            )).ToList(),
            totalCount,
            page,
            pageSize
        );

        return Ok(response);
    }

    [HttpGet("transactions/{transactionId}")]
    public async Task<IActionResult> GetTransactionById(
        int transactionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var transaction = await _walletRepository.GetTransactionByIdAsync(
            transactionId,
            cancellationToken);

        if (transaction is null)
        {
            return NotFound(new
            {
                success = false,
                message = "Transaction not found."
            });
        }

        var wallet = await _walletRepository.GetWalletByUserIdAsync(userId, cancellationToken);
        if (wallet is null || transaction.WalletID != wallet.WalletID)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                message = "You cannot access another user's transaction."
            });
        }

        var response = new WalletTransactionDetailResponse(
            transaction.TransactionID,
            transaction.TransactionType,
            transaction.Amount,
            transaction.BalanceAfter,
            transaction.Booking?.BookingCode,
            transaction.RefundID,
            transaction.Description,
            transaction.CreatedAt
        );

        return Ok(response);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetWalletSummary(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var wallet = await _walletRepository.GetWalletByUserIdAsync(userId, cancellationToken);
        if (wallet is null)
        {
            return Ok(new WalletSummaryResponse(
                CurrentBalance: 0,
                TotalRefundReceived: 0,
                TotalSpent: 0,
                TransactionCount: 0
            ));
        }

        var (totalRefundReceived, totalSpent, transactionCount) =
            await _walletRepository.GetWalletSummaryAsync(userId, cancellationToken);

        var response = new WalletSummaryResponse(
            CurrentBalance: wallet.Balance,
            TotalRefundReceived: totalRefundReceived,
            TotalSpent: totalSpent,
            TransactionCount: transactionCount
        );

        return Ok(response);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdValue = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdValue, out userId);
    }
}
