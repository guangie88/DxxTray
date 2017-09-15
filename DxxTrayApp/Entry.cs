using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DxxTrayApp
{
    [Table("Entry")]
    public class Entry
    {
        [Column("Index")]
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Index { get; set; }

        [Column("SubmitTime")]
        [Required]
        public DateTime SubmitTime { get; set; }
    }
}
