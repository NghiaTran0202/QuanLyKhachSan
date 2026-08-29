using System;
using System.ComponentModel.DataAnnotations;

namespace quanlykhachsan.Models
{
    public class BookingRequestModel
    {
        [Required]
        public string FullName { get; set; } = "";

        [Required]
        public string Phone { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string CCCD { get; set; } = "";

        [Required]
        public int RoomTypeId { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        public string BookingType { get; set; } = "Ngay";

        [DataType(DataType.Date)]
        public DateTime? CheckInDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CheckOutDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? HourlyCheckInDateTime { get; set; }

        [Range(1, 24)]
        public int HoursBooked { get; set; } = 1;

        [Required]
        [Range(1, 20)]
        public int NumberOfGuests { get; set; } = 1;
    }
}