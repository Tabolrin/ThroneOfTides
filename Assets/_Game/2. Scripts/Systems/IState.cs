namespace ThroneOfTides.Systems
{
    public interface IState
    {
        void Enter();
        void Tick();
        void Exit();
    }
}