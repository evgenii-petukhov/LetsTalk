namespace LetsTalk.Server.KeepAlive;

// A hosted store that must see regular activity, or the provider deletes or pauses it.
// Each store supplies its own implementation, so the hosted service stays provider-neutral.
public interface IKeepAliveTarget
{
    // Shown in the logs; never a connection string.
    string Name { get; }

    // Sends the smallest signal the provider counts as activity. Throws when the store cannot be reached.
    Task KeepAliveAsync(CancellationToken cancellationToken);
}
