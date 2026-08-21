using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StudentManagement.AI.Extensions;
using StudentManagement.AI.RAG;
using StudentManagement.AI.Reliability;
using StudentManagement.AI.Sessions;
using StudentManagement.AI.Workflows.Enrollment;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Services;
using StudentManagement.Infrastructure.Hybrid;
using StudentManagement.Infrastructure.Hybrid.Repositories;
using StudentManagementApp.WebApi.ExceptionHandling;
using StudentManagementApp.WebApi.Services;
using StudentManagementApp.WebApi.Sessions;
using StudentManagementApp.WebApi.Workflows;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Get Connection String
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

// 2. Register HybridDbContext
builder.Services.AddDbContextFactory<HybridDbContext>(
    options =>
        options.UseSqlServer(connectionString));

// SQL Server Session
builder.Services.AddScoped<
    ISessionStore,
    SqlServerSessionStore>();

// 3. Register All Hybrid Repositories
builder.Services.AddScoped<
    IStudentRepository,
    HybridStudentRepository>();

builder.Services.AddScoped<
    ICourseRepository,
    HybridCourseRepository>();

builder.Services.AddScoped<
    IEnrollmentRepository,
    HybridEnrollmentRepository>();

builder.Services.AddScoped<
    IFeeRepository,
    HybridFeeRepository>();

builder.Services.AddScoped<
    IUserRepository,
    HybridUserRepository>();

builder.Services.AddScoped<
    IAttendanceRepository,
    HybridAttendanceRepository>();

// 4. Register All Core Business Services
builder.Services.AddScoped<
    IStudentService,
    StudentService>();

builder.Services.AddScoped<
    ICourseService,
    CourseService>();

builder.Services.AddScoped<
    IEnrollmentService,
    EnrollmentService>();

builder.Services.AddScoped<
    IFeeService,
    FeeService>();

builder.Services.AddScoped<
    IUserService,
    UserService>();

builder.Services.AddScoped<
    IAttendanceService,
    AttendanceService>();

// Current User Context
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ICurrentUserContext,
    CurrentUserContext>();

// AI Services
builder.Services.AddStudentManagementAI(
    builder.Configuration);

// SQL Workflow Checkpointing
builder.Services.AddSingleton<
    SqlWorkflowCheckpointStore>();

builder.Services.AddSingleton<CheckpointManager>(
    sp =>
    {
        var store =
            sp.GetRequiredService<
                SqlWorkflowCheckpointStore>();

        return CheckpointManager.CreateJson(
            store);
    });

// Enrollment Workflow Persistence
builder.Services.AddSingleton<
    EnrollmentWorkflowRecordStore>();

builder.Services.AddSingleton<
    IEnrollmentWorkflowRecordStore,
    EnrollmentWorkflowRecordStore>();

builder.Services.AddSingleton<
    IEnrollmentWorkflowHistoryStore,
    EnrollmentWorkflowHistoryStore>();

// Global Exception Handling
builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

// JWT Configuration
var jwtSettings =
    builder.Configuration.GetSection(
        "JwtSettings");

var secretKey =
    jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException(
        "JWT Secret Key is missing in appsettings.json");

builder.Services.AddAuthentication(
    options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(
        options =>
        {
            options.IncludeErrorDetails = true;

            options.Events =
            new JwtBearerEvents
            {
            OnMessageReceived =
                context =>
                {
                    if (context.Request.Cookies
                        .TryGetValue(
                            "access_token",
                            out var token))
                    {
                        context.Token = token;
                    }

                    return Task.CompletedTask;
                },

            OnAuthenticationFailed =
                context =>
                {
                    Console.WriteLine(
                        $"Token validation failed: {context.Exception.Message}");

                    return Task.CompletedTask;
                }
            };

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    jwtSettings["Issuer"],

                ValidAudience =
                    jwtSettings["Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            secretKey)),

                ClockSkew =
                    TimeSpan.Zero
            };
        });

// Controllers + Validation Response
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory =
        context =>
        {
            var errors =
            context.ModelState
            .Where(
                entry =>
                    entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry =>
                    entry.Key,

                entry =>
                entry.Value!
                .Errors
                .Select(
                    error =>
                    string.IsNullOrWhiteSpace(
                        error.ErrorMessage)
                        ? "The supplied value is invalid."
                        : error.ErrorMessage)
                .ToArray());

            var problemDetails =
            new ValidationProblemDetails(
                errors)
            {
                Status =
                    StatusCodes.Status400BadRequest,

                Title =
                    "Validation failed",

                Detail =
                    "One or more validation errors occurred.",

                Instance =
                    context.HttpContext.Request.Path
            };

            return new BadRequestObjectResult(
            problemDetails);
        };
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(
    options =>
    {
        options.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title =
                    "Student Management API",

                Version =
                    "v1"
            });

        options.AddSecurityDefinition(
            "bearer",
            new OpenApiSecurityScheme
            {
                Name =
                    "Authorization",

                In =
                    ParameterLocation.Header,

                Type =
                    SecuritySchemeType.Http,

                Scheme =
                    "bearer",

                BearerFormat =
                    "JWT",

                Description =
                    "Input your JWT token directly. Do NOT type 'Bearer ' manually, just paste the token value."
            });

        options.AddSecurityRequirement(
            document =>
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecuritySchemeReference(
                            "bearer",
                            document)
                    ] = []
                });
    });

// CORS
builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "AngularPolicy",
            policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    });

var app = builder.Build();

// Centralized Exception Handler
app.UseExceptionHandler();

// Redirect root to Swagger
app.MapGet(
    "/",
    () =>
        Results.Redirect(
            "/swagger"));

// Test Endpoint for Current User Context
app.MapGet(
    "/api/_test/current-user",
    (ICurrentUserContext currentUser) =>
    {
        return Results.Ok(
            new
            {
                currentUser.IsAuthenticated,
                currentUser.UserId,
                currentUser.Username,
                currentUser.Email,
                currentUser.Role
            });
    })
    .RequireAuthorization();

// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(
        c =>
        {
            c.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "Student Management API v1");

            c.RoutePrefix =
                "swagger";
        });
}

app.UseRouting();

app.UseHttpsRedirection();

app.UseCors(
    "AngularPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();