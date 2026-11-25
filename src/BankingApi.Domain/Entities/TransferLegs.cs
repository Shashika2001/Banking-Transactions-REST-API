namespace BankingApi.Domain.Entities;

/// <summary>
/// The two transactions produced by a transfer between accounts.
/// </summary>
/// <param name="TransferId">The identifier shared by both legs.</param>
/// <param name="Outgoing">The debit recorded on the source account.</param>
/// <param name="Incoming">The credit recorded on the destination account.</param>
public sealed record TransferLegs(Guid TransferId, Transaction Outgoing, Transaction Incoming);
