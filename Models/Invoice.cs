using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quanlykhachsan.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        public int BookingId { get; set; }

        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }

        public decimal RoomAmount { get; set; }

        public decimal ServiceAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public string PaymentStatus { get; set; } = "ChuaThanhToan";

        public DateTime? PaymentDate { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }
    }
}
