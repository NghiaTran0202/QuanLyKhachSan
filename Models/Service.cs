using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace quanlykhachsan.Models
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [Required]
        [StringLength(100)]
        public string ServiceName { get; set; } = "";

        [Required]
        public decimal Price { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

             [StringLength(255)]
        public string? Image { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}