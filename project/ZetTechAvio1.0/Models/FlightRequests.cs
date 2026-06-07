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

        public List<FareClassRequest> FareClasses { get; set; } = new();
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
        public required List<int> Weekdays { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public string Status { get; set; } = "Scheduled";

        public List<FareClassRequest> FareClasses { get; set; } = new();
    }

    public sealed class FareClassRequest
    {
        [Required]
        public required string ClassType { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        [Range(0, 999999)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 499)]
        public int Seats { get; set; }

        public string? Baggage { get; set; }
    }
}
