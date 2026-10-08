using Muyu.Modules.Escrow.Application;

namespace Muyu.Modules.Escrow.Infrastructure;

/// <summary>
/// Adaptador de desarrollo. En producción se reemplaza por la implementación del proveedor de pagos.
/// </summary>
public sealed class PaymentGateway : IPaymentGateway
{
    public Task<PaymentResult> TransferAsync(
        Guid recipientId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reference = $"MUYU-{idempotencyKey.ToUpperInvariant()}";
        return Task.FromResult(new PaymentResult(true, reference, null));
    }
}
