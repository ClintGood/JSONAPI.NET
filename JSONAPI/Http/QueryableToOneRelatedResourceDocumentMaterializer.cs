using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JSONAPI.Documents;
using JSONAPI.Documents.Builders;
using System.Collections.Generic;

namespace JSONAPI.Http
{
    /// <summary>
    /// Base class for implementations of <see cref="IRelatedResourceDocumentMaterializer"/> that use IQueryable to get related resources
    /// for a to-many relationship.
    /// </summary>
    public abstract class QueryableToOneRelatedResourceDocumentMaterializer<TRelated> : IRelatedResourceDocumentMaterializer
    {
        private readonly ISingleResourceDocumentBuilder _singleResourceDocumentBuilder;
        private readonly IBaseUrlService _baseUrlService;

        /// <summary>
        /// Creates a new QueryableRelatedResourceDocumentMaterializer
        /// </summary>
        protected QueryableToOneRelatedResourceDocumentMaterializer(
            ISingleResourceDocumentBuilder singleResourceDocumentBuilder, IBaseUrlService baseUrlService)
        {
            _singleResourceDocumentBuilder = singleResourceDocumentBuilder;
            _baseUrlService = baseUrlService;
        }

        public async Task<IJsonApiDocument> GetRelatedResourceDocument(string primaryResourceId,
            IEnumerable<KeyValuePair<string, string>> requestParams, System.Uri requestUri, CancellationToken cancellationToken)
        {
            var record = await GetRelatedRecord(primaryResourceId, cancellationToken);
            var baseUrl = _baseUrlService.GetBaseUrl(requestUri);
            return _singleResourceDocumentBuilder.BuildDocument(record, baseUrl, null, null); // TODO: allow implementors to specify includes and metadata
        }

        /// <summary>
        /// Gets the query for the related resources
        /// </summary>
        protected abstract Task<TRelated> GetRelatedRecord(string primaryResourceId, CancellationToken cancellationToken);
    }
}