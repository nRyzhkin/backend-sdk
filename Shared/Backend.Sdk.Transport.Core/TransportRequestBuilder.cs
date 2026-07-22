using System;
using System.Collections.Generic;

namespace BackendSdk.Transport.Core
{
    /// <summary>
    /// Builds transport request metadata shared by the live transport and tests.
    /// </summary>
    internal static class TransportRequestBuilder
    {
        internal const string ContentType = "application/json";
        internal const string ApplicationIdHeader = "X-Application-Id";
        internal const string AuthorizationHeader = "Authorization";
        internal const string RequestIdHeader = "X-Request-Id";

        internal static ITransportBodySerializer BodySerializer { get; set; }

        internal static TransportRequestSnapshot Build(
            ITransportRequestSettings settings,
            HttpVerb verb,
            string path,
            object body,
            string authorizationHeader,
            RequestContext context)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Accept"] = ContentType
            };

            if (!string.IsNullOrWhiteSpace(settings.ApplicationId))
            {
                headers[ApplicationIdHeader] = settings.ApplicationId;
            }

            if (!string.IsNullOrWhiteSpace(authorizationHeader))
            {
                headers[AuthorizationHeader] = authorizationHeader;
            }

            if (!string.IsNullOrEmpty(context?.RequestId))
            {
                headers[RequestIdHeader] = context.RequestId;
            }

            var payload = ResolvePayload(verb, body);
            if (!string.IsNullOrEmpty(payload))
            {
                headers["Content-Type"] = ContentType;
            }

            return new TransportRequestSnapshot(
                MapMethod(verb),
                BuildUrl(settings, path),
                payload,
                headers);
        }

        internal static RequestContext CreateRequestContext(HttpVerb verb, object body)
        {
            if (verb == HttpVerb.Get || body is ReadOnlyJsonRequestBody)
            {
                return new RequestContext(null);
            }

            return new RequestContext(Guid.NewGuid().ToString("N"));
        }

        internal static string ResolvePayload(HttpVerb verb, object body)
        {
            if (verb == HttpVerb.Get || verb == HttpVerb.Delete)
            {
                return string.Empty;
            }

            if (body is JsonRequestBody jsonRequest)
            {
                return jsonRequest.Json;
            }

            if (body is ReadOnlyJsonRequestBody readOnlyJsonRequest)
            {
                return readOnlyJsonRequest.Json;
            }

            if (body == null)
            {
                return string.Empty;
            }

            if (BodySerializer == null)
            {
                throw new InvalidOperationException(
                    "TransportRequestBuilder.BodySerializer must be configured before serializing DTO bodies.");
            }

            return BodySerializer.Serialize(body);
        }

        private static string BuildUrl(ITransportRequestSettings settings, string path)
        {
            var baseUrl = (settings.ServerUrl ?? string.Empty).TrimEnd('/');
            var relativePath = (path ?? string.Empty).TrimStart('/');

            if (string.IsNullOrEmpty(baseUrl))
            {
                return relativePath;
            }

            if (string.IsNullOrEmpty(relativePath))
            {
                return baseUrl;
            }

            return $"{baseUrl}/{relativePath}";
        }

        private static string MapMethod(HttpVerb verb)
        {
            switch (verb)
            {
                case HttpVerb.Get:
                    return TransportHttpMethods.Get;
                case HttpVerb.Post:
                    return TransportHttpMethods.Post;
                case HttpVerb.Put:
                    return TransportHttpMethods.Put;
                case HttpVerb.Delete:
                    return TransportHttpMethods.Delete;
                default:
                    throw new ArgumentOutOfRangeException(nameof(verb), verb, "Unsupported HTTP verb.");
            }
        }
    }
}
