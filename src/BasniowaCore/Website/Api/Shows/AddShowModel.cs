﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Website.Api.Shows
{
    /// <summary>
    /// Model for command adding show.
    /// </summary>
    public class AddShowModel
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the subtitle.
        /// </summary>
        [MaxLength(500)]
        public string Subtitle { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the properties.
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
    }
}