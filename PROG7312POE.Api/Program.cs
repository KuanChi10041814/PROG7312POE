using Microsoft.EntityFrameworkCore;
using PROG7312POE.Api.Data;
using PROG7312POE.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("SmartXClient", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<SmartXDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("SmartXDatabase")));
builder.Services.AddScoped<SensorService>();
builder.Services.AddScoped<TelemetryService>();
builder.Services.AddScoped<AttachmentService>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SmartXDbContext>();
    dbContext.Database.EnsureCreated();
    EnsurePartOneSchema(dbContext);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("SmartXClient");

app.UseAuthorization();

app.MapControllers();

app.Run();

static void EnsurePartOneSchema(SmartXDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw(
        """
        CREATE TABLE IF NOT EXISTS "TelemetryRecords" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_TelemetryRecords" PRIMARY KEY AUTOINCREMENT,
            "SensorId" INTEGER NOT NULL,
            "MetricName" TEXT NOT NULL,
            "ValueType" TEXT NOT NULL,
            "RawValue" TEXT NOT NULL,
            "NumericValue" REAL NULL,
            "IsAnomaly" INTEGER NOT NULL,
            "Severity" TEXT NOT NULL,
            "RecordedAtUtc" TEXT NOT NULL,
            CONSTRAINT "FK_TelemetryRecords_Sensors_SensorId" FOREIGN KEY ("SensorId") REFERENCES "Sensors" ("Id") ON DELETE CASCADE
        );
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        CREATE TABLE IF NOT EXISTS "SensorAttachments" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_SensorAttachments" PRIMARY KEY AUTOINCREMENT,
            "SensorId" INTEGER NOT NULL,
            "FileName" TEXT NOT NULL,
            "ContentType" TEXT NOT NULL,
            "SizeBytes" INTEGER NOT NULL,
            "StoredPath" TEXT NOT NULL,
            "UploadedAtUtc" TEXT NOT NULL,
            CONSTRAINT "FK_SensorAttachments_Sensors_SensorId" FOREIGN KEY ("SensorId") REFERENCES "Sensors" ("Id") ON DELETE CASCADE
        );
        """);

    AddColumnIfMissing(dbContext, "Sensors", "Status", "TEXT NOT NULL DEFAULT 'Online'");
    dbContext.Database.ExecuteSqlRaw(
        """CREATE INDEX IF NOT EXISTS "IX_TelemetryRecords_RecordedAtUtc" ON "TelemetryRecords" ("RecordedAtUtc");""");
    dbContext.Database.ExecuteSqlRaw(
        """CREATE INDEX IF NOT EXISTS "IX_TelemetryRecords_SensorId" ON "TelemetryRecords" ("SensorId");""");
    dbContext.Database.ExecuteSqlRaw(
        """CREATE INDEX IF NOT EXISTS "IX_SensorAttachments_SensorId" ON "SensorAttachments" ("SensorId");""");
}

static void AddColumnIfMissing(SmartXDbContext dbContext, string tableName, string columnName, string columnDefinition)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State == System.Data.ConnectionState.Closed;
    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }

    var sql = $"""ALTER TABLE "{tableName}" ADD COLUMN "{columnName}" {columnDefinition};""";
    dbContext.Database.ExecuteSqlRaw(sql);
}
