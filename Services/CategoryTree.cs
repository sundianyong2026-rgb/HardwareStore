using HardwareStore.Models.Entities;

namespace HardwareStore.Services;

/// <summary>
/// Turns the flat category table into a parent-first list. Every category is
/// followed immediately by its children, so a parent can never be displayed
/// below its own subcategories.
/// </summary>
public static class CategoryTree
{
    public static List<Category> Flatten(IEnumerable<Category> categories)
    {
        var all = categories.OrderBy(c => c.SortOrder).ThenBy(c => c.CategoryId).ToList();

        var byParent = all.GroupBy(c => c.ParentId ?? 0)
            .ToDictionary(g => g.Key, g => g.ToList());

        var visited = new HashSet<int>();
        var result = new List<Category>();

        void Walk(int parentId)
        {
            if (!byParent.TryGetValue(parentId, out var children)) return;
            foreach (var c in children.OrderBy(x => x.SortOrder).ThenBy(x => x.CategoryId))
            {
                if (!visited.Add(c.CategoryId)) continue;
                result.Add(c);
                Walk(c.CategoryId);
            }
        }

        Walk(0);

        // A category with a broken parent link still belongs on the page.
        foreach (var c in all)
            if (!visited.Contains(c.CategoryId)) result.Add(c);

        return result;
    }

    /// <summary>Depth of every category, used for indentation in dropdowns.</summary>
    public static Dictionary<int, int> Depths(IEnumerable<Category> flattened)
    {
        var parentOf = flattened.ToDictionary(c => c.CategoryId, c => c.ParentId);
        var depths = new Dictionary<int, int>();

        foreach (var c in flattened)
        {
            var depth = 0;
            int? parent = parentOf.TryGetValue(c.CategoryId, out var p) ? p : null;
            while (parent != null && depth < 5)
            {
                depth++;
                parent = parentOf.TryGetValue(parent.Value, out var np) ? np : null;
            }
            depths[c.CategoryId] = depth;
        }

        return depths;
    }
}
