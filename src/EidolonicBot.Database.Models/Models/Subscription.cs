using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EidolonicBot.Models;

[Index(nameof(Address), IsUnique = true)]
public class Subscription {
    [Key] public Guid Id { get; set; }

    [Required] public string Address { get; set; } = null!;

    public ICollection<SubscriptionByChat> SubscriptionByChat { get; set; } = null!;
}