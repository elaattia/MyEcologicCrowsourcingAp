using MyEcologicCrowsourcingApp.Data;
using MyEcologicCrowsourcingApp.Controllers;
using MyEcologicCrowsourcingApp.Models;
using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Services;
using MyEcologicCrowsourcingApp.Services.Interfaces;
using MyEcologicCrowsourcingApp.Repositories;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("GeminiSettings"));

// IMPORTANT: Désactiver le mapping automatique des claims JWT
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Configuration JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

if (jwtSettings == null)
{
    throw new InvalidOperationException("JwtSettings configuration is missing");
}

builder.Services.Configure<RoboflowSettings>(
    builder.Configuration.GetSection("RoboflowSettings")
);

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddDbContext<EcologicDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IOrganisationRepository, OrganisationRepository>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();

builder.Services.AddScoped<IVehiculeRepository, VehiculeRepository>();
builder.Services.AddScoped<IVehiculeService, VehiculeService>();

builder.Services.AddScoped<IDepotRepository, DepotRepository>();
builder.Services.AddScoped<IDepotService, DepotService>();

builder.Services.AddScoped<IForumCategoryRepository, ForumCategoryRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IPostReactionRepository, PostReactionRepository>();
builder.Services.AddScoped<ICommentReactionRepository, CommentReactionRepository>();
builder.Services.AddScoped<IPostReportRepository, PostReportRepository>();

builder.Services.AddScoped<IForumCategoryService, ForumCategoryService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IReactionService, ReactionService>();
builder.Services.AddScoped<IReportService, ReportService>();


builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IGeminiAIService, GeminiAIService>();

builder.Services.AddScoped<IAchievementRepository, AchievementRepository>();
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();
builder.Services.AddScoped<IChallengeSubmissionRepository, ChallengeSubmissionRepository>();
builder.Services.AddScoped<IChallengeTemplateRepository, ChallengeTemplateRepository>();
builder.Services.AddScoped<ISubmissionVoteRepository, SubmissionVoteRepository>();
builder.Services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();
builder.Services.AddScoped<IUserChallengeRepository, UserChallengeRepository>();
builder.Services.AddScoped<IUserStatsRepository, UserStatsRepository>();

builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IChallengeSubmissionRepository, ChallengeSubmissionRepository>();
builder.Services.AddScoped<ISubmissionVoteRepository, SubmissionVoteRepository>();

builder.Services.AddScoped<IUserStatsService, UserStatsService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();

builder.Services.AddHttpClient("Roboflow", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient(); // Pour les appels OSRM
builder.Services.AddScoped<VRPOptimisationService>();

builder.Services.AddScoped<GeminiSemanticKernelAgent>();

builder.Services.AddSingleton<BackgroundRecommendationService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<BackgroundRecommendationService>());

builder.Services.AddMemoryCache();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("Authentification échouée: {Message}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Token validé avec succès");

            var claims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}");
            if (claims != null)
            {
                logger.LogInformation("Claims: {Claims}", string.Join(", ", claims));
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.AddHttpClient("GoogleProvider", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MyEcologicApp/1.0");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();