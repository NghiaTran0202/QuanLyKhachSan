using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace quanlykhachsan.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required]
        [StringLength(20)]
        public string RoomNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string RoomName { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public decimal HourlyPrice { get; set; }

        public bool Status { get; set; }

        [StringLength(255)]
        public string? Image { get; set; }

        public int RoomTypeId { get; set; }

        [ForeignKey("RoomTypeId")]
        public virtual RoomType? RoomType { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}