using Microsoft.Extensions.DependencyInjection;
using TrabajoSocial.Application.Common.Exceptions;

namespace TrabajoSocial.Application.Catalogs;

internal sealed class CatalogServiceFactory(IServiceProvider serviceProvider) : ICatalogServiceFactory
{
    public ICatalogService Get(string key)
    {
        if (!CatalogRegistry.TryGet(key, out var definition))
        {
            throw new NotFoundException($"La tabla maestra '{key}' no existe.");
        }

        return serviceProvider.GetRequiredKeyedService<ICatalogService>(definition.Key);
    }
}
