Smart-X IoT Mesh Gateway - Part 1

Smart-X IoT Mesh Ecosystem POE. It includes a .NET Web API backend, a Blazor WebAssembly client, and shared Core models for sensor registration, telemetry ingestion, validation, attachments, and dashboard engagement.

Solution Structure

- PROG7312POE.Core - shared domain models, DTOs, generic telemetry packet, operator overload structures, recursive deployment validation, and telemetry batch buffering.
- PROG7312POE.Api - ASP.NET Core API with SQLite persistence, sensor endpoints, telemetry endpoints, anomaly scoring, and multipart file upload support.
- PROG7312POE.Client - Blazor WebAssembly dashboard for the required startup menu, sensor registration, telemetry simulation, attachment uploads, and anomaly triage.
- PROG7312POE.Tests - unit test project.

Part 1 Features

- Startup interface with the three required architectural pillars.
- Sensor Data Ingestion and Telemetry is active for Part 1.
- Sensor registration captures device name, MAC/unique identifier, deployment path, and category.
- API-backed telemetry ingestion supports double, int, bool, and strin` values.
- TelemetryPacket<T> handles mixed telemetry types without boxing into a single object pipeline.
- PowerReading overloads +, -, >, and < for direct load aggregation and comparison.
- TelemetryBatchBuffer stages raw telemetry in jagged arrays before moving records into List<T>.
- DeploymentNode.ValidateRecursively validates nested deployment paths.
- Sensor profile attachments support configuration files, deployment photos, and hardware logs through multipart upload.
- Dynamic engagement feature: the dashboard displays anomaly triage counts, status cards, severity highlights, and troubleshooting prompts.

Prerequisites

- .NET SDK 8.0 or newer.
- A browser that can run Blazor WebAssembly.

Restore and Build

powershell
dotnet restore PROG7312POE.sln
dotnet build PROG7312POE.sln


Run the Backend API

Open a terminal in the repository root and run:

powershell
dotnet run --project PROG7312POE.Api

The API uses SQLite with the connection string in PROG7312POE.Api/appsettings.json.

Swagger is available in Development mode at:

text
http://localhost:5026/swagger


If your local API launches on a different port, update PROG7312POE.Client/wwwroot/appsettings.json.

Run the Blazor Client

Open a second terminal in the repository root and run:

powershell
dotnet run --project PROG7312POE.Client

Then open the client URL printed in the terminal.

Demo Flow

1. Start the API.
2. Start the Blazor client.
3. Open the home page and confirm the three pillars are shown.
4. Go to Register Sensor and create a sensor, for example:
   - Device name: Device1
   - MAC address: XX:XX:XX:XX:XX
   - Deployment path: Path
   - Category: Environmental
5. Go to Telemetry and push readings:
   - temperature, double, 24.5
   - moisture, double, 14.2 to trigger a critical anomaly
   - valve-open, bool, false to trigger a warning
6. Select the sensor and upload a device configuration, photo, or log file.
7. Show the anomaly triage panel and recent telemetry table.

Tests

powershell
dotnet test PROG7312POE.sln
