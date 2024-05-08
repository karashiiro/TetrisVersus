using VRC.SDK3.Data;

namespace DataTokenExtensions
{
    public static class DataTokenExtensions
    {
        public static T As<T>(this DataToken token) where T : class
        {
            return (T)token.Reference;
        }
    }
}