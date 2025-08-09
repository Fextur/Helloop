namespace Helloop.StateMachines
{
    public interface IState<T> where T : class
    {
        void OnEnter(T owner);
        void Update(T owner);
        void OnExit(T owner);
    }
}