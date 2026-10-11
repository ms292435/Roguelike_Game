namespace Roguelike
{
    /// <summary>
    /// Observer interface implemented by objects that react to player gameplay events (e.g. UI, level progression).
    /// </summary>
    public interface IObserver
    {
        /// <summary>
        /// Called when an observed subject triggers a named notification event.
        /// </summary>
        /// <param name="pSubject">The event subject that raised the notification.</param>
        /// <param name="pEventName">Identifier of the event (e.g. "LevelUp", "Experience").</param>
        void OnNotify(ISubject pSubject, string pEventName);
    }
}