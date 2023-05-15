namespace EidolonicBot;

public static class EnumerableExtensions {
    public static IEnumerable<T[]> Split<T>(this IReadOnlyCollection<T> arr, int size) {
        for (var i = 0; i < arr.Count / size + 1; i++) yield return arr.Skip(i * size).Take(size).ToArray();
    }
}
