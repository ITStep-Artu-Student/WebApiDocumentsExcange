﻿namespace sabatex.ObjectsExchange.Models
{
using Sabatex.Core;
using System.ComponentModel.DataAnnotations;
    /// <summary>
    /// обєкти які потрібно отримати з нода
    /// </summary>
    public class QueryObject : IEntityBase
    {
        public long Id { get; set; }
        public Guid Sender { get; set; }
        public Guid Destination { get; set; }
        [MaxLength(50)]
        public string ObjectId { get; set; } = default!; //must lovercase
        [MaxLength(50)]
        public string ObjectType { get; set; } = default!; // lovercase

        string IEntityBase.KeyAsString() => Id.ToString();

    }

}