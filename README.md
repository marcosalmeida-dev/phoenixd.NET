
# phoenixd.NET

## Project Overview

**phoenixd.NET** is a .NET 8 implementation of the Phoenixd Bitcoin/Lightning service provided by [ACINQ](https://acinq.co) (specifically for the [Phoenix Server](https://phoenix.acinq.co/server)). This project leverages **SignalR** to handle real-time client notifications for payment statuses, enabling easy integration with Blazor-based applications. It aims to streamline communication between the .NET backend and clients using SignalR hubs for seamless payment tracking.

## Features
- Real-time payment notifications using SignalR.
- Supports the [Phoenixd](https://phoenix.acinq.co/server) web service for Bitcoin/Lightning payments.
- Example integration with Blazor client for handling payment status updates.
- Docker support for easy deployment.

## Prerequisites
- .NET 8 SDK
- Docker
- Phoenixd server from ACINQ running in a Docker instance

## Setup

### 1. Phoenixd Web Service Client Configuration
Configure your application to connect to the Phoenixd web service by including the following in your `appsettings.json`:

```json
"PhoenixConfig": {
  "Token": "(Get this token by running the phoenixd Docker instance file path: phoenix/.phoenix/phonix.conf -> http-password)",
  "Host": "http://localhost:9741",
  "Username": "phoenix"
}
```

### 2. Backend Service Configuration

To set up the backend with Phoenixd services in your .NET 8 project, configure the `Program.cs` or `Startup.cs` files as follows:

```csharp
using Phoenixd.NET;
using Phoenixd.NET.Core.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.ConfigurePhoenixdServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();
app.UseAntiforgery();

app.MapHub<PaymentHub>("/paymentHub"); // Register the SignalR Hub endpoint

app.Run();
```

This code snippet demonstrates how to integrate the Phoenixd services and SignalR hub into your .NET 8 backend, enabling client notifications for payment events.

### 3. Blazor Client Integration

For a Blazor client, the following example shows how to set up a SignalR connection and handle incoming payment notifications:

```csharp
// Initialize the SignalR connection
_hubConnection = new HubConnectionBuilder()
    .WithUrl(Navigation.ToAbsoluteUri("/paymentHub")) // Adjust the URL to match your server setup
    .WithAutomaticReconnect() // Automatically reconnect if the connection is lost
    .Build();

// Handle receiving the connection ID
_hubConnection.On<string>("ReceiveConnectionId", (id) =>
{
    connectionId = id; // Store the connection ID to be used as a parameter for payment API calls
});

// Handle receiving payment information
_hubConnection.On<PaymentReceived>("ReceivePayment", async (payment) =>
{
    if (payment.PaymentHash != null && paymentDialogReference != null)
    {
        await InvokeAsync(() =>
        {
            // Handle the payment result
        });
    }
});
```

### 4. Running in Docker

The project can be built and run using Docker. The Dockerfile for the phoenixd.NET web service client is located at the following path:

[Dockerfile](https://github.com/marcosalmeida-dev/phoenixd.NET/blob/main/Phoenixd.NET.WebServiceClient/.docker/phoenixd/Dockerfile)

To run the service, use the following commands:

```bash
docker build -t phoenixd-net-service -f .docker/phoenixd/Dockerfile .
docker run -d -p 9741:9741 --name phoenixd phoenixd-net-service
```

Make sure to replace the token in the settings file after you obtain it from the running Phoenixd Docker instance.

### 5. SignalR Hub for Payment Notifications

The SignalR hub named `PaymentHub` allows clients to receive payment notifications in real-time. Make sure your frontend clients are connected to `/paymentHub` for proper integration.

## Additional Information

- **Phoenixd** is a server-side solution for managing Bitcoin and Lightning payments using ACINQ's [Phoenix app](https://phoenix.acinq.co/app).
- **SignalR** is used for real-time communication between the .NET backend and clients.
- The project is designed to work seamlessly with **Blazor** and can be extended to other frontend frameworks.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Feel free to open issues and submit pull requests to improve the project.

---

Thanks for using **phoenixd.NET**! If you have any questions or suggestions, don't hesitate to reach out.
