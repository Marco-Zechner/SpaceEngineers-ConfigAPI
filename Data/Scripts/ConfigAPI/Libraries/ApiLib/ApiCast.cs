using System.Collections.Generic;

namespace MarcoZechner.ApiLib
{
    public static class ApiCast
    {
        public static bool Try<T>(object obj, out T result)
        {
            if (obj is T)
            {
                result = (T)obj;
                return true;
            }
            result = default(T);
            return false;
        }

        public static bool TryGet<T>(Dictionary<string, object> dict, string key, out T value)
        {
            object o;
            if (dict != null && dict.TryGetValue(key, out o) && o is T)
            {
                value = (T)o;
                return true;
            }
            value = default(T);
            return false;
        }
    }
}