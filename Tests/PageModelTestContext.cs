using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace SistemaDeControleComtac.Tests;

internal static class PageModelTestContext
{
    public static T ComTempData<T>(this T page) where T : PageModel
    {
        page.TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider());
        return page;
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
            => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
