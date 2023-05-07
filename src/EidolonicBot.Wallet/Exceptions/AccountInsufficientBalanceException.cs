namespace EidolonicBot.Exceptions;

public class AccountInsufficientBalanceException : Exception {
    public AccountInsufficientBalanceException(decimal balance) {
        Balance = balance;
    }

    public decimal Balance { get; }
}