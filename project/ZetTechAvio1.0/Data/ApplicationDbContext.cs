using Microsoft.EntityFrameworkCore;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Airport> Airports { get; set; }
        public DbSet<Airline> Airlines { get; set; }
        public DbSet<Aircraft> Aircrafts { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Fare> Fares { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Seat> Seats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


//          -- ============================================
//          -- 1. Пользователи
//          -- ============================================
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("idx_email");

                entity.HasIndex(e => e.Role)
                    .HasDatabaseName("idx_role");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Phone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAt);

                entity.Property(e => e.UpdatedAt);

                entity.HasIndex(e => e.Email).HasDatabaseName("idx_email");
                entity.HasIndex(e => e.Role).HasDatabaseName("idx_role");
            });

//          -- ============================================
//          -- 2. Авиалинии
//          -- ============================================
            modelBuilder.Entity<Airline>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.IataCode)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.LogoUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt);

                entity.HasIndex(e => e.IataCode)
                .IsUnique()
                .HasDatabaseName("idx_iata");
                
            });


//          -- ============================================
//          -- 3. Аэропорты
//          -- ============================================
            modelBuilder.Entity<Airport>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Iata)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.City)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Country)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Lat);

                entity.Property(e => e.Lon);

                entity.Property(e => e.CreatedAt);

                entity.HasIndex(e => e.Iata).HasDatabaseName("idx_iata");
                entity.HasIndex(e => e.City).HasDatabaseName("idx_city");
                entity.HasIndex(e => e.Country).HasDatabaseName("idx_country");
            });


//          -- ============================================
//          -- 4. Самолеты
//          -- ============================================
            modelBuilder.Entity<Aircraft>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Model)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Manufacturer)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TotalSeats)
                    .IsRequired();

                entity.Property(e => e.CreatedAt);

                entity.HasIndex(e => e.Model).HasDatabaseName("idx_model");
            });


//          -- ============================================
//          -- 5. Рейсы
//          -- ============================================
            modelBuilder.Entity<Flight>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FlightNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.AirlineId)
                    .IsRequired();

                entity.Property(e => e.AircraftId)
                    .IsRequired();

                entity.Property(e => e.OriginAirportId)
                    .IsRequired();

                entity.Property(e => e.DestAirportId)
                    .IsRequired();

                entity.Property(e => e.DepartureDt)
                    .IsRequired();

                entity.Property(e => e.ArrivalDt)
                    .IsRequired();

                entity.Property(e => e.DurationMinutes)
                    .IsRequired();

                entity.Property(e => e.CreatedAt);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<string>();

                entity.HasOne(f => f.Airline)
                    .WithMany()
                    .HasForeignKey(f => f.AirlineId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Aircraft)
                    .WithMany()
                    .HasForeignKey(f => f.AircraftId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.OriginAirport)
                    .WithMany()
                    .HasForeignKey(f => f.OriginAirportId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.DestAirport)
                    .WithMany()
                    .HasForeignKey(f => f.DestAirportId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasIndex(e => e.FlightNumber).HasDatabaseName("idx_flight_number");
                entity.HasIndex(e => e.DepartureDt).HasDatabaseName("idx_departure");
                entity.HasIndex(e => new { e.OriginAirportId, e.DestAirportId }).HasDatabaseName("idx_route");
                entity.HasIndex(e => e.AirlineId).HasDatabaseName("idx_airline");
                entity.HasIndex(e => e.Status).HasDatabaseName("idx_status");



            });

//          -- ============================================
//          -- 6. Тарифы        
//          -- ============================================
            modelBuilder.Entity<Fare>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FlightId)
                    .IsRequired();

                entity.Property(e => e.Currency)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(e => e.Price)
                    .IsRequired()
                    .HasColumnType("decimal(12,2)");

                entity.Property(e => e.SeatsAvailable)
                    .IsRequired();

                entity.Property(e => e.BaggageIncluded);

                entity.Property(e => e.BaggageWeightKg);

                entity.Property(e => e.Refundable);

                entity.Property(e => e.Class)
                    .IsRequired()
                    .HasConversion<string>();

                entity.HasOne(f => f.Flight)
                    .WithMany(fl => fl.Fares)
                    .HasForeignKey(f => f.FlightId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.FlightId).HasDatabaseName("idx_flight_id");
                entity.HasIndex(e => e.Class).HasDatabaseName("idx_class");
            });

//          -- ============================================
//          -- 7. Бронирования
//          -- ============================================
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.BookingReference).IsRequired().HasMaxLength(10);
                entity.Property(e => e.TotalAmount).IsRequired().HasColumnType("decimal(10,2)");
                entity.Property(e => e.Status).IsRequired().HasConversion<string>();
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);

                entity.HasOne(b => b.User)
                    .WithMany()
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.UserId).HasDatabaseName("idx_user");
                entity.HasIndex(e => e.BookingReference).IsUnique().HasDatabaseName("idx_booking_ref");
                entity.HasIndex(e => e.Status).HasDatabaseName("idx_status");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_created");
            });

//          -- ============================================
//          -- 8. Места в самолёте
//          -- ============================================
            modelBuilder.Entity<Seat>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FlightId).IsRequired();
                entity.Property(e => e.SeatNumber).IsRequired().HasMaxLength(10);
                entity.Property(e => e.SeatClass).IsRequired().HasConversion<string>();
                entity.Property(e => e.Status).IsRequired().HasConversion<string>();

                entity.HasOne(s => s.Flight)
                    .WithMany()
                    .HasForeignKey(s => s.FlightId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.FlightId).HasDatabaseName("idx_flight");
                entity.HasIndex(e => e.Status).HasDatabaseName("idx_status");
                entity.HasIndex(e => e.SeatClass).HasDatabaseName("idx_class");
                entity.HasIndex(e => new { e.FlightId, e.SeatNumber }).IsUnique().HasDatabaseName("unique_seat_per_flight");
            });

//          -- ============================================
//          -- 9. Билеты
//          -- ============================================
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.BookingId).IsRequired();
                entity.Property(e => e.FlightId).IsRequired();
                entity.Property(e => e.FareId).IsRequired();
                entity.Property(e => e.TicketNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PassengerName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PassengerType).IsRequired().HasConversion<string>();
                entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(10,2)");
                entity.Property(e => e.Status).IsRequired().HasConversion<string>();

                entity.HasOne(t => t.Booking)
                    .WithMany(b => b.Tickets)
                    .HasForeignKey(t => t.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.Flight)
                    .WithMany()
                    .HasForeignKey(t => t.FlightId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Fare)
                    .WithMany()
                    .HasForeignKey(t => t.FareId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Seat)
                    .WithMany(s => s.Tickets)
                    .HasForeignKey(t => t.SeatId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.BookingId).HasDatabaseName("idx_booking");
                entity.HasIndex(e => e.FlightId).HasDatabaseName("idx_flight");
                entity.HasIndex(e => e.TicketNumber).IsUnique().HasDatabaseName("idx_ticket_number");
                entity.HasIndex(e => e.Status).HasDatabaseName("idx_status");
            });
        }
    }
}
