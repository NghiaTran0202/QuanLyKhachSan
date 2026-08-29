using System;
using System.ComponentModel.DataAnnotations;

namespace quanlykhachsan.Models
{
    public class BookingCreateViewModel
    {
        public int? CustomerId { get; set; }

        public bool IsNewCustomer { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? CCCD { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        public string BookingType { get; set; } = "Ngay";

        public int HoursBooked { get; set; } = 1;

        public bool CheckInNow { get; set; } = true;

        [DataType(DataType.DateTime)]
        public DateTime? HourlyCheckInDateTime { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Range(1, 20)]
        public int NumberOfGuests { get; set; }
    }
}