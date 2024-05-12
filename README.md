# Experiment Config Sidecar

This sidecar acts as a proxy between the dapr sidecar and the application.
It is responsible for managing experiment configuration properties of both the sidecar and the application.

## Features

- Artificial CPU load on one or more threads (simulates a performance degradation)
- Artificial memory usage (simulates a memory leak)
- Artificial delay and error rate on service invocation (simulates network latency and errors)
- Artificial delay and error rate on dapr events (simulates network latency and errors)

## Getting Started

The sidecar cannot be used standalone.
It must be used with the dapr sidecar and the application.
A docker file is provided to build the sidecar image.

### Configuration Properties
- **APP_PORT**: the port on which the application is running
- **SERVICE_NAME**: the name of the service (as registered with dapr)
- **HEARTBEAT_INTERVAL**: the interval at which the sidecar sends a heartbeat to the experiment configuration service

> [!NOTE]
> Running the service locally through the IDE is neither recommended nor supported.

## License
MIT