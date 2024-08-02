namespace EidolonicBot.Models;

[Index(nameof(Address), IsUnique = true)]
public class Subscription
{
    [Key] public Guid Id { get; set; }

    [Required] [MaxLength(66)] public string Address { get; set; } = null!;

    public ICollection<SubscriptionByChat> SubscriptionByChat { get; set; } = null!;
}
