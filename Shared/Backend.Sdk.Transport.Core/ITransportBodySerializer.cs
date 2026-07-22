namespace BackendSdk.Transport.Core
{
    internal interface ITransportBodySerializer
    {
        string Serialize(object value);
    }
}
