using DataAccess.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; } = null;


        protected BaseEntity()
        {
            Id = Guid.NewGuid().ToString();
            var vietnamTime = DateTimeHelper.GetVietnamTime();
            CreatedAt = vietnamTime;
            UpdatedAt = vietnamTime;
        }
    }

}
