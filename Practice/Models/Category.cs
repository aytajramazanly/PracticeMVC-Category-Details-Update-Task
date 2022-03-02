using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Practice.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Image { get; set; }

        public bool IsMain { get; set; }

        public bool IsDeleted { get; set; }

        public Category Parent { get; set; }

        public ICollection<Category> Children { get; set; }

        [NotMapped]
        public IFormFile Photo { get; set; }

    }
}
