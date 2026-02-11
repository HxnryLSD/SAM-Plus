/* Copyright (c) 2024-2026 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using Microsoft.UI.Xaml.Controls;
using SAM.Core.Services;

namespace SAM.WinUI.Views;

/// <summary>
/// About page with application information.
/// </summary>
public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        ApplyLocalization();
    }
    
    private void ApplyLocalization()
    {
        PageTitleText.Text = Loc.Get("About.Title");
        DescriptionText.Text = Loc.Get("About.DescriptionLong");
        DevelopedBySectionText.Text = Loc.Get("About.DevelopedBy");
        OriginalDeveloperText.Text = Loc.Get("About.OriginalDeveloper");
        WinUI3ContributorText.Text = Loc.Get("About.WinUI3Contributor");
        LinksSectionText.Text = Loc.Get("About.Links");
        ReportBugLink.Content = Loc.Get("About.ReportBug");
        LicenseSectionText.Text = Loc.Get("About.License");
    }
}
