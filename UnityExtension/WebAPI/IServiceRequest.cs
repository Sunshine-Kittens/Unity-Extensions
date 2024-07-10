using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    public interface IServiceRequest<T> where T : IServiceResponse, new()
    {
        public ReadOnlyHttpRequest HttpRequest { get; }

        public Task<T> Send();
    }
}
