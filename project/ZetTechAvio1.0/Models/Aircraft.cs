using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZetTechAvio1._0.Models
{
    [Table("Aircraft")]
    public class Aircraft
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Model { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Manufacturer { get; set; } = "";

        [Required]
        public int TotalSeats { get; set;}

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        
    }
}