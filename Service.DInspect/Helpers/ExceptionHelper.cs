using System;

namespace Service.DInspect.Helpers
{
    [Serializable]
    public class ExceptionHelper : Exception
    {
        public Type Type { get; set; }
        public int Status { get; set; }

        public ExceptionHelper() : base() { }
        public ExceptionHelper(string message) : base(message) { }
        public ExceptionHelper(string message, Exception inner) : base(message, inner) { }
    }
}
