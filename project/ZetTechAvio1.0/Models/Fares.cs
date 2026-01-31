using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZetTechAvio1._0.Models
{
    [Table("Fares")]
    public class Fare
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int FlightId { get; set; }

        [ForeignKey("FlightId")]
        public required Flight Flight { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "RUB";

        //Цена в рублях
        [Required]
        [Range(1000, 99999)]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Price { get; set; }

        [Required]
        public int SeatsAvailable {get; set;}

        public bool BaggageIncluded { get; set; }

        public int BaggageWeightKg { get; set; }

        public bool Refundable { get; set; }

        [Required]
        public Fare_class Class { get; set; } = Fare_class.Economy;
        public enum Fare_class
        {
            Economy,
            Business,
            First
        }
    }
}