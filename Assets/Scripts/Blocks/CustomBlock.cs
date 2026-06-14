public interface ICustomBlock
{
    void Damaged(bool automatic, ulong clientId, byte key);

    void Interacted();
}
