namespace BiliRadar.Models;

public sealed class CreatorRow
{
    public CreatorRow(BiliCreator creator)
    {
        Mid = creator.Mid;
        Name = creator.Name;
        Initial = string.IsNullOrWhiteSpace(creator.Name) ? "?" : creator.Name[..1];
    }

    public long Mid { get; }

    public string Name { get; }

    public string Initial { get; }
}
