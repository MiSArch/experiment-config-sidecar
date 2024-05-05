using System.Net.Http.Headers;
using Dapr;
using ExperimentConfigSidecar.Models;
using ExperimentConfigSidecar.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();
var httpClient = new HttpClient();
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
    var responseMessage = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{appUrl}/dapr/subscribe"));
    var subscriptionSpecs = await responseMessage.Content.ReadFromJsonAsync<List<SubscriptionSpec>>();
    foreach (var spec in subscriptionSpecs)
    {
        spec.Route = spec.Route.StartsWith('/') ? $"/_ecs/pubsub{spec.Route}" : $"/_esc/pubsub/{spec.Route}";
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
        logger.LogError(e, "Failed to get defined variables from service");
    }
    configService.AddVariableDefinitions(config);
    return new VariableDefinitions(config);
});

app.MapPost("/_ecs/pubsub/{**path}", async context => {
    var path = context.Request.RouteValues["path"];
    var requestUrl = $"{appUrl}/{path}";
    var deterioration = configService.GetPubsubDeterioration();
    await Util.ProxyRequest(requestUrl, HttpMethod.Post, context, deterioration, httpClient);
});

app.MapFallback(async context =>
{
    var path = context.Request.Path;
    var method = context.Request.Method;
    var requestUrl = $"{appUrl}{path}";
    var deterioration = configService.GetServiceInvocationDeterioration(path);
    await Util.ProxyRequest(requestUrl, new HttpMethod(method), context, deterioration, httpClient);
});

logger.LogInformation("Waiting for application on port {AppPort}", appPort);
new StartupService().WaitForStartup(appPort).Wait();
logger.LogInformation("Application is running on port {AppPort}", appPort);

new HeartbeatService(heartbeatInterval, pubsubName, replicaId, serviceName, logger).StartAsync();

app.Run();
