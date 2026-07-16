using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace BackendSdk.Internal
{
    internal static class UnityWebRequestExtensions
    {
        internal static Task SendWebRequestAsync(this UnityWebRequest request, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            var operation = request.SendWebRequest();

            operation.completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.ConnectionError && cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                tcs.TrySetResult(true);
            };

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
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

            return tcs.Task;
        }
    }
}
