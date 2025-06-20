using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Extension
{
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[Guid(PackageGuidString)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideOptionPage(typeof(Analyzer.GeneralOptions), "Alignment Analyzer", "General", 0, 0, true,
		SupportsProfiles = true)]
	[ProvideOptionPage(typeof(Analyzer.AssemblyOptions), "Alignment Analyzer", "Assembly", 0, 0, true,
		SupportsProfiles = true)]
	public sealed class ExtensionPackage : AsyncPackage
	{
		public const string PackageGuidString = "85c4ed75-3e80-4378-9884-b3f5ba1416ea";

		#region Package Members

		protected override async Task InitializeAsync(CancellationToken cancellationToken,
			IProgress<ServiceProgressData> progress)
		{
			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
			await Analyzer.InitializeAsync(this);
		}

		#endregion Package Members
	}
}