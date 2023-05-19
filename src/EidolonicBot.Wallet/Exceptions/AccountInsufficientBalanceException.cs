namespace EidolonicBot.Exceptions;

public sealed class AccountInsufficientBalanceException : Exception {
    public AccountInsufficientBalanceException(decimal balance) {
        Balance = balance;
        Data.Add("Balance", balance);
    }

    public decimal Balance { get; }
}
