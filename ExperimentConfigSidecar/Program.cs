using System.Net.Http.Headers;
using Dapr;
using ExperimentConfigSidecar.Models;
using ExperimentConfigSidecar.Services;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddHttpForwarder();

var app = builder.Build();
var httpClient = new HttpClient();
var httpMessageInvoker = new HttpMessageInvoker(new SocketsHttpHandler());
var logger = app.Logger;
var configService = new ConfigService();

var appPort = int.Parse(app.Configuration["APP_PORT"] ?? "8080");
var serviceName = app.Configuration["SERVICE_NAME"] ?? "missing-service-name";
var heartbeatInterval = int.Parse(app.Configuration["HEARTBEAT_INTERVAL"] ?? "1000");

var appUrl = $"http://localhost:{appPort}";
var replicaId = Guid.NewGuid();
const string pubsubName = "experiment-config-pubsub";

app.MapGet("/dapr/subscribe", async () =>
{
    logger.LogInformation("Received subscription request");
    List<SubscriptionSpec> subscriptionSpecs;
    try
    {
        var responseMessage = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{appUrl}/dapr/subscribe"));
        subscriptionSpecs = await responseMessage.Content.ReadFromJsonAsync<List<SubscriptionSpec>>();
    }
    catch (Exception e)
    {
        subscriptionSpecs = [];
        logger.LogInformation("Failed to get subscriptions from service");
        logger.LogDebug(e, "Cause");
    }
    foreach (var spec in subscriptionSpecs)
    {
        spec.Route = spec.Route.StartsWith('/') ? $"/_ecs/pubsub{spec.Route}" : $"/_ecs/pubsub/{spec.Route}";
    }
    subscriptionSpecs.Add(new SubscriptionSpec
    {
        Topic = $"config/{serviceName}",
        Pubsubname = pubsubName,
        Route = "/_ecs/variables-event",
    });
    return subscriptionSpecs;
});

app.MapPost("/_ecs/variables-event", async context =>
{
    var cloudEvent = await context.Request.ReadFromJsonAsync<CloudEvent<ConfigurationEvent>>();
    var config = cloudEvent.Data.Configurations.Where(config => Guid.Parse(config.ReplicaId) == replicaId).FirstOrDefault();
    if (config != null)
    {
        var remainingConfig = configService.UpdateConfig(config.Variables);
        if (remainingConfig.Count > 0)
        {
            var responseMessage = await httpClient.PostAsJsonAsync($"{appUrl}/ecs/variables", remainingConfig);
            responseMessage.EnsureSuccessStatusCode();
        }
    }
    context.Response.StatusCode = 200;
});

app.MapGet("/_ecs/defined-variables", async () => {
    Dictionary<string, VariableDefinition> config;
    try
    {
        var responseMessage = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{appUrl}/ecs/defined-variables"));
        config = await responseMessage.Content.ReadFromJsonAsync<Dictionary<string, VariableDefinition>>();
        logger.LogInformation("Received config properties: {Config}", config);
    }
    catch (Exception e)
    {
        config = [];
        logger.LogInformation("Failed to get defined variables from service");
        logger.LogDebug(e, "Cause");
    }
    configService.AddVariableDefinitions(config);
    return new VariableDefinitions(config);
});

app.Map("/_ecs/pubsub/{**path}", async (HttpContext context, IHttpForwarder forwarder) => {
    var path = context.Request.RouteValues["path"] as string;
    var deterioration = configService.GetPubsubDeterioration();
    await Util.ProxyRequest(appUrl, $"/{path}", context, deterioration, httpMessageInvoker, forwarder);
});

app.MapFallback(async (HttpContext context, IHttpForwarder forwarder) => {
    var path = context.Request.Path;
    var deterioration = configService.GetServiceInvocationDeterioration(path);
    await Util.ProxyRequest(appUrl, path, context, deterioration, httpMessageInvoker, forwarder);
});

logger.LogInformation("Waiting for application on port {AppPort}", appPort);
new StartupService().WaitForStartup(appPort).Wait();
logger.LogInformation("Application is running on port {AppPort}", appPort);

new HeartbeatService(heartbeatInterval, pubsubName, replicaId, serviceName, logger).StartAsync();

app.Run();
