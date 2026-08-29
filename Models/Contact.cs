using System;
using System.ComponentModel.DataAnnotations;

namespace quanlykhachsan.Models
{
    public class Contact
    {
        [Key]
        public int ContactId { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        public string? Message { get; set; }

        public bool IsAnonymous { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }
}