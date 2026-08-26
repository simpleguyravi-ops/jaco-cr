using JACO.CR.Web.Data;
using JACO.CR.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JACO.CR.Web.Controllers;

public sealed class AdminController(CrDbContext db) : Controller
{
    public static readonly string[] SupportedTypes =
    [
        "Department",
        "Priority",
        "Impact",
        "ChangeReason"
    ];

    [HttpGet]
    public async Task<IActionResult> Index(string? type = null)
    {
        type ??= "Department";

        if (!SupportedTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            type = "Department";

        ViewBag.LookupTypes = SupportedTypes;

        var values = await db.CRLookupValues
            .Where(x => x.LookupType == type)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayText)
            .ToListAsync();

        ViewBag.SelectedType = type;
        return View(values);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string lookupType, string value, string displayText, int sortOrder = 10)
    {
        if (!SupportedTypes.Contains(lookupType, StringComparer.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Invalid lookup type.";
            return RedirectToAction(nameof(Index));
        }

        value = value.Trim();
        displayText = displayText.Trim();

        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(displayText))
        {
            TempData["Error"] = "Value and Display Text are required.";
            return RedirectToAction(nameof(Index), new { type = lookupType });
        }

        if (await db.CRLookupValues.AnyAsync(x =>
            x.LookupType == lookupType && x.Value == value))
        {
            TempData["Error"] = "This lookup value already exists.";
            return RedirectToAction(nameof(Index), new { type = lookupType });
        }

        db.CRLookupValues.Add(new CRLookupValue
        {
            LookupType = lookupType,
            Value = value,
            DisplayText = displayText,
            SortOrder = sortOrder,
            Active = true
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Lookup value added.";
        return RedirectToAction(nameof(Index), new { type = lookupType });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await db.CRLookupValues.FindAsync(id);
        if (item is null) return NotFound();

        item.Active = !item.Active;
        await db.SaveChangesAsync();

        TempData["Success"] = item.Active
            ? "Lookup value activated."
            : "Lookup value deactivated.";

        return RedirectToAction(nameof(Index), new { type = item.LookupType });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string displayText, int sortOrder)
    {
        var item = await db.CRLookupValues.FindAsync(id);
        if (item is null) return NotFound();

        displayText = displayText.Trim();
        if (string.IsNullOrWhiteSpace(displayText))
        {
            TempData["Error"] = "Display Text is required.";
            return RedirectToAction(nameof(Index), new { type = item.LookupType });
        }

        item.DisplayText = displayText;
        item.SortOrder = sortOrder;

        await db.SaveChangesAsync();
        TempData["Success"] = "Lookup value updated.";
        return RedirectToAction(nameof(Index), new { type = item.LookupType });
    }
}
