namespace EidolonicBot.Exceptions;

public class AccountInsufficientBalanceException(
  decimal balance
) : Exception {
  public decimal Balance { get; } = balance;
}
