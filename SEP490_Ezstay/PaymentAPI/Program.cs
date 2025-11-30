using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using PaymentAPI.Config;
using PaymentAPI.Mapping;
using PaymentAPI.Repository.Interface;
using PaymentAPI.Repository;
using PaymentAPI.Services;
using PaymentAPI.Services.Interfaces;
using Shared.DTOs.Payments.Responses;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var mongoClient = new MongoClient(builder.Configuration["ConnectionStrings:ConnectionString"]);
builder.Services.AddSingleton( mongoClient.GetDatabase(builder.Configuration["ConnectionStrings:DatabaseName"]));

builder.Services.Configure<VnPayConfig>(builder.Configuration.GetSection("SePay"));
builder.Services.Configure<SePayConfig>(builder.Configuration.GetSection("SePay"));

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();

builder.Services.AddScoped<IBankGatewayRepository, BankGatewayRepository>();
builder.Services.AddScoped<IBankGatewayService, BankGatewayService>();
builder.Services.AddHttpClient<IBankGatewayService, BankGatewayService>();

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddHttpClient<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<IPaymentHistoryRepository, PaymentHistoryRepository>();

builder.Services.AddHttpClient<ISePayService, SePayService>();
builder.Services.AddScoped<ISePayService, SePayService>();

// Utility Bill Service (HTTP Client to UtilityBillAPI) - OPTIONAL
var utilityBillApiUrl = builder.Configuration["ServiceUrls:UtilityBillAPI"];
if (!string.IsNullOrEmpty(utilityBillApiUrl) && Uri.TryCreate(utilityBillApiUrl, UriKind.Absolute, out _))
{
    builder.Services.AddHttpClient<IUtilityBillService, UtilityBillService>((serviceProvider, client) =>
    {
        client.BaseAddress = new Uri(utilityBillApiUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        return handler;
    });
}
else
{
    // Register null service when UtilityBillAPI is not configured
    builder.Services.AddScoped<IUtilityBillService>(sp => null!);
}



builder.Services.AddAutoMapper(typeof(BankAccountMappingProfile));
builder.Services.AddAutoMapper(typeof(PaymentMappingProfile));

var odatabuilder = new ODataConventionModelBuilder();
odatabuilder.EntitySet<BankGatewayResponse>("BankGateway");
odatabuilder.EntitySet<BankAccountResponse>("BankAccount");
var odata = odatabuilder.GetEdmModel();
builder.Services.AddControllers().AddOData(options =>
    options.AddRouteComponents("odata", odata)
        .SetMaxTop(100)
        .Count()
        .Filter()
        .OrderBy()
        .Expand()
        .Select());

// builder.Services.AddHttpClient<ITenantClientService, TenantClientService>(client =>
// {
//     client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:TenantAPI"]);
// });

var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
        };
    });



builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PaymentAPI", Version = "v1" });

    // Thêm JWT Security
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"Nhập vào JWT token theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Cho phép nhận string -> enum và khi trả ra thì enum thành string
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});


// Add CORS for frontend (Next.js) - Allow all origins for payment API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Allow all origins for API
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    
    // Alternative: Specific origins policy
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "https://ezstay-fe-project.vercel.app",
                "https://ezstay-fe.vercel.app",
                "https://payment-api-r4zy.onrender.com"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();