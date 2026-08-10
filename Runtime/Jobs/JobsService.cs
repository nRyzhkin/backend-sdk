using System;
using System.Threading;
using System.Threading.Tasks;
using BackendSdk.Internal;

namespace BackendSdk
{
    /// <summary>
    /// Provides Jobs operations for the Backend SDK.
    /// </summary>
    /// <remarks>
    /// Paths are scoped to the configured application. Game code must not pass ApplicationId.
    /// When <c>StartAsync</c> fails with <c>jobs_already_in_progress</c>, resume the unfinished run
    /// (check <see cref="JobOffer.HasActiveRun"/>) and call <see cref="CompleteAsync"/> after gameplay.
    /// </remarks>
    public sealed class JobsService : IJobsService
    {
        /// <inheritdoc />
        public async Task<JobOfferBatch> GetAsync(CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();

            var responseJson = await client.GetRawAsync(BuildBasePath(client), cancellationToken).ConfigureAwait(false);
            return JobsJson.ParseBatch(responseJson);
        }

        /// <inheritdoc />
        public async Task<JobOffer> GetAsync(string jobId, CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateJobId(jobId);

            var responseJson = await client.GetRawAsync(BuildJobPath(client, jobId), cancellationToken)
                .ConfigureAwait(false);
            return JobsJson.ParseOffer(responseJson);
        }

        /// <inheritdoc />
        public async Task<JobStartResult> StartAsync(string jobId, CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateJobId(jobId);

            var responseJson = await client.PostJsonRawAsync(
                    $"{BuildJobPath(client, jobId)}/start",
                    "{}",
                    cancellationToken)
                .ConfigureAwait(false);
            return JobsJson.ParseStart(responseJson);
        }

        /// <inheritdoc />
        public async Task<JobCompleteResult> CompleteAsync(string jobId, CancellationToken cancellationToken = default)
        {
            var client = GetAuthenticatedClient();
            cancellationToken.ThrowIfCancellationRequested();
            ValidateJobId(jobId);

            var responseJson = await client.PostJsonRawAsync(
                    $"{BuildJobPath(client, jobId)}/complete",
                    "{}",
                    cancellationToken)
                .ConfigureAwait(false);
            return JobsJson.ParseComplete(responseJson);
        }

        private static BackendClient GetAuthenticatedClient()
        {
            EnsureInitialized();

            if (!Backend.Auth.IsAuthenticated)
            {
                throw new BackendException(
                    "Jobs requires an authenticated player. Call Backend.Auth.LoginAsync first.",
                    "not_authenticated");
            }

            return Backend.ClientOrThrow();
        }

        private static string BuildBasePath(BackendClient client)
        {
            var applicationId = Uri.EscapeDataString(client.ApplicationIdOrThrow());
            return $"v1/game/{applicationId}/jobs";
        }

        private static string BuildJobPath(BackendClient client, string jobId)
        {
            return $"{BuildBasePath(client)}/{Uri.EscapeDataString(jobId.Trim())}";
        }

        private static void EnsureInitialized()
        {
            if (!Backend.IsInitialized)
            {
                throw new BackendException("Backend SDK has not been initialized.", "backend_not_initialized");
            }
        }

        private static void ValidateJobId(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new BackendException("Job id is required.", "invalid_job_id");
            }
        }
    }
}
