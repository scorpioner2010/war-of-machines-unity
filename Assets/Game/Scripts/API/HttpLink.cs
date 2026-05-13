namespace Game.Scripts.API
{
    public abstract class HttpLink
    {
        public const string LocalAPIBase = "https://localhost:44377";
        public const string RenderAPIBase = "https://war-of-machines-api.onrender.com";
        public static string APIBase = RenderAPIBase;
    }
}