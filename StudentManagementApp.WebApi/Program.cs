using System.Text;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentManagement.Core.Services;
using StudentManagement.Core.Interfaces;
using StudentManagement.Infrastructure.Hybrid;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using StudentManagement.Infrastructure.Hybrid.Repositories;

var builder = WebApplication.CreateBuilder(args);



// 1. Get Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Register HybridDbContext
builder.Services.AddDbContext<HybridDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Register All Hybrid Repositories (Scoped to request lifetime)
builder.Services.AddScoped<IStudentRepository, HybridStudentRepository>();
builder.Services.AddScoped<ICourseRepository, HybridCourseRepository>();
builder.Services.AddScoped<IEnrollmentRepository, HybridEnrollmentRepository>();
builder.Services.AddScoped<IFeeRepository, HybridFeeRepository>();
builder.Services.AddScoped<IUserRepository, HybridUserRepository>();

// 4. Register All Core Business Services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IFeeService, FeeService>();
builder.Services.AddScoped<IUserService, UserService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT Secret Key is missing in appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.IncludeErrorDetails = true;
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("access_token", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Token validation failed: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Removes the default 5-minute grace period on expiration
    };
});


builder.Services.AddControllers();

// ADD THIS: Register Swagger Services 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Student Management API",
        Version = "v1"
    });

   // 1.Define the Security Scheme using exact lowercase "bearer" as the key
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",             // Must be lowercase "bearer"
        BearerFormat = "JWT",
        Description = "Input your JWT token directly. Do NOT type 'Bearer ' manually, just paste the token value."
    });

    // 2. Map the requirement cleanly using the EXACT same "bearer" key
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular app local port
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


var app = builder.Build();
//Redirect if someone visit "/" it will redirect to /swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

//Instead of Redirecting you can show message in JSON Format.
//{
//    return Results.Ok(new
//    {
//        Message = "Welcome to Student Managment APIs",
//        Status = "API is running successfully!"
//    });
//});

// Enable Swagger Middleware in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Management API v1");
        c.RoutePrefix = "swagger"; // Serves the UI at root/swagger
    });
}

app.UseRouting();

app.UseHttpsRedirection();

// Enable CORS, Authorization, and Map Routes
app.UseCors("AngularPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();