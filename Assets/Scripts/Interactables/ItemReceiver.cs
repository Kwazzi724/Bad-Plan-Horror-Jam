public interface ItemReceiver
{
    bool CanReceive(Pickup item);
    void Receive(Pickup item);
}
