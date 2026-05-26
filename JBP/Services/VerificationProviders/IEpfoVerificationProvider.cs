using JBP.Models;

namespace JBP.Services.VerificationProviders
{
    public interface IEpfoVerificationProvider
    {
        Task<UanVerificationResult> VerifyUanAsync(
            string uan,
            CancellationToken cancellationToken = default);
    }
}
