
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;

namespace MRBSystem.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<MRBAppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MRBSystemConnectionString")));

            builder.Services.AddIdentity<AppUser, AppRole>(options =>
            {
                options.User.RequireUniqueEmail = true; 
            })
.AddEntityFrameworkStores<MRBAppDbContext>()
.AddDefaultTokenProviders();

            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
