using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace quanlykhachsan.Models
{
    public class RoomType
    {
        [Key]
        public int RoomTypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string TypeName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [Range(1, 20)]
        public int MaxGuests { get; set; } = 1;

        public virtual ICollection<Room>? Rooms { get; set; }
    }
}
