using System.Threading;
using System.Threading.Tasks;

namespace BackendSdk
{
    /// <summary>
    /// Payload for a client critical-error report.
    /// </summary>
    public sealed class ClientErrorReportRequest
    {
        public ClientErrorReportRequest(
            string message,
            string logType,
            string place = null,
            string trackId = null,
            string trackName = null,
            string transportId = null,
            string transportName = null,
            object context = null)
        {
            Message = message ?? string.Empty;
            LogType = logType ?? string.Empty;
            Place = place ?? string.Empty;
            TrackId = trackId ?? string.Empty;
            TrackName = trackName ?? string.Empty;
            TransportId = transportId ?? string.Empty;
            TransportName = transportName ?? string.Empty;
            Context = context;
        }

        public string Message { get; }
        public string LogType { get; }
        public string Place { get; }
        public string TrackId { get; }
        public string TrackName { get; }
        public string TransportId { get; }
        public string TransportName { get; }
        public object Context { get; }
    }

    /// <summary>
    /// Client error reporting contract.
    /// </summary>
    public interface IClientErrorsService
    {
        /// <summary>
        /// Reports a critical client error for the authenticated player.
        /// </summary>
        Task ReportAsync(
            ClientErrorReportRequest request,
            CancellationToken cancellationToken = default);
    }
}
