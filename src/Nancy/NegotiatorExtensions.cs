﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nancy.Responses.Negotiation;

namespace Nancy
{
    public static class NegotiatorExtensions
    {
        /// <summary>
        /// Add a header to the response
        /// </summary>
        /// <param name="negotiator">Negotiator object</param>
        /// <param name="header">Header name</param>
        /// <param name="value">Header value</param>
        /// <returns>Modified negotiator</returns>
        public static Negotiator WithHeader(this Negotiator negotiator, string header, string value)
        {
            return negotiator.WithHeaders(new { Header = header, Value = value });
        }

        /// <summary>
        /// Adds headers to the response using anonymous types
        /// </summary>
        /// <param name="negotiator">Negotiator object</param>
        /// <param name="headers">
        /// Array of headers - each header should be an anonymous type with two string properties 
        /// 'Header' and 'Value' to represent the header name and its value.
        /// </param>
        /// <returns>Modified negotiator</returns>
        public static Negotiator WithHeaders(this Negotiator negotiator, params object[] headers)
        {
            return negotiator.WithHeaders(headers.Select(GetTuple).ToArray());
        }

        /// <summary>
        /// Adds headers to the response using anonymous types
        /// </summary>
        /// <param name="negotiator">Negotiator object</param>
        /// <param name="headers">
        /// Array of headers - each header should be a Tuple with two string elements 
        /// for header name and header value
        /// </param>
        /// <returns>Modified negotiator</returns>
        public static Negotiator WithHeaders(this Negotiator negotiator, params Tuple<string, string>[] headers)
        {
            foreach (var keyValuePair in headers)
            {
                negotiator.NegotiationContext.Headers[keyValuePair.Item1] = keyValuePair.Item2;
            }

            return negotiator;
        }

        /// <summary>
        /// Allows the response to be negotiated with any processors available for any content type
        /// </summary>
        /// <param name="negotiator">Negotiator object</param>
        /// <returns>Modified negotiator</returns>
        public static Negotiator WithFullNegotiation(this Negotiator negotiator)
        {
            negotiator.NegotiationContext.PermissableMediaRanges.Clear();
            negotiator.NegotiationContext.PermissableMediaRanges.Add("*/*");

            return negotiator;
        }

        /// <summary>
        /// Allows the response to be negotiated with a specific media range
        /// This will remove the wildcard range if it is already specified
        /// </summary>
        /// <param name="negotiator">Negotiator object</param>
        /// <param name="mediaRange">Media range to add</param>
        /// <returns>Modified negotiator</returns>
        public static Negotiator WithAllowedMediaRange(this Negotiator negotiator, MediaRange mediaRange)
        {
            var wildcards =
                negotiator.NegotiationContext.PermissableMediaRanges.Where(
                    mr => mediaRange.Type.IsWildcard && mediaRange.Subtype.IsWildcard);

            foreach (var wildcard in wildcards)
            {
                negotiator.NegotiationContext.PermissableMediaRanges.Remove(wildcard);
            }

            negotiator.NegotiationContext.PermissableMediaRanges.Add(mediaRange);

            return negotiator;
        }

        /// <summary>
        /// Uses the specified model as the default model for negotiation
        /// </summary>
        /// <param name="negotiator">Negotiator object</param>
        /// <param name="model">Model object</param>
        /// <returns>Modified negotiator</returns>
        public static Negotiator WithModel(this Negotiator negotiator, dynamic model)
        {
            negotiator.NegotiationContext.DefaultModel = model;

            return negotiator;
        }

        /// <summary>
        /// Uses the specified view for html output
        /// </summary>
        /// <param name="negotiator">Negotiator object</param>
        /// <param name="viewName">View name</param>
        /// <returns>Modified negotiator</returns>
        public static Negotiator WithView(this Negotiator negotiator, string viewName)
        {
            negotiator.NegotiationContext.ViewName = viewName;

            return negotiator;
        }

        /// <summary>
        /// Sets the model to use for a particular media range.
        /// Will also add the MediaRange to the allowed list
        /// </summary>
        /// <param name="negotiator">Negotiator object</param>
        /// <param name="range">Range to match against</param>
        /// <param name="model">Model object</param>
        /// <returns>Updated negotiator object</returns>
        public static Negotiator WithMediaRangeModel(this Negotiator negotiator, MediaRange range, object model)
        {
            return negotiator.WithMediaRangeModel(range, () => model);
        }

        /// <summary>
        /// Sets the model to use for a particular media range.
        /// Will also add the MediaRange to the allowed list
        /// </summary>
        /// <param name="negotiator">Negotiator object</param>
        /// <param name="range">Range to match against</param>
        /// <param name="modelFactory">Model factory for returning the model object</param>
        /// <returns>Updated negotiator object</returns>
        public static Negotiator WithMediaRangeModel(this Negotiator negotiator, MediaRange range, Func<object> modelFactory)
        {
            negotiator.NegotiationContext.PermissableMediaRanges.Add(range);
            negotiator.NegotiationContext.MediaRangeModelMappings.Add(range, modelFactory);

            return negotiator;
        }

        private static Tuple<string, string> GetTuple(object header)
        {
            var properties = header.GetType()
                                   .GetProperties()
                                   .Where(prop => prop.CanRead && prop.PropertyType == typeof(string))
                                   .ToArray();

            var headerProperty = properties
                                    .Where(p => string.Equals(p.Name, "Header", StringComparison.InvariantCultureIgnoreCase))
                                    .FirstOrDefault();

            var valueProperty = properties
                                    .Where(p => string.Equals(p.Name, "Value", StringComparison.InvariantCultureIgnoreCase))
                                    .FirstOrDefault();

            if (headerProperty == null || valueProperty == null)
            {
                throw new ArgumentException("Unable to extract 'Header' or 'Value' properties from anonymous type.");
            }

            return Tuple.Create(
                (string)headerProperty.GetValue(header, null),
                (string)valueProperty.GetValue(header, null));
        }
    }
}