using System;
using System.Text;

namespace UnityEngine
{
    public static class Debug
    {
        public static void Log(object message)
        {
        }
    }

    public static class Application
    {
        public static bool isEditor => true;
    }

    public static class JsonUtility
    {
        public static string ToJson(object value)
        {
            return value == null ? string.Empty : value.ToString();
        }

        public static T FromJson<T>(string json)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)(json ?? string.Empty);
            }

            throw new NotSupportedException("JsonUtility.FromJson is not supported in dotnet test stubs.");
        }

        public static object FromJson(string json, Type type)
        {
            if (type == typeof(string))
            {
                return json ?? string.Empty;
            }

            throw new NotSupportedException("JsonUtility.FromJson is not supported in dotnet test stubs.");
        }
    }

    public class AsyncOperation
    {
        public bool isDone { get; set; } = true;
    }
}

namespace UnityEngine.Networking
{
    public class DownloadHandler
    {
        public string text { get; set; } = string.Empty;
    }

    public sealed class DownloadHandlerBuffer : DownloadHandler
    {
    }

    public sealed class UploadHandlerRaw
    {
        public UploadHandlerRaw(byte[] data)
        {
            Data = data ?? Array.Empty<byte>();
        }

        public byte[] Data { get; }
    }

    public class UnityWebRequest : IDisposable
    {
        public enum Result
        {
            InProgress,
            Success,
            ConnectionError,
            ProtocolError,
            DataProcessingError
        }

        public UnityWebRequest(string url, string method)
        {
            this.url = url ?? string.Empty;
            Method = method ?? string.Empty;
        }

        public string url { get; private set; }

        public string Method { get; }

        public int timeout { get; set; }

        public Result result { get; set; } = Result.Success;

        public long responseCode { get; set; } = 200;

        public string error { get; set; } = string.Empty;

        public DownloadHandler downloadHandler { get; set; }

        public UploadHandlerRaw uploadHandler { get; set; }

        public void SetRequestHeader(string name, string value)
        {
        }

        public UnityWebRequestAsyncOperation SendWebRequest()
        {
            return new UnityWebRequestAsyncOperation();
        }

        public void Abort()
        {
        }

        public void Dispose()
        {
        }
    }

    public sealed class UnityWebRequestAsyncOperation : AsyncOperation
    {
        public event Action<AsyncOperation> completed
        {
            add
            {
                value?.Invoke(this);
            }
            remove
            {
            }
        }
    }
}
