using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoLanguageService : LanguageService
	{
		private LanguagePreferences m_preferences;

		private GoScanner m_scanner;

		public override string GetFormatFilterList()
		{
			return "Go files (*.go)\n*.go\n";
		}

		public override LanguagePreferences GetLanguagePreferences()
		{
			if( m_preferences == null )
			{
				m_preferences = new LanguagePreferences( this.Site, typeof( GoLanguageService ).GUID, this.Name );
				m_preferences.Init();
			}
			return m_preferences;
		}

		public override IScanner GetScanner( IVsTextLines buffer )
		{
			if( m_scanner == null )
			{
				m_scanner = new GoScanner();
			}
			return m_scanner;
		}

		public override string Name
		{
			get { return "Go"; }
		}

		public override AuthoringScope ParseSource( ParseRequest req )
		{
			return new GoAuthoringScope();
		}
	}
}
