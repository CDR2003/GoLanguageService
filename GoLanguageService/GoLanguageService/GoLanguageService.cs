using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoLanguageService : LanguageService
	{
		private LanguagePreferences m_preferences;

		private GoScanner m_scanner;

		private GoColorizer m_colorizer;

		private GoSource m_source;

		public GoLanguageService()
		{
			//this.ParseStandardLibrary();
		}

		private void ParseStandardLibrary()
		{
			this.ParseDir( @"D:\DEVELOP\Go\src\pkg" );
		}

		private void ParseDir( string path )
		{
			var dir = new DirectoryInfo( path );
			foreach( var subDir in dir.GetDirectories() )
			{
				this.ParseDir( subDir.FullName );
			}

			foreach( var file in dir.GetFiles( "*.go" ) )
			{
				this.ParseFile( file.FullName );
			}
		}

		private void ParseFile( string path )
		{
			var fileSet = new GoSourceFileSet();
			var parser = new GoParser( fileSet, path, File.ReadAllText( path ) );
			parser.ParseFile();

			if( parser.Errors.Count == 0 )
			{
				Debug.WriteLine( path );
			}
			else
			{
				foreach( var error in parser.Errors )
				{
					error.Pos = parser.File.Position( error.Pos.Offset );
				}
				throw new Exception();
			}
		}

		public override string GetFormatFilterList()
		{
			return "Go files (*.go)\n*.go\n";
		}

		public override LanguagePreferences GetLanguagePreferences()
		{
			if( m_preferences == null )
			{
				m_preferences = new LanguagePreferences( this.Site, typeof( GoLanguageService ).GUID, this.Name );
				m_preferences.EnableCodeSense = true;
				m_preferences.EnableCommenting = true;
				m_preferences.EnableMatchBraces = true;
				m_preferences.EnableMatchBracesAtCaret = true;
				m_preferences.EnableShowMatchingBrace = true;
				m_preferences.Init();
			}
			return m_preferences;
		}

		public override IScanner GetScanner( IVsTextLines buffer )
		{
			if( m_scanner == null )
			{
				m_scanner = new GoScanner();
				m_colorizer = new GoColorizer( this, buffer, m_scanner );
			}
			return m_scanner;
		}

		public override string Name
		{
			get { return "Go"; }
		}

		public override AuthoringScope ParseSource( ParseRequest req )
		{
			var fileSet = new GoSourceFileSet();
			var parser = new GoParser( fileSet, req.FileName, req.Text );
			var file = parser.ParseFile();

			var tasks = m_source.GetTaskProvider().Tasks;
			tasks.Clear();
			foreach( var error in parser.Errors )
			{
				var ts = new TextSpan();
				m_source.GetLineIndexOfPosition( error.Pos.Offset - 1, out ts.iStartLine, out ts.iStartIndex );
				ts.iEndLine = ts.iStartLine;
				ts.iEndIndex = ts.iStartIndex + 1;

				var task = m_source.CreateErrorTaskItem( ts, MARKERTYPE.MARKER_COMPILE_ERROR, req.FileName );
				task.Text = error.Msg;
				tasks.Add( task );
			}

			return new GoAuthoringScope();
		}

		public override Source CreateSource( IVsTextLines buffer )
		{
			if( m_source == null )
			{
				m_source = new GoSource( this, buffer, m_colorizer );
			}
			return m_source;
		}
	}
}
