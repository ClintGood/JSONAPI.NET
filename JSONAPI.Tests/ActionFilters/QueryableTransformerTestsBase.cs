﻿using System.Linq;
using System.Net.Http;
//using JSONAPI.ActionFilters;
using JSONAPI.QueryableTransformers;

namespace JSONAPI.Tests.ActionFilters
{
    public abstract class QueryableTransformerTestsBase
    {
        internal IQueryable<T> Transform<T>(IQueryableFilteringTransformer filteringTransformer, IQueryable<T> query, string uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            return filteringTransformer.Filter(query, request.GetQueryNameValuePairs());
        }
    }
}
