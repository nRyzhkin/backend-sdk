using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace BackendSdk.Internal
{
    internal static class UnityWebRequestExtensions
    {
        /// <summary>
        /// Completes when the UnityWebRequest finishes on the player loop.
        /// Do not use ConfigureAwait(false): Unity and WebGL have no worker thread pool;
        /// PlayerPrefs / UnityWebRequest must stay on the main thread.
        /// </summary>
        internal static Task SendWebRequestAsync(this UnityWebRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tcs = new TaskCompletionSource<bool>();
            var operation = request.SendWebRequest();

            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    try
                    {
                        request.Abort();
                    }
                    finally
                    {
                        tcs.TrySetCanceled(cancellationToken);
                    }
                });
            }

            operation.completed += _ =>
            {
                registration.Dispose();

                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                tcs.TrySetResult(true);
            };

            return tcs.Task;
        }
    }
}
