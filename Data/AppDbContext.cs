using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeCapsule.API.Controllers;
using TimeCapsule.API.Entities;

namespace TimeCapsule.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {        
        }
        public DbSet<CapsuleEntity> TimeCapsules {get; set;}
        public DbSet<User> Users { get; set; }
        public DbSet<UserSpotifyToken> UserSpotifyTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CapsuleEntity>().HasOne(c=>c.User)
            .WithMany(u=>u.TimeCapsules)
            .HasForeignKey(c=>c.UserID)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>().HasOne(u=>u.SpotifyToken)
            .WithOne(s=>s.User)
            .HasForeignKey<UserSpotifyToken>(s=>s.UserId);

            base.OnModelCreating(modelBuilder);
        }
    }
}