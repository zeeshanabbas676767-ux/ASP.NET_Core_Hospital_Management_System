using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MyMvcApp.Models
{
    public enum PaymentStatus { Pending = 0, Paid = 1, PartiallyPaid = 2, Refunded = 3 }
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal Amount { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        // optional link to appointment
        public int? AppointmentId { get; set; }
        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }

        // invoice belongs to a patient
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        public virtual ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
    }
}
