using AtelierApp.Models;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace AtelierApp.Data
{
    internal class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Authorizations> Authorizations { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<OrderService> OrderServices { get; set; }
        public DbSet<Materials> Materials { get; set; }
        public DbSet<MaterialCategories> MaterialCategories { get; set; }
        public DbSet<Measurement> Measurements { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<UsedMaterial> UsedMaterials { get; set; }
        public DbSet<OrderEmployee> OrderEmployees { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                try
                {
                    // Загружаем .env файл
                    LoadEnvFile();

                    // Получаем строку подключения
                    var connectionString = GetConnectionString();

                    // Используем Npgsql
                    optionsBuilder.UseNpgsql(connectionString);

                    // Опционально: включаем логирование для отладки
                    // optionsBuilder.LogTo(Console.WriteLine);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Ошибка конфигурации БД: {ex.Message}", ex);
                }
            }
        }

        private void LoadEnvFile()
        {
            // Пути для поиска .env файла
            string[] possiblePaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".env")
            };

            foreach (var path in possiblePaths)
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    Env.Load(fullPath);
                    System.Diagnostics.Debug.WriteLine($".env загружен из: {fullPath}");
                    return;
                }
            }

            throw new FileNotFoundException(
                "Файл .env не найден. Убедитесь, что он есть и установлен Copy to Output Directory = Copy always");
        }

        private string GetConnectionString()
        {
            var host = Env.GetString("DB_HOST");
            var port = Env.GetString("DB_PORT");
            var database = Env.GetString("DB_NAME");
            var username = Env.GetString("DB_USER");
            var password = Env.GetString("DB_PASSWORD");

            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("DB_PASSWORD не найдена в .env файле");
            }

            return $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Уникальные индексы
            modelBuilder.Entity<Authorizations>()
                .HasIndex(a => a.Login)
                .IsUnique();

            modelBuilder.Entity<Position>()
                .HasIndex(p => p.Name)
                .IsUnique();

            modelBuilder.Entity<Status>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<Service>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<Materials>()
                .HasIndex(m => m.Article)
                .IsUnique();

            // Настройка связей многие-ко-многим
            modelBuilder.Entity<OrderService>()
                .HasKey(os => os.ID);

            modelBuilder.Entity<OrderService>()
                .HasIndex(os => new { os.OrderID, os.ServiceID })
                .IsUnique();

            modelBuilder.Entity<UsedMaterial>()
                .HasKey(um => um.ID);

            modelBuilder.Entity<OrderEmployee>()
                .HasKey(oe => oe.ID);

            modelBuilder.Entity<OrderEmployee>()
                .HasIndex(oe => new { oe.OrderID, oe.EmployeeID })
                .IsUnique();
        }
    }
}
