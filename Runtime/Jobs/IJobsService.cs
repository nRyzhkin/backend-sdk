using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk
{
    /// <summary>
    /// Player-facing Jobs API: list offers, start a run, complete a run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All methods require an authenticated player. ApplicationId is taken from SDK settings.
    /// </para>
    /// <para>
    /// Stable server error codes (via <see cref="BackendException.ErrorCode"/>):
    /// <c>jobs_already_in_progress</c> (409) — a run of this job is already active; resume gameplay and call complete;
    /// <c>jobs_insufficient_tickets</c>;
    /// <c>jobs_no_active_run</c>;
    /// <c>jobs_not_found</c>;
    /// <c>jobs_disabled</c>.
    /// </para>
    /// </remarks>
    public interface IJobsService
    {
        /// <summary>
        /// Gets all enabled job offers for the current player.
        /// </summary>
        Task<JobOfferBatch> GetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single job offer by id (e.g. <c>cleaner</c>).
        /// </summary>
        Task<JobOffer> GetAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a job run (spends a ticket). Idempotent on the transport layer.
        /// </summary>
        Task<JobStartResult> StartAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes the active run and grants rewards. Idempotent on the transport layer.
        /// </summary>
        Task<JobCompleteResult> CompleteAsync(string jobId, CancellationToken cancellationToken = default);
    }
}
