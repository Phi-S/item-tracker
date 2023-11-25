﻿using System.ComponentModel.DataAnnotations;

namespace infrastructure.Database.Models;

public class ItemListItemActionDbModel
{
    [Key] public long Id { get; set; }
    [Required] public required ItemListDbModel ItemListDbModel { get; set; }
    [Required] public required long ItemId { get; set; }
    [Required] [StringLength(1)] public required string Action { get; set; }
    [Required] public required decimal PricePerOne { get; set; }
    [Required] public required long Amount { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}