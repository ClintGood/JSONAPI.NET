using System.Collections.Generic;

namespace JSONAPI.Http
{
    /// <summary>
    /// Service to extract sort expressions from an HTTP request
    /// </summary>
    public interface ISortExpressionExtractor
    {
        /// <summary>
        /// Extracts sort expressions from the request
        /// </summary>
        /// <param name="requestParams"></param>
        /// <returns></returns>
        string[] ExtractSortExpressions(IEnumerable<KeyValuePair<string, string>> requestParams);
    }
}
