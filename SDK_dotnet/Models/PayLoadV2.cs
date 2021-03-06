﻿using System.ComponentModel.DataAnnotations;

namespace Gluwa.SDK_dotnet.Models
{
    public sealed class PayLoadV2
    {
        /// <summary>
        /// The ID of the webhook.
        /// </summary>
        [Required]
        public string ID { get; set; }

        /// <summary>
        /// The created date and time of the webhook.
        /// </summary>
        [Required]
        public string CreatedDateTime { get; set; }

        /// <summary>
        /// The type of the resource. Transaction, Exchange.
        /// </summary>
        [Required]
        public EResourceType? ResourceType { get; set; }

        /// <summary>
        /// Must use constants defined in EEventName class
        /// </summary>
        [Required]
        public string EventName { get; set; }

        /// <summary>
        /// The summary of the webhook.
        /// </summary>
        [Required]
        public string Summary { get; set; }

        /// <summary>
        /// The resource associated with the webhook.
        /// </summary>
        [Required]
        public IResourceObj Resource { get; set; }
    }
}
