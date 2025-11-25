using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZetTechAvio1._0.Models
{
    [Table("Airports")]
    public class Airport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(3)]
        public string Iata { get; set; } = "";

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(255)]
        public string City { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = "";


        public double Lat { get; set; }
        public double Lon { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}