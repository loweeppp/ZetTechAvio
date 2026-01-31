using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ZetTechAvio1._0.Models;

public class FlightDto
{
    public int Id { get; set; }
    public required string FlightNumber { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime DepartureDt { get; set; }
    public DateTime ArrivalDt { get; set; }
    public decimal MinPrice { get; set; }  // ← Минимальная цена
    public required Airport OriginAirport { get; set; }
    public required Airport DestAirport { get; set; }
}