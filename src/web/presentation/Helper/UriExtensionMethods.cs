﻿using System.Web;

namespace presentation.Helper;

public static class UriExtensionsMethods
{
    /// <summary>
    /// Adds the specified parameter to the Query String.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="paramName">Name of the parameter to add.</param>
    /// <param name="paramValue">Value for the parameter to add.</param>
    /// <returns>Url with added parameter.</returns>
    public static Uri AddParameter(this Uri url, string paramName, string paramValue)
    {
        var uriBuilder = new UriBuilder(url);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query[paramName] = paramValue;
        uriBuilder.Query = query.ToString();

        return uriBuilder.Uri;
    }
}