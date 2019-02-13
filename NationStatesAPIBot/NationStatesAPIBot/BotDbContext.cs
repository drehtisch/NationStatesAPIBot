﻿using Microsoft.EntityFrameworkCore;
using NationStatesAPIBot.Entities;
using System.IO;
using System.Linq;

namespace NationStatesAPIBot
{
    public class BotDbContext : DbContext
    {
        public DbSet<Nation> Nations { get; set; }
        public DbSet<NationStatus> NationStatuses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL(GetConnectionString());
        }

        private string GetConnectionString()
        {
            if (File.Exists("keys.config"))
            {
                var lines = File.ReadAllLines("keys.config").ToList();
                if (lines.Exists(cl => cl.StartsWith("dbConnection=")))
                {
                    var line = lines.FirstOrDefault(cl => cl.StartsWith("dbConnection="));
                    var dbString = line.Remove(0, "dbConnection=".Length);
                    return dbString;
                }
                else
                {
                    throw new InvalidDataException("The 'keys.config' does not contain a dbConnection string");
                }
            }
            else
            {
                throw new FileNotFoundException("The 'keys.config' file could not be found.");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Many to Many User <-> Permission
            modelBuilder.Entity<UserPermissions>()
                .HasKey(up => new { up.UserId, up.PermissionId });

            modelBuilder.Entity<UserPermissions>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserPermissions>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId);

            //Many to Many Role <-> Permission
            modelBuilder.Entity<RolePermissions>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermissions>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermissions>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            //Many to Many User <-> Role
            modelBuilder.Entity<UserRoles>()
                .HasKey(ur => new { ur.RoleId, ur.UserId });

            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.Roles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(ur => ur.RoleId);
        }
    }
}
