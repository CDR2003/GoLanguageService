using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Package;

namespace Fitbos.GoLanguageService
{
	[PackageRegistration( UseManagedResourcesOnly = true )]
	[InstalledProductRegistration( "#110", "#112", "1.0", IconResourceID = 400 )]
	[Guid( GuidList.guidGoLanguageServicePkgString )]
	[ProvideService( typeof( GoLanguageService ) )]
	[ProvideLanguageExtension( typeof( GoLanguageService ), ".go" )]
	[ProvideLanguageService( typeof( GoLanguageService ), "Go", 0 )]
	public sealed class GoLanguageServicePackage : Package, IOleComponent
	{
		private uint m_componentID;

		public GoLanguageServicePackage()
		{
			Debug.WriteLine( string.Format( CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString() ) );
		}

		protected override void Initialize()
		{
			Debug.WriteLine( string.Format( CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString() ) );
			base.Initialize();

			var container = this as IServiceContainer;
			var service = new GoLanguageService();
			service.SetSite( this );
			container.AddService( typeof( GoLanguageService ), service, true );

			IOleComponentManager mgr = GetService( typeof( SOleComponentManager ) )
									   as IOleComponentManager;
			if( m_componentID == 0 && mgr != null )
			{
				OLECRINFO[] crinfo = new OLECRINFO[1];
				crinfo[0].cbSize = (uint)Marshal.SizeOf( typeof( OLECRINFO ) );
				crinfo[0].grfcrf = (uint)_OLECRF.olecrfNeedIdleTime |
											  (uint)_OLECRF.olecrfNeedPeriodicIdleTime;
				crinfo[0].grfcadvf = (uint)_OLECADVF.olecadvfModal |
											  (uint)_OLECADVF.olecadvfRedrawOff |
											  (uint)_OLECADVF.olecadvfWarningsOff;
				crinfo[0].uIdleTimeInterval = 1000;
				int hr = mgr.FRegisterComponent( this, crinfo, out m_componentID );
			}
		}

		protected override void Dispose( bool disposing )
		{
			if( m_componentID != 0 )
			{
				var manager = this.GetService( typeof( SOleComponentManager ) ) as IOleComponentManager;
				if( manager != null )
				{
					manager.FRevokeComponent( m_componentID );
				}
				m_componentID = 0;
			}

			base.Dispose( disposing );
		}

		public int FContinueMessageLoop( uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked )
		{
			return 1;
		}

		public int FDoIdle( uint grfidlef )
		{
			bool bPeriodic = ( grfidlef & (uint)_OLEIDLEF.oleidlefPeriodic ) != 0;
			var service = GetService( typeof( GoLanguageService ) ) as LanguageService;
			if( service != null )
			{
				service.OnIdle( bPeriodic );
			}
			return 0;
		}

		public int FPreTranslateMessage( MSG[] pMsg )
		{
			return 0;
		}

		public int FQueryTerminate( int fPromptUser )
		{
			return 1;
		}

		public int FReserved1( uint dwReserved, uint message, IntPtr wParam, IntPtr lParam )
		{
			return 1;
		}

		public IntPtr HwndGetWindow( uint dwWhich, uint dwReserved )
		{
			return IntPtr.Zero;
		}

		public void OnActivationChange( IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo, int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved )
		{
		}

		public void OnAppActivate( int fActive, uint dwOtherThreadID )
		{
		}

		public void OnEnterState( uint uStateID, int fEnter )
		{
		}

		public void OnLoseActivation()
		{
		}

		public void Terminate()
		{
		}
	}
}
