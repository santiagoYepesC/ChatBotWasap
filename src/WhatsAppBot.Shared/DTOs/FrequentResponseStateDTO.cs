using System.ComponentModel.DataAnnotations;

namespace WhatsAppBot.Shared.DTOs;

public sealed class FrequentResponseStateDTO
{
    [Required]
    public bool? IsActive { get; init; }
}
