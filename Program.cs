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

// ============ CORS Configuration ============
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Your React app's URL
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // If you're using cookies/authentication
    });
});

// ============ Configuration Settings ============
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("GeminiSettings"));
builder.Services.Configure<RoboflowSettings>(builder.Configuration.GetSection("RoboflowSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// IMPORTANT: Désactiver le mapping automatique des claims JWT
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Configuration JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

if (jwtSettings == null)
{
    throw new InvalidOperationException("JwtSettings configuration is missing");
}

// ============ Database Configuration ============
builder.Services.AddDbContext<EcologicDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============ Core Services ============
builder.Services.AddScoped<IJwtService, JwtService>();

// ============ User & Organization Repositories ============
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IOrganisationRepository, OrganisationRepository>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();

builder.Services.AddScoped<IVehiculeRepository, VehiculeRepository>();
builder.Services.AddScoped<IVehiculeService, VehiculeService>();

builder.Services.AddScoped<IDepotRepository, DepotRepository>();
builder.Services.AddScoped<IDepotService, DepotService>();

// ============ FORUM Repositories ============
builder.Services.AddScoped<IForumCategoryRepository, ForumCategoryRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IPostReactionRepository, PostReactionRepository>();
builder.Services.AddScoped<ICommentReactionRepository, CommentReactionRepository>();
builder.Services.AddScoped<IPostReportRepository, PostReportRepository>();

// ============ FORUM Services ============
builder.Services.AddScoped<IForumCategoryService, ForumCategoryService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IReactionService, ReactionService>();
builder.Services.AddScoped<IReportService, ReportService>();
// ⚠️ SI vous avez créé IPostReportService (recommandé), ajoutez :
// builder.Services.AddScoped<IPostReportService, PostReportService>();

// ============ CHALLENGE Repositories ============
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();
builder.Services.AddScoped<IChallengeSubmissionRepository, ChallengeSubmissionRepository>();
builder.Services.AddScoped<IChallengeTemplateRepository, ChallengeTemplateRepository>();
builder.Services.AddScoped<ISubmissionVoteRepository, SubmissionVoteRepository>(); // ✅ DÉJÀ PRÉSENT
builder.Services.AddScoped<IUserChallengeRepository, UserChallengeRepository>();
builder.Services.AddScoped<IUserStatsRepository, UserStatsRepository>();
builder.Services.AddScoped<IAchievementRepository, AchievementRepository>();
builder.Services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();

// ============ CHALLENGE Services ============
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IUserStatsService, UserStatsService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IGeminiAIService, GeminiAIService>();

// ============ HTTP Clients ============
builder.Services.AddHttpClient("Roboflow", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("GoogleProvider", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MyEcologicApp/1.0");
});

builder.Services.AddHttpClient(); // Pour les appels OSRM et autres

// ============ Additional Services ============
builder.Services.AddScoped<VRPOptimisationService>();
builder.Services.AddScoped<GeminiSemanticKernelAgent>();

// ============ Background Services ============
builder.Services.AddSingleton<BackgroundRecommendationService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<BackgroundRecommendationService>());

// ============ Memory Cache ============
builder.Services.AddMemoryCache();

// ============ JWT Authentication ============
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
        ClockSkew = TimeSpan.Zero,
        // ✅ IMPORTANT : Spécifier le claim type pour l'ID utilisateur
        NameClaimType = "nameid" // ou "sub" selon votre configuration
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

// ============ Controllers Configuration ============
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // ✅ Gestion des enums en string
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        
        // ✅ Gestion des références circulaires
        options.JsonSerializerOptions.ReferenceHandler = 
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        
        // ✅ Ignorer les valeurs null
        options.JsonSerializerOptions.DefaultIgnoreCondition = 
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddControllersWithViews();

// ============ BUILD APP ============
var app = builder.Build();

// ============ MIDDLEWARE PIPELINE ============

// ✅ IMPORTANT: CORS doit être AVANT Authentication
app.UseCors("AllowReactApp");

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

// ✅ Authentication APRÈS CORS
app.UseAuthentication(); 
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ============ DATABASE INITIALIZATION (Optional) ============
// Décommenter pour vérifier la connexion à la base de données au démarrage
/*
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<EcologicDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // Vérifier la connexion
        if (context.Database.CanConnect())
        {
            logger.LogInformation("✅ Database connection successful");
            
            // Appliquer les migrations automatiquement (OPTIONNEL)
            // context.Database.Migrate();
        }
        else
        {
            logger.LogWarning("⚠️ Cannot connect to database");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Error during database initialization");
    }
}
*/

app.Run();