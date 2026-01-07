# Dashboard Layout Templates

This folder contains layout templates that will be automatically copied to your project when you build after installing the ExperimentFramework.Dashboard.UI NuGet package.

## How It Works

When you first build your project after adding the ExperimentFramework.Dashboard.UI package:

1. **DashboardMainLayout.razor** will be automatically created in your `Components/Layout/` folder
2. You'll see a build message confirming the file was created
3. You can then customize this layout for your application's branding and navigation needs

## Using the Layout

To use the Dashboard layout for your pages, add this to your `Components/_Imports.razor`:

```razor
@layout YourNamespace.Components.Layout.DashboardMainLayout
```

Or specify it on individual pages:

```razor
@page "/your-page"
@layout YourNamespace.Components.Layout.DashboardMainLayout

<h1>Your Page</h1>
```

## Customization

The generated `DashboardMainLayout.razor` is **yours to customize**. You can:

- Change colors, fonts, and styling
- Add or remove navigation links
- Modify the sidebar layout
- Add your company logo/branding
- Integrate with your authentication system
- Add breadcrumbs, notifications, or other UI elements

The file will only be created once - subsequent builds won't overwrite your customizations.

## Why Layouts Are in the Host App

Following .NET 8+ Blazor best practices, layouts should be defined in the consuming application rather than the Razor Class Library. This is because:

1. **RenderFragment serialization** - Blazor doesn't support serializing RenderFragment (`@Body`) as a root component parameter with InteractiveServer render mode
2. **Application control** - The host application should control overall page structure, navigation, and branding
3. **Flexibility** - Different applications can customize the layout to match their design system

## Manual Setup

If you prefer to create the layout manually or the auto-copy didn't work, simply copy `DashboardMainLayout.razor.template` from this folder to your `Components/Layout/` directory and rename it to `DashboardMainLayout.razor`.

## Learn More

- [ASP.NET Core Blazor layouts](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/layouts)
- [ASP.NET Core Blazor render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)
