using BackendSdk.Transport.Core;

namespace BackendSdk.Internal
{
    internal sealed class UnityTransportBodySerializer : ITransportBodySerializer
    {
        internal static readonly UnityTransportBodySerializer Instance = new UnityTransportBodySerializer();

        private UnityTransportBodySerializer()
        {
        }

        public string Serialize(object value)
        {
            return UnityJsonSerializer.Serialize(value);
        }
    }
}
