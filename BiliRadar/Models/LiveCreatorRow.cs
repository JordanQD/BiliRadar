namespace BiliRadar.Models;

public sealed class LiveCreatorRow
{
    public LiveCreatorRow(BiliLiveCreator creator)
    {
        Mid = creator.Mid;
        RoomId = creator.RoomId;
        Name = creator.Name;
        AvatarUrl = creator.AvatarUrl;
        Title = creator.Title;
        Url = creator.Url;
    }

    public long Mid { get; }

    public long RoomId { get; }

    public string Name { get; }

    public string AvatarUrl { get; }

    public string Title { get; }

    public string Url { get; }

    public BiliLiveCreator ToModel()
    {
        return new BiliLiveCreator(Mid, RoomId, Name, AvatarUrl, Title, Url);
    }
}
