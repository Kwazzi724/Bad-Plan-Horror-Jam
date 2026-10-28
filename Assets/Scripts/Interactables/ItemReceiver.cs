public interface IItemReceiver
{
    bool CanReceive(Pickup item);
    void Receive(Pickup item);
}
