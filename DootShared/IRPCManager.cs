using System;



namespace Doot
{
    public interface IRPCManager
    {
        void RegisterRPCFunction(string name, Func<SessionBase, object[], object> function);
        Func<SessionBase, object[], object> GetRPCFunction(string name);
    }
}
