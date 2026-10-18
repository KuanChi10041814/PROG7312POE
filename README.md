# Smart-X IoT Mesh Gateway - Parts 1 and 2

This repository contains the Smart-X IoT Mesh Ecosystem POE implementation. It includes a .NET Web API backend, a Blazor WebAssembly client, SQLite persistence, and shared Core models for sensor registration, telemetry ingestion, validation, command streaming, and dashboard engagement.

## Solution Structure

- `PROG7312POE.Core` - shared domain models, DTOs, generic telemetry packets, overloaded sensor readings, recursive deployment validation, telemetry buffers, and command contracts.
- `PROG7312POE.Api` - ASP.NET Core API with SQLite persistence, telemetry endpoints, sensor endpoints, attachment upload, command stream processing, and recommendation logic.
- `PROG7312POE.Client` - Blazor WebAssembly dashboard for the startup menu, sensor registration, telemetry simulation, anomaly triage, command stream, search, undo, and suggested actions.
- `PROG7312POE.Tests` - unit test project.

## Part 1 Features

- Startup interface with the three required architectural pillars.
- Sensor Data Ingestion and Telemetry is implemented.
- Sensor registration captures device name, MAC/unique identifier, deployment path, and category.
- API-backed telemetry ingestion supports `double`, `int`, `bool`, and `string` values.
- `TelemetryPacket<T>` handles mixed telemetry types without boxing into a single object pipeline.
- `PowerReading` overloads `+`, `-`, `>`, and `<` for direct load aggregation and comparison.
- `TelemetryBatchBuffer` stages raw telemetry in jagged arrays before moving records into `List<T>`.
- `DeploymentNode.ValidateRecursively` validates nested deployment paths.
- Sensor profile attachments support configuration files, deployment photos, and hardware logs through multipart upload.
- Dynamic engagement feature displays anomaly triage counts, status cards, severity highlights, and troubleshooting prompts.

## Part 2 Features

- Real-Time Command Stream and History is implemented as an active module.
- `Queue<T>` separates standard FIFO telemetry packets.
- `PriorityQueue<TElement, TPriority>` surfaces critical and warning telemetry ahead of normal packets.
- `Stack<T>` powers command history and undo behavior.
- `Dictionary<TKey, TValue>` supports fast device lookup by MAC address and ID.
- `SortedDictionary<TKey, TValue>` stores telemetry timeline buckets in timestamp order.
- `HashSet<T>` tracks unique active error states and prevents duplicate alert labels.
- Search and filtering can find devices by category, status, alert state, and telemetry metric.
- Suggested Actions use recent anomalies, command history, and search context to recommend manual overrides.

## Prerequisites

- .NET SDK 8.0 or newer.
- A browser that can run Blazor WebAssembly.

## Restore and Build

```powershell
dotnet restore PROG7312POE.sln
dotnet build PROG7312POE.sln
```

## Run the Backend API

Open a terminal in the repository root and run:

```powershell
dotnet run --project PROG7312POE.Api
```

The API uses SQLite with the connection string in `PROG7312POE.Api/appsettings.json`.

Swagger is available in Development mode at:

```text
http://localhost:5026/swagger
```

If your local API launches on a different port, update `PROG7312POE.Client/wwwroot/appsettings.json`.

## Run the Blazor Client

Open a second terminal in the repository root and run:

```powershell
dotnet run --project PROG7312POE.Client
http://localhost:5200
```

Then open the client URL printed in the terminal.

## Demo Flow

1. Start the API.
2. Start the Blazor client.
3. Open the home page and confirm Part 1 and Part 2 are active while Mesh Routing is reserved for the final POE.
4. Go to Register Sensor and create a sensor, for example:
   - Device name: `ESP32 Hydro Node A`
   - MAC address: `A4:CF:12:8B:44:90`
   - Deployment path: `Facility A > Zone 1 > Sub-Zone B`
   - Category: `Environmental`
5. Go to Telemetry and push readings:
   - `temperature`, `double`, `24.5`
   - `moisture`, `double`, `14.2` to trigger a critical anomaly
   - `valve-open`, `bool`, `false` to trigger a warning
6. Select the sensor and upload a configuration file, photo, or hardware log.
7. Open Command Stream.
8. Search for `Environmental`, `Critical`, `moisture`, or a device name.
9. Review the priority queue, FIFO queue, active error set, sorted timeline, and suggested actions.
10. Issue a manual override command, then use Undo Last to demonstrate the command stack.

## Tests

```powershell
dotnet test PROG7312POE.sln
```

## Submission Notes

- Push the full source directory to GitHub.
- Keep regular commits. The rubric expects a strong GitHub history, ideally 20-25+ meaningful commits.
- Include the Part 1 research Word document separately with proper referencing.
- Include the Part 2 demo video or presentation if instructed by your lecturer.
