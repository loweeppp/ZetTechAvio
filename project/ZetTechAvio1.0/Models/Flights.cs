using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZetTechAvio1._0.Models
{
    [Table("Flights")]
    public class Flight
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public ICollection<Fare> Fares { get; set; } = new List<Fare>();
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

        [Required]
        [StringLength(50)]
        public string FlightNumber { get; set; } = "";

        [Required]
        public int AirlineId { get; set; }

        [Required]
        public int AircraftId { get; set; }

        [Required]
        public int OriginAirportId { get; set; }

        [Required]
        public int DestAirportId { get; set; }

        [Required]
        public DateTime DepartureDt { get; set; }
        [Required]
        public DateTime ArrivalDt { get; set; }
        [Required]
        public int DurationMinutes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public Airline Airline { get; set; } = null!;
        public Aircraft Aircraft { get; set; } = null!;
        public Airport OriginAirport { get; set; } = null!;
        public Airport DestAirport { get; set; } = null!;
        public FlightStatus Status { get; set; } = FlightStatus.Scheduled;


    }

    public enum FlightStatus
    {
        Scheduled,
        Delayed,
        Cancelled,
        Completed
    }

}