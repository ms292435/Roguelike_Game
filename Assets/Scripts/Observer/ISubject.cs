namespace Roguelike
{
    /// <summary>
    /// Subject interface implemented by entities that emit gameplay notifications to registered observers.
    /// </summary>
    public interface ISubject
    {
        /// <summary>
        /// Subscribes an observer to receive notifications from this subject.
        /// </summary>
        /// <param name="pObserver">The observer instance to register.</param>
        void AddObserver(IObserver pObserver);

        /// <summary>
        /// Unsubscribes an observer from receiving notifications from this subject.
        /// </summary>
        /// <param name="pObserver">The observer instance to remove.</param>
        void RemoveObserver(IObserver pObserver);
    }
}