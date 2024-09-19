
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

### 6. Backend Controller and Blazor Client Sample for Payments

The following code demonstrates a simple payment management controller and a Blazor client request sample to process Lightning payments.

#### Backend Controller Sample

```csharp
[Route("api/payment-manager")]
public class PaymentManagerController : Controller
{
    private readonly PhoenixdManagerService _phoenixdManagerService;

    public PaymentManagerController(PhoenixdManagerService phoenixdManagerService)
    {
        _phoenixdManagerService = phoenixdManagerService;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var balance = await _phoenixdManagerService.NodeService.GetBalance();
        return Ok(balance);
    }

    [HttpGet("node-info")]
    public async Task<IActionResult> GetNodeInfo()
    {
        var nodeInfo = await _phoenixdManagerService.NodeService.GetNodeInfo();
        return Ok(nodeInfo);
    }

    [HttpPost("receive-payment")]
    public async Task<IActionResult> ReceiveLightningPayment([FromBody] ReceiveLightningPaymentRequest receiveLightningPaymentRequest)
    {
        var invoice = await _phoenixdManagerService.PaymentService.ReceiveLightningPaymentAsync(
            receiveLightningPaymentRequest.Description, 
            receiveLightningPaymentRequest.AmountSat, 
            receiveLightningPaymentRequest.ExternalId
        );
        return Ok(invoice);
    }
}
```

#### Blazor Client Side Request Payment Sample Using QRCoder

```csharp




//QRCoder component
@using QRCoder

@if (!string.IsNullOrEmpty(Data))
{
    <div class="tab-pane payment-box" id="link-tab" role="tabpanel">
        <div class="qr-container" data-clipboard="12">
            <img style="image-rendering:pixelated;image-rendering:-moz-crisp-edges;min-width:@(Size)px;min-height:@(Size)px" src="@GetQRCodeData(Data, Amount)" class="qr-code" />
            <img src="/img/..." class="qr-icon" alt="address-type-icon" />
        </div>
    </div>
}

@code {
    [Parameter]
    public string Data { get; set; }

    [Parameter]
    public decimal? Amount { get; set; }

    [Parameter]
    public int Size { get; set; } = 256;

    private string GetQRCodeData(string data, decimal? amount)
    {
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode($"{data}?amount={amount}", QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var bytes = qrCode.GetGraphic(5, new byte[] { 0, 0, 0, 255 }, new byte[] { 0xf5, 0xf5, 0xf7, 255 });
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}


//Blazor client sample
<QRCode Data="@addressValue" Amount="@addressAmountValue"></QRCode>

@code {
    private async Task SendLightningPaymentRequest()
    {
        try
        {
            model.ExternalId = connectionId;

            var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:44379/") };
            var response = await httpClient.PostAsJsonAsync("api/payment-manager/receive-payment", model);

            if (response.IsSuccessStatusCode)
            {
                var invoice = await response.Content.ReadFromJsonAsync<Invoice>();
                addressValue = invoice.Serialized;
                addressAmountValue = invoice.AmountSat;
            }
        }
        catch(Exception ex)
        {
            //Handle error...
        }

        StateHasChanged();
    }
}
```

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
