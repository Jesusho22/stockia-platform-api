using Microsoft.AspNetCore.Mvc.ApplicationModels;
using DataBit.StockIA.Platform.Shared.Infrastructure.Interfaces.ASP.Configuration.Extensions;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Interfaces.ASP.Configuration;

/// <summary>
///     Rewrites the <c>[controller]</c> route token to kebab case
///     (<c>InventoryItemsController</c> → <c>api/v1/inventory-items</c>).
/// </summary>
public class KebabCaseRouteNamingConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        foreach (var selector in controller.Selectors)
            selector.AttributeRouteModel = ReplaceControllerTemplate(selector, controller.ControllerName);

        foreach (var selector in controller.Actions.SelectMany(action => action.Selectors))
            selector.AttributeRouteModel = ReplaceControllerTemplate(selector, controller.ControllerName);
    }

    private static AttributeRouteModel? ReplaceControllerTemplate(SelectorModel selector, string name) =>
        selector.AttributeRouteModel != null
            ? new AttributeRouteModel { Template = selector.AttributeRouteModel.Template?.Replace("[controller]", name.ToKebabCase()) }
            : null;
}
