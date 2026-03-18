using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZetTechAvio1._0.Models
{
    [Table("ConfirmationCodes")]
    public class ConfirmationCode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }

    [Required]
    [StringLength(255)]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(6)]
    public string Code { get; set; } = "";

    [Required]
    public DateTime Expiration { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
}

