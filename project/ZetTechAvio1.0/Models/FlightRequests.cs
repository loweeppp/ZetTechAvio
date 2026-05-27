using System.ComponentModel.DataAnnotations;

namespace ZetTechAvio1._0.Models
{
    public sealed class FlightCommandRequest
    {
        [StringLength(50)]
        public string? FlightNumber { get; set; }

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

        [Required]
        public string Status { get; set; } = "Scheduled";
    }

    public sealed class FlightScheduleRequest
    {
        [StringLength(50)]
        public string? FlightNumber { get; set; }

        [Required]
        public int AirlineId { get; set; }

        [Required]
        public int AircraftId { get; set; }

        [Required]
        public int OriginAirportId { get; set; }

        [Required]
        public int DestAirportId { get; set; }

        [Required]
        public required string DepartureTime { get; set; }

        [Required]
        public required string ArrivalTime { get; set; }

        [Required]
        public required List<string> Weekdays { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public string Status { get; set; } = "Scheduled";
    }
}
