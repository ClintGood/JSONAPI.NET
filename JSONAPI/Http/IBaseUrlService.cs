using System.Net.Http;

namespace JSONAPI.Http
{
    /// <summary>
    /// Service allowing you to get the base URL for a request
    /// </summary>
    public interface IBaseUrlService
    {
        /// <summary>
        /// Gets the base URL for a request
        /// </summary>
        string GetBaseUrl(System.Uri requestUri);

        /// <summary>
        /// Gets the context path JSONAPI is served under without slashes at the beginning and end.
        /// </summary>
        /// <returns></returns>
        string GetContextPath();
    }
}
