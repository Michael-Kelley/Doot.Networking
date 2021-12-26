using System;



namespace Doot
{
    public interface IRPCManager
    {
        void RegisterRPCFunction(string name, Func<object[], object> function);
        Func<object[], object> GetRPCFunction(string name);
    }
}
