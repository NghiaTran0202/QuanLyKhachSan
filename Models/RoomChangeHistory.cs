using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quanlykhachsan.Models
{
    public class RoomChangeHistory
    {
        [Key]
        public int RoomChangeHistoryId { get; set; }

        public int BookingId { get; set; }

        public int OldRoomId { get; set; }

        public int NewRoomId { get; set; }

        public decimal OldRoomPrice { get; set; }

        public decimal NewRoomPrice { get; set; }

        [StringLength(20)]
        public string PriceMode { get; set; } = "KeepCurrent";

        public decimal AppliedPrice { get; set; }

        public DateTime ChangedAt { get; set; }

        [StringLength(255)]
        public string? Reason { get; set; }

        [ForeignKey(nameof(BookingId))]
        public virtual Booking? Booking { get; set; }

        [ForeignKey(nameof(OldRoomId))]
        public virtual Room? OldRoom { get; set; }

        [ForeignKey(nameof(NewRoomId))]
        public virtual Room? NewRoom { get; set; }
    }
}