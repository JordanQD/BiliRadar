using System.Threading;
using System.Threading.Tasks;

namespace BiliRadar.Services;

internal interface IMainPanelPage
{
    void Initialize(MainPanelSession session);

    Task ActivateAsync(CancellationToken cancellationToken = default);
}
