using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZetTechAvio1._0.Models
{
    [Table("Airlines")]
    public class Airline
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(3)]
        public string IataCode { get; set; }="";

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = "";

        [StringLength(500)]
        public string LogoUrl { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    }
}