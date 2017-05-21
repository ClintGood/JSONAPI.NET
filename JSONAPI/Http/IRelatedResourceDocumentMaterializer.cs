using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JSONAPI.Documents;
using System.Collections.Generic;

namespace JSONAPI.Http
{
    /// <summary>
    /// Crafts a document corresponding to a related resource URL
    /// </summary>
    public interface IRelatedResourceDocumentMaterializer
    {
        /// <summary>
        /// Builds a document containing the results of the relationship.
        /// </summary>
        Task<IJsonApiDocument> GetRelatedResourceDocument(string primaryResourceId, IEnumerable<KeyValuePair<string, string>> requestParams, 
            System.Uri requestUri, CancellationToken cancellationToken);
    }
}