namespace DynaBomberClient
{
    public interface IGameState
    {
        void EnterFrame(double dt);
        void Activate();
        void Deactivate();
    }
}
