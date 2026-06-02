# BiliRadar

一个使用 **WinUI 3 原生开发**的 B 站任务栏托盘动态雷达。  
用于查看关注 UP 的最新视频、直播状态、历史记录、稍后再看，并在后台提醒视频更新和开播。

<a href="https://apps.microsoft.com/detail/9MX8LLSP816P" target="_self" >
	<img src="https://get.microsoft.com/images/zh-cn%20dark.svg" width="200"/>
</a>

<img src="./docs/assets/主界面.png" alt="主界面" width="400" />

> BiliRadar 不是哔哩哔哩官方应用，与哔哩哔哩没有从属、赞助或背书关系。

## 功能

- 关注动态：查看关注 UP 的最新视频。

- 直播状态：显示正在直播的 UP，可在设置中选择展开、折叠或隐藏。

    <img src="./docs/assets/关注.png" alt="关注&直播" width="400" />

- 历史记录：查看最近看过的视频。

    <img src="./docs/assets/历史.png" alt="历史记录" width="400" />

- 稍后再看：查看稍后再看列表，并支持从应用内添加或移除。

    <img src="./docs/assets/稍后再看.png" alt="稍后再看" width="400" />

- 开机自启：可选择登录 Windows 后自动运行。
- 配置导入导出：导出或恢复通知、启动、登录状态等配置。
- 托盘驻留：从任务栏托盘快速打开主界面或设置。

    <img src="./docs/assets/设置-通用.png" alt="设置-通用" width="560" />

- 桌面通知：支持视频更新通知和开播通知，可只关注指定 UP 的视频更新或开播提醒。

    <img src="./docs/assets/设置-通知-所有.png" alt="通知-所有" width="560" />

    <img src="./docs/assets/设置-通知-自定义.png" alt="通知-指定UP" width="560" />

## 安装

详见 [安装教程](./docs/INSTALL.md)。

## 使用说明

首次使用需要在设置中通过网页登录 Bilibili 账号。登录后，BiliRadar 会读取与你账号相关的关注动态、历史记录、稍后再看和直播状态。

BiliRadar 依赖 Bilibili 的网页登录和公开接口行为。如果 Bilibili 调整登录验证、接口返回或风控策略，部分功能可能需要更新后才能继续使用。

## 账号与隐私

登录状态保存在本机应用数据中，用于后续刷新动态。BiliRadar 不运营自己的账号系统，也不会把你的 Bilibili 数据发送到开发者服务器。网络请求由你的设备直接访问 Bilibili 服务或你主动打开的链接。

配置导出文件可能包含登录 Cookie 和其他本地设置，请不要随意分享导出的配置文件。

更完整的隐私说明见 [PRIVACY.md](./docs/PRIVACY.md)。

## 开发环境

- Windows 10 1809 或更高版本。
- Visual Studio 2022，建议安装“.NET 桌面开发”和 Windows 应用 SDK / WinUI 相关工作负载。
- .NET SDK 9 或更高版本。
- Windows App SDK 2.1.3。
- WebView2 Runtime。

项目使用：

- WinUI 3
- Windows App SDK
- CommunityToolkit.Mvvm
- CommunityToolkit.WinUI
- Microsoft.Web.WebView2
- SystemTrayWinUI3

## 编译

还原依赖：

```powershell
dotnet restore BiliRadar.slnx
```

编译 x64：

```powershell
dotnet build BiliRadar.slnx -c Release -p:Platform=x64 --no-restore
```

编译 ARM64：

```powershell
dotnet build BiliRadar.slnx -c Release -p:Platform=ARM64 --no-restore
```

生成用于 Partner Center 的 Microsoft Store 上传包：

```powershell
.\scripts\Build-StorePackage.ps1
```

脚本会读取 `Package.appxmanifest` 中的版本，生成未签名的 x64 + ARM64 自包含 `.msixbundle`。Microsoft Store 要求四段版本号的最后一段保持为 `0`，例如 `1.0.1.0`。

## 贡献

欢迎提交 Issue 或 Pull Request。

## 许可证

本项目采用 **GNU General Public License v3.0（GPL-3.0）** 开源，完整条款见 [LICENSE](LICENSE)。

你可以在 GPL-3.0 许可条款下自由使用、复制、修改和分发本项目的源代码；如果分发修改后的版本或基于本项目的衍生作品，也需要继续以 GPL-3.0 兼容的方式公开相应源代码。

BiliRadar 使用到的第三方项目、库和服务可能适用各自的许可证或使用条款，请在使用和分发时一并遵守。

## 参考

- [Richasy/Bili.Copilot](https://github.com/Richasy/Bili.Copilot)
- [Richasy/bili-kernel](https://github.com/Richasy/bili-kernel)
- [microsoft/WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK)
- [microsoft/WinUI](https://github.com/microsoft/WinUI-Gallery)
- [microsoft/microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml)
- [CommunityToolkit/Windows](https://github.com/CommunityToolkit/Windows)
- [MEHDIMYADI/SystemTrayWinUI3](https://github.com/MEHDIMYADI/SystemTrayWinUI3)
