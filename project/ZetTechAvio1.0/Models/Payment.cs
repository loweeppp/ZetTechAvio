using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZetTechAvio1._0.Models
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        // Идентификатор платежа в Яндекс.Кассе
        [Required]
        public string YooKassaPaymentId { get; set; } = "";

        // Сумма платежа
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        // Статус: pending, succeeded, failed, canceled
        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        // Ссылка на форму оплаты Яндекс.Касса
        [StringLength(500)]
        public string? ConfirmationUrl { get; set; }

        // Метод оплаты: card, apple_pay, google_pay и т.д.
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        // Дополнительное описание или комментарий к платежу
        [StringLength(255)]
        public string? Discription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public Booking? Booking { get; set; }

    public enum PaymentStatus
    {
        Pending,
        Succeeded,
        Failed,
        Canceled
    }
}
}
