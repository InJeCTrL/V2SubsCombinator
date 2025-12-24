using Microsoft.AspNetCore.Authentication.JwtBearer;
using V2SubsCombinator.Database;
using V2SubsCombinator.IServices;
using V2SubsCombinator.Services;
using V2SubsCombinator.Utils;


var builder = WebApplication.CreateBuilder(args);

var jwtHelper = new JWTHelper(builder.Configuration);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = jwtHelper.GetValidationParameters();
});

builder.Services.AddSingleton(jwtHelper);
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IAuthentication, Authentication>();
builder.Services.AddScoped<ISubscription, Subscription>();

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
