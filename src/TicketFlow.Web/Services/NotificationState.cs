namespace TicketFlow.Web.Services;

public class NotificationState
{
    public int UnreadCount { get; private set; }
    public event Action? Changed;

    public void SetCount(int count)
    {
        UnreadCount = count;
        Changed?.Invoke();
    }

    public void Increment()
    {
        UnreadCount++;
        Changed?.Invoke();
    }
}
