using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace JSONAPI.Http
{
    /// <summary>
    /// Default implementation of <see cref="IIncludeExpressionExtractor" />
    /// </summary>
    public class DefaultIncludeExpressionExtractor: IIncludeExpressionExtractor
    {
        private const string IncludeQueryParamKey = "include";

        public string[] ExtractIncludeExpressions(IEnumerable<KeyValuePair<string, string>> requestParams)
        {
            var includeParam = requestParams.FirstOrDefault(kvp => kvp.Key == IncludeQueryParamKey);
            if (includeParam.Key != IncludeQueryParamKey) return new string[] { };
            return includeParam.Value.Split(',');
        }
    }
}
