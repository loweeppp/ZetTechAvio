using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZetTechAvio1._0.Models
{
    [Table("Seats")]
    public class Seat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int FlightId { get; set; }

        [Required]
        [StringLength(10)]
        public string SeatNumber { get; set; } = "";

        [Required]
        public SeatClass SeatClass { get; set; }

        [Required]
        public SeatStatus Status { get; set; } = SeatStatus.Available;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public Flight? Flight { get; set; }
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }

    public enum SeatClass
    {
        Economy,
        Business,
        First
    }

    public enum SeatStatus
    {
        Available,
        Reserved,
        Booked
    }
}
