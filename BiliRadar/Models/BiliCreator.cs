namespace BiliRadar.Models;

public sealed record BiliCreator(long Mid, string Name, string AvatarUrl);

public sealed record BiliLiveCreator(long Mid, long RoomId, string Name, string AvatarUrl, string Title, string Url);
