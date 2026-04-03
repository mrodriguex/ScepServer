using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScepAdmin.Models;
using ScepAdmin.Services;

namespace ScepAdmin.Pages.Crl;

public class IndexModel : PageModel
{
    private readonly ICrlService _crlService;

    public IndexModel(ICrlService crlService)
    {
        _crlService = crlService;
    }

    public CrlStatus? Status { get; set; }

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    [TempData]
    public string LastPayloadBase64 { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        Status = await _crlService.GetStatusAsync();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        var result = await _crlService.GenerateAsync();
        StatusMessage = result.Message + $" Version={result.Version}. Revoked={result.RevokedCount}.";
        LastPayloadBase64 = result.PayloadBase64;
        return RedirectToPage();
    }
}
