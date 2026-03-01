using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZetTechAvio1._0.Models
{
    [Table("Tickets")]
    public class Ticket
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int FlightId { get; set; }

        [Required]
        public int FareId { get; set; }

        public int? SeatId { get; set; }

        [Required]
        [StringLength(20)]
        public string TicketNumber { get; set; } = "";

        [Required]
        [StringLength(255)]
        public string PassengerName { get; set; } = "";

        [Required]
        public PassengerType PassengerType { get; set; } = PassengerType.Adult;

        [StringLength(4)]
        public string PassportSeries { get; set; } = "";

        [StringLength(6)]
        public string PassportNumber { get; set; } = "";

        public DateTime? PassengerDob { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        public TicketStatus Status { get; set; } = TicketStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public Booking? Booking { get; set; }
        public Flight? Flight { get; set; }
        public Fare? Fare { get; set; }
        public Seat? Seat { get; set; }
    }

    public enum PassengerType
    {
        Adult,
        Child,
        Infant
    }

    public enum TicketStatus
    {
        Active,
        Cancelled,
        Used
    }
}
