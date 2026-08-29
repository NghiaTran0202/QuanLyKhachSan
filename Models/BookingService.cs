using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quanlykhachsan.Models
{
    public class BookingService
    {
        [Key]
        public int BookingServiceId { get; set; }

        public int BookingId { get; set; }

        public int ServiceId { get; set; }

        public int Quantity { get; set; }

        public decimal TotalPrice { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service? Service { get; set; }

    }

}