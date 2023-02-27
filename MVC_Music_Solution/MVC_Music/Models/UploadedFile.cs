﻿using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace MVC_Music.Models
{
    public class UploadedFile
    {
        public int ID { get; set; }

        [StringLength(255, ErrorMessage = "The name of the file cannot be more than 255 characters.")]
        [Display(Name = "File Name")]
        public string FileName { get; set; }

        [StringLength(255)]
        public string MimeType { get; set; }

        public FileContent FileContent { get; set; } = new FileContent();
    }
}
