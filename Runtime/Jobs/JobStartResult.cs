namespace BackendSdk
{
    /// <summary>
    /// Result of starting a job run.
    /// </summary>
    public sealed class JobStartResult
    {
        internal JobStartResult(JobOffer job)
        {
            Job = job ?? throw new System.ArgumentNullException(nameof(job));
        }

        /// <summary>Updated offer after the ticket was spent and the run became active.</summary>
        public JobOffer Job { get; }
    }
}
