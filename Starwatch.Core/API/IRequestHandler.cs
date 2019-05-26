using WebSocketSharp.Server;

namespace Starwatch.API
{
    public interface IRequestHandler
    {
        bool HandleRequest(RequestMethod method, HttpRequestEventArgs args, Authentication auth);
    }
}
