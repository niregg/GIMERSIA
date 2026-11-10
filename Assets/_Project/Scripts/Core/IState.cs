namespace Gimersia.Core
{
    public interface IState
    {
        void OnEnter();
        void OnTick(float deltaTime);
        void OnFixedTick(float fixedDeltaTime);
        void OnExit();
    }
}
