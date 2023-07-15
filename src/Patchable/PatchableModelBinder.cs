﻿// Copyright (c) Dennis Shevtsov. All rights reserved.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using System.ComponentModel;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Patchable;

/// <summary>
/// The <see cref="PatchableModelBinder"/> provides a mechanism to create an instance
/// of a model that implements the <see cref="IPatchable"/> from HTTP request. The binder
/// allows to bind parameters from the body, the route and the query string of the HTTP.
/// The binder also sets a list of properties that have been populated from the HTTP request.
/// </summary>
public sealed class PatchableModelBinder : IModelBinder
{
  /// <summary>
  /// Attempts to bind a model.
  /// </summary>
  /// <param name="bindingContext">A context that contains operating information for model binding and validation.</param>
  /// <returns>An instance of the <see cref="Task"/> that represents an asynchronous operation.</returns>
  public async Task BindModelAsync(ModelBindingContext bindingContext)
  {
    object model = Activator.CreateInstance(bindingContext.ModelType)!;
    HashSet<string> properties = new();

    await FillOutFromBodyAsync(model, properties, bindingContext);
    FillOutFromRoute(model, properties, bindingContext);
    FillOutFromQueryString(model, properties, bindingContext);

    bindingContext.Result = ModelBindingResult.Success(model);
  }

  private async Task FillOutFromBodyAsync(object model, HashSet<string> properties, ModelBindingContext bindingContext)
  {
    if (bindingContext.HttpContext.Request.ContentLength == null &&
        bindingContext.HttpContext.Request.ContentLength == 0)
    {
      return;
    }

    JsonDocument? document = await JsonSerializer.DeserializeAsync<JsonDocument>(
        bindingContext.HttpContext.Request.Body);

    if (document == null)
    {
      return;
    }

    foreach (var documentProperty in document.RootElement.EnumerateObject())
    {
      ModelMetadata? modelProperty = bindingContext.ModelMetadata.Properties[documentProperty.Name];

      if (modelProperty != null && modelProperty.PropertySetter != null)
      {
        modelProperty.PropertySetter.Invoke(
          model, documentProperty.Value.Deserialize(modelProperty.ModelType));
      }
    }
  }

  private void FillOutFromRoute(object model, HashSet<string> properties, ModelBindingContext bindingContext)
  {
    foreach (ModelMetadata propertyMetadata in bindingContext.ModelMetadata.Properties)
    {
      object? routeValue;
      TypeConverter? converter;

      if (propertyMetadata != null &&
          propertyMetadata.PropertySetter != null &&
          propertyMetadata.PropertyName != null &&
          (routeValue = bindingContext.ActionContext.RouteData.Values[propertyMetadata.PropertyName]) != null &&
          (converter = TypeDescriptor.GetConverter(propertyMetadata.ModelType)) != null)
      {
        propertyMetadata.PropertySetter(model!, converter.ConvertFrom(routeValue));
      }
    }
  }

  private void FillOutFromQueryString(object model, HashSet<string> properties, ModelBindingContext bindingContext)
  {
    foreach (KeyValuePair<string, StringValues> queryParam in bindingContext.HttpContext.Request.Query)
    {
      ModelMetadata? propertyMetadata;
      TypeConverter? converter;

      if ((propertyMetadata = bindingContext.ModelMetadata.Properties[queryParam.Key]) != null &&
          propertyMetadata.PropertySetter != null &&
          (converter = TypeDescriptor.GetConverter(propertyMetadata.ModelType)) != null)
      {
        propertyMetadata.PropertySetter(model!, converter.ConvertFrom(queryParam.Value.ToString()));
      }
    }
  }
}