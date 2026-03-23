using Microsoft.EntityFrameworkCore;
using HotelSystem.Models.Entities;

namespace HotelSystem.Data;

public class HotelDbContext : DbContext
{
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<BookingService> BookingServices { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<SystemLog> Logs { get; set; }

    private readonly string? _connectionString;

    public HotelDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Room
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.WaterExpense).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ElectricityExpense).HasColumnType("decimal(18,2)");
            entity.Property(e => e.InternetExpense).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CleaningExpense).HasColumnType("decimal(18,2)");
        });

        // Client
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Passport).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.TotalSpent).HasColumnType("decimal(18,2)");
        });

        // Employee
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Login).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.Salary).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.Login).IsUnique();
        });

        // Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Room)
                  .WithMany(r => r.Bookings)
                  .HasForeignKey(e => e.RoomId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Client)
                  .WithMany(c => c.Bookings)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CreatedBy)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Service
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        // BookingService
        modelBuilder.Entity<BookingService>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Booking)
                  .WithMany(b => b.BookingServices)
                  .HasForeignKey(e => e.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Service)
                  .WithMany(s => s.BookingServices)
                  .HasForeignKey(e => e.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Booking)
                  .WithMany()
                  .HasForeignKey(e => e.BookingId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Room)
                  .WithMany()
                  .HasForeignKey(e => e.RoomId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Service)
                  .WithMany()
                  .HasForeignKey(e => e.ServiceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Expense
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Room)
                  .WithMany()
                  .HasForeignKey(e => e.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SystemLog
        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired();
            entity.HasIndex(e => e.LogDate);
            entity.HasIndex(e => e.Level);
        });

        // Seed Data - Создание администратора по умолчанию
        modelBuilder.Entity<Employee>().HasData(new Employee
        {
            Id = 1,
            FullName = "Администратор",
            Login = "admin",
            PasswordHash = HashPassword("admin123"), // Пароль: admin123
            Role = UserRole.Admin,
            Phone = "+7 (999) 000-00-00",
            Position = "Администратор",
            Salary = 50000,
            IsActive = true,
            CreatedAt = DateTime.Now
        });

        // Seed Data - Номера
        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1, Name = "101", Type = RoomType.Standard, Price = 3000, Status = RoomStatus.Free, Capacity = 2, Description = "Стандартный номер на первом этаже", WaterExpense = 500, ElectricityExpense = 300, InternetExpense = 200, CleaningExpense = 400 },
            new Room { Id = 2, Name = "102", Type = RoomType.Standard, Price = 3500, Status = RoomStatus.Free, Capacity = 2, Description = "Стандартный номер с видом на сад", WaterExpense = 500, ElectricityExpense = 300, InternetExpense = 200, CleaningExpense = 400 },
            new Room { Id = 3, Name = "201", Type = RoomType.Lux, Price = 6000, Status = RoomStatus.Free, Capacity = 4, Description = "Люкс с джакузи", WaterExpense = 800, ElectricityExpense = 500, InternetExpense = 300, CleaningExpense = 600 },
            new Room { Id = 4, Name = "202", Type = RoomType.Lux, Price = 6500, Status = RoomStatus.Free, Capacity = 4, Description = "Люкс с террасой", WaterExpense = 800, ElectricityExpense = 500, InternetExpense = 300, CleaningExpense = 600 },
            new Room { Id = 5, Name = "301", Type = RoomType.Apartments, Price = 10000, Status = RoomStatus.Free, Capacity = 6, Description = "Апартаменты с кухней", WaterExpense = 1200, ElectricityExpense = 800, InternetExpense = 500, CleaningExpense = 1000 }
        );

        // Seed Data - Услуги
        modelBuilder.Entity<Service>().HasData(
            new Service { Id = 1, Name = "Завтрак", Description = "Завтрак \"Шведский стол\"", Price = 500 },
            new Service { Id = 2, Name = "Обед", Description = "Обед в ресторане отеля", Price = 800 },
            new Service { Id = 3, Name = "Ужин", Description = "Ужин в ресторане отеля", Price = 1000 },
            new Service { Id = 4, Name = "Трансфер", Description = "Трансфер из/в аэропорт", Price = 2000 },
            new Service { Id = 5, Name = "Прачечная", Description = "Стирка и глажка белья", Price = 300 },
            new Service { Id = 6, Name = "SPA", Description = "Посещение SPA-центра", Price = 1500 }
        );

        // Seed Data - Клиенты
        modelBuilder.Entity<Client>().HasData(
            new Client { Id = 1, FullName = "Иванов Иван Иванович", Passport = "4500 123456", Phone = "+7 (900) 111-22-33", Email = "ivanov@mail.ru" },
            new Client { Id = 2, FullName = "Петрова Анна Сергеевна", Passport = "4501 654321", Phone = "+7 (900) 222-33-44", Email = "petrova@gmail.com" },
            new Client { Id = 3, FullName = "Сидоров Алексей Дмитриевич", Passport = "4502 111222", Phone = "+7 (900) 333-44-55", Email = "sidorov@yandex.ru" }
        );
    }

    private static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}