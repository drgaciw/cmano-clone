namespace ProjectAegis.Delegation.Decision;

public sealed class SeededRng
{
    private ulong _state;

    public SeededRng(int globalSeed, int agentSalt)
    {
        _state = (ulong)(globalSeed ^ (agentSalt * 0x9E3779B9));
        if (_state == 0)
        {
            _state = 1;
        }
    }

    public double NextUnit()
    {
        _state ^= _state << 13;
        _state ^= _state >> 7;
        _state ^= _state << 17;
        return (_state & 0xFFFFFF) / (double)0x1000000;
    }
}
