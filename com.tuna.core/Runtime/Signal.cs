using System;
using System.Collections.Generic;

namespace Core
{
    public class Signal : IDisposable
    {
        private Dictionary<int, List<IReceive>> _signalDict = new Dictionary<int, List<IReceive>>(new FastComparable());
        
        public void Send<T>(in T val)
        {
            if (!_signalDict.TryGetValue(typeof(T).GetHashCode(), out var receiveList))
                return;
            int count = receiveList.Count;
            for (int index = 0; index < count; index++)
                (receiveList[index] as IReceive<T>)?.Receive(in val);
        }
        
        public void Add(IReceive receive, Type type)
        {
            int hashCode = type.GetHashCode();
            if (_signalDict.TryGetValue(hashCode, out var receiveList))
                receiveList.Add(receive);
            else
                _signalDict.Add(hashCode, new List<IReceive> { receive });
        }

        public void Remove(IReceive receive, Type type)
        {
            if (!_signalDict.TryGetValue(type.GetHashCode(), out var receiveList))
                return;
            receiveList.Remove(receive);
        }
        
        public void Dispose()
        {
            _signalDict.Clear();
        }
    }
    
    public class FastComparable : IEqualityComparer<int>
    {
        public static FastComparable Default = new FastComparable();

        public bool Equals(int x, int y)
        {
            return x == y;
        }

        public int GetHashCode(int obj)
        {
            return obj.GetHashCode();
        }
    }
    
    public interface IReceive<T> : IReceive
    {
        void Receive(in T signal);
    }

    public interface IReceive
    {
    }
}