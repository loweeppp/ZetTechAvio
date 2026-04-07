using System;
using System.ComponentModel.DataAnnotations;

namespace ZetTechAvio1._0.Models
{
    public class CreateBookingRequest
    {
        [Required]
        public int FlightId { get; set; }

        [Required]
        public int FareId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; }

        [Required]
        public List<PassengerInfo> Passengers { get; set; } = new();
    }

    public class PassengerInfo
    {
        [Required]
        [StringLength(255)]
        public string FullName { get; set; } = "";

        [Required]
        public string PassengerType { get; set; } = "Adult"; // Adult, Child, Infant

        [StringLength(4)]
        public string PassportSeries { get; set; } = "";

        [StringLength(6)]
        public string PassportNumber { get; set; } = "";
    }

    public class BookingResponse
    {
        public int Id { get; set; }
        public string BookingReference { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public List<TicketResponse> Tickets { get; set; } = new();
    }

    public class TicketResponse
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = "";
        public string PassengerName { get; set; } = "";
        public decimal Price { get; set; }
        public string Status { get; set; } = "";
        public int FlightId { get; set; }
        public FlightResponse? Flight { get; set; }
    }

    public class FlightResponse
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; } = "";
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public TimeSpan Duration { get; set; }
        public AirportResponse? DepartureAirport { get; set; }
        public AirportResponse? ArrivalAirport { get; set; }
    }

    public class AirportResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string City { get; set; } = "";
    }
}
