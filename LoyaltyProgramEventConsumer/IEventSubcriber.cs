namespace LoyaltyProgramEventConsumer
{
    using System.Threading.Tasks;

    public interface IEventSubcriber<TTransferedEvent> where TTransferedEvent : class
    {
        Task ReadAndHandleEvents();

        Task<TTransferedEvent> ReadEvents();

        Task HandleEvents(TTransferedEvent @event);

        bool IsValidEvent(TTransferedEvent @event);
    }
}