using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace ExperimentConfigSidecar.Services;

/// <summary>
/// Transforms the request path.
/// </summary>
/// <param name="path">The path to transform to.</param>
class PathTransformer(string path) : HttpTransformer
{
    public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);
        var queryContext = new QueryTransformContext(httpContext.Request);
        proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress(destinationPrefix, path, queryContext.QueryString);
    }
}