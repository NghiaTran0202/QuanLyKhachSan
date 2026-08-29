using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quanlykhachsan.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        public int CustomerId { get; set; }

        public int RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng")]
        [DataType(DataType.DateTime)]
        public DateTime CheckInDate { get; set; }

        public DateTime? ActualCheckInTime { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng")]
        [DataType(DataType.DateTime)]
        public DateTime CheckOutDate { get; set; }

        public DateTime? ActualCheckOutTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số người")]
        [Range(1, 20)]
        public int NumberOfGuests { get; set; }

        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        public string BookingType { get; set; } = "Ngay";

        public int HoursBooked { get; set; } = 0;

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        [ForeignKey(nameof(RoomId))]
        public virtual Room? Room { get; set; }

        public virtual ICollection<RoomChangeHistory> RoomChangeHistories
        { get; set; } = new List<RoomChangeHistory>();
    }
}